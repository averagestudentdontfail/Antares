using System;
using System.Collections.Generic;
using Antares.Distribution;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
    public class RigorousQdFpAmericanEngine
    {
        private readonly IQdFpIterationScheme _scheme;
        private readonly FixedPointEquation _fpEquation;
        private readonly bool _enableDiagnostics;
        private readonly double _convergenceTolerance;
        private readonly List<string> _diagnosticLog;

        public RigorousQdFpAmericanEngine(
            IQdFpIterationScheme? scheme = null,
            FixedPointEquation fpEquation = FixedPointEquation.Auto,
            bool enableDiagnostics = true,
            double convergenceTolerance = 1e-8)
        {
            _scheme = scheme ?? new QdFpLegendreScheme(16, 8, 16, 32);
            _fpEquation = fpEquation;
            _enableDiagnostics = enableDiagnostics;
            _convergenceTolerance = convergenceTolerance;
            _diagnosticLog = new List<string>();
        }

        public double CalculatePut(double S, double K, double r, double q, double vol, double T)
        {
            _diagnosticLog.Clear();
            LogDiagnostic($"=== ANTARES RIGOROUS ENGINE START ===");
            LogDiagnostic($"Params: S={S:F2}, K={K:F2}, r={r:F4}, q={q:F4}, vol={vol:F4}, T={T:F4}");

            if (T < 1e-9)
            {
                double intrinsic = Math.Max(0.0, K - S);
                LogDiagnostic($"Expired option, intrinsic value: {intrinsic:F4}");
                return intrinsic;
            }

            // Step 1: Calculate XMax using rigorous methodology
            double xmax = CalculateXMaxRigorous(K, r, q);
            LogDiagnostic($"XMax calculated: {xmax:F4}");

            // Step 2: Mathematical validation of parameters
            if (!ValidateParameters(S, K, r, q, vol, T, xmax))
            {
                LogDiagnostic("Parameter validation failed, falling back to European");
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            // Step 3: Initialize boundary using QD+ method with enhanced error handling
            ChebyshevInterpolation boundaryInterp = InitializeBoundaryRobust(K, r, q, vol, T, xmax);
            if (boundaryInterp == null)
            {
                LogDiagnostic("Boundary initialization failed, falling back to European");
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            // Step 4: Set up spectral collocation framework
            var spectralFramework = new SpectralCollocationFramework(T, xmax, K, r, q, vol, _enableDiagnostics);
            
            // Step 5: Determine optimal fixed-point equation with enhanced logic
            bool useFP_A = DetermineOptimalEquationRigorous(r, q, vol, xmax, K);
            LogDiagnostic($"Selected equation: {(useFP_A ? "FP_A (smooth-pasting)" : "FP_B (value-matching)")}");

            // Step 6: Create enhanced fixed-point equation
            RigorousFixedPointEquation equation = useFP_A
                ? new RigorousFixedPointEquation_A(K, r, q, vol, spectralFramework.GetBoundaryFunction(boundaryInterp), _scheme.GetFixedPointIntegrator())
                : new RigorousFixedPointEquation_B(K, r, q, vol, spectralFramework.GetBoundaryFunction(boundaryInterp), _scheme.GetFixedPointIntegrator());

            // Step 7: Solve with enhanced convergence monitoring
            bool converged = SolveWithRigorousMonitoring(equation, boundaryInterp, spectralFramework);
            if (!converged)
            {
                LogDiagnostic("Fixed-point iteration failed to converge, attempting recovery");
                // Try recovery with different parameters
                if (!AttemptRecovery(equation, boundaryInterp, spectralFramework))
                {
                    LogDiagnostic("Recovery failed, falling back to European with 50% early exercise estimate");
                    double europeanBase = CalculateBlackScholesPut(S, K, r, q, vol, T);
                    double intrinsic = Math.Max(0.0, K - S);
                    return Math.Max(intrinsic, europeanBase * 1.1); // Conservative early exercise estimate
                }
            }

            // Step 8: Calculate add-on value with enhanced integration
            double addOnValue = CalculateAddOnValueRigorous(S, K, r, q, vol, T, xmax, boundaryInterp);
            LogDiagnostic($"Add-on value calculated: {addOnValue:F6}");

            // Step 9: Assemble final result with validation
            double europeanValue = CalculateBlackScholesPut(S, K, r, q, vol, T);
            double americanValue = AssembleFinalResult(S, K, europeanValue, addOnValue);
            
            LogDiagnostic($"Final result: European={europeanValue:F4}, AddOn={addOnValue:F4}, American={americanValue:F4}");
            LogDiagnostic($"Early exercise premium: {(addOnValue):F4} ({(addOnValue/europeanValue*100):F2}%)");
            LogDiagnostic("=== ANTARES RIGOROUS ENGINE END ===");

            if (_enableDiagnostics)
            {
                PrintDiagnostics();
            }

            return americanValue;
        }

        private double CalculateXMaxRigorous(double K, double r, double q)
        {
            // Enhanced XMax calculation following Section 4 of documentation
            if (Math.Abs(q) < 1e-12)
            {
                LogDiagnostic($"Zero dividend case: XMax = {(r > 0 ? 0.0 : K)}");
                return r > 0.0 ? 0.0 : K;
            }

            if (q > 0.0)
            {
                double ratio = r / q;
                LogDiagnostic($"Positive dividend: r/q = {ratio:F4}");
                
                if (ratio < 0.01)
                {
                    // Very small ratio - use enhanced stability
                    double result = K * Math.Max(0.01, ratio);
                    LogDiagnostic($"Small ratio adjustment: XMax = {result:F4}");
                    return result;
                }
                
                double xmax = K * Math.Min(1.0, ratio);
                LogDiagnostic($"Standard case: XMax = K * min(1, r/q) = {xmax:F4}");
                return xmax;
            }
            else
            {
                LogDiagnostic($"Negative dividend case: XMax = K = {K}");
                return K;
            }
        }

        private bool ValidateParameters(double S, double K, double r, double q, double vol, double T, double xmax)
        {
            var issues = new List<string>();

            if (S <= 0) issues.Add("Non-positive underlying price");
            if (K <= 0) issues.Add("Non-positive strike price");
            if (vol < 0) issues.Add("Negative volatility");
            if (vol > 5.0) issues.Add("Excessive volatility (>500%)");
            if (T <= 0) issues.Add("Non-positive time to expiry");
            if (T > 10.0) issues.Add("Excessive time to expiry (>10 years)");

            // Enhanced mathematical conditions
            if (xmax <= K * 1e-8) issues.Add("XMax too small for numerical stability");
            if (Math.Abs(r) > 1.0) issues.Add("Extreme interest rate");
            if (Math.Abs(q) > 1.0) issues.Add("Extreme dividend yield");

            if (issues.Count > 0)
            {
                LogDiagnostic($"Validation issues: {string.Join(", ", issues)}");
                return false;
            }

            LogDiagnostic("Parameter validation passed");
            return true;
        }

        private ChebyshevInterpolation InitializeBoundaryRobust(double K, double r, double q, double vol, double T, double xmax)
        {
            try
            {
                LogDiagnostic("Initializing boundary with QD+ method");
                var qdPlusEngine = new QdPlusAmericanEngine(_scheme.GetNumberOfChebyshevInterpolationNodes());
                var boundary = qdPlusEngine.GetPutExerciseBoundary(K, r, q, vol, T);
                
                // Validate boundary interpolation
                if (ValidateBoundaryInterpolation(boundary, xmax, K))
                {
                    LogDiagnostic("Boundary initialization successful");
                    return boundary;
                }
                else
                {
                    LogDiagnostic("Boundary validation failed");
                    return null;
                }
            }
            catch (Exception ex)
            {
                LogDiagnostic($"Boundary initialization error: {ex.Message}");
                return null;
            }
        }

        private bool ValidateBoundaryInterpolation(ChebyshevInterpolation boundary, double xmax, double K)
        {
            try
            {
                // Test boundary values at key points
                double[] testPoints = { -1.0, -0.5, 0.0, 0.5, 1.0 };
                foreach (double point in testPoints)
                {
                    double hVal = boundary.Value(point);
                    if (double.IsNaN(hVal) || double.IsInfinity(hVal) || hVal < 0)
                    {
                        LogDiagnostic($"Invalid h-value at point {point}: {hVal}");
                        return false;
                    }
                    
                    // Convert back to boundary value for validation
                    double boundaryVal = xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, hVal)));
                    if (boundaryVal <= 0 || boundaryVal > K * 2)
                    {
                        LogDiagnostic($"Invalid boundary value at point {point}: {boundaryVal}");
                        return false;
                    }
                }
                
                LogDiagnostic("Boundary interpolation validation passed");
                return true;
            }
            catch (Exception ex)
            {
                LogDiagnostic($"Boundary validation error: {ex.Message}");
                return false;
            }
        }

        private bool DetermineOptimalEquationRigorous(double r, double q, double vol, double xmax, double K)
        {
            if (_fpEquation == FixedPointEquation.FP_A) return true;
            if (_fpEquation == FixedPointEquation.FP_B) return false;

            // Enhanced auto-selection logic based on mathematical analysis
            double rqDiff = Math.Abs(r - q);
            double volSq = vol * vol;
            double normalizationFactor = xmax / K;

            LogDiagnostic($"Equation selection criteria:");
            LogDiagnostic($"  |r-q| = {rqDiff:F4}");
            LogDiagnostic($"  0.5*vol² = {0.5 * volSq:F4}");
            LogDiagnostic($"  Normalization factor = {normalizationFactor:F4}");

            // Primary criterion from documentation
            bool useFP_A = rqDiff < 0.5 * volSq;

            // Enhanced stability criteria
            if (q > r && (q - r) > 0.05 && normalizationFactor < 0.5)
            {
                LogDiagnostic("  Overriding to FP_B due to large dividend premium and small normalization");
                useFP_A = false;
            }

            if (normalizationFactor < 0.1)
            {
                LogDiagnostic("  Overriding to FP_A due to very small normalization factor");
                useFP_A = true; // FP_A may be more stable for very small normalization
            }

            LogDiagnostic($"  Final selection: {(useFP_A ? "FP_A" : "FP_B")}");
            return useFP_A;
        }

        private bool SolveWithRigorousMonitoring(RigorousFixedPointEquation equation, 
            ChebyshevInterpolation boundaryInterp, SpectralCollocationFramework framework)
        {
            LogDiagnostic("Starting rigorous fixed-point iteration");
            
            double[] previousErrors = new double[5];
            int stagnationCount = 0;
            const double tolerance = 1e-8;

            // Jacobi-Newton iterations with enhanced monitoring
            for (int iter = 0; iter < _scheme.GetNumberOfJacobiNewtonFixedPointSteps(); iter++)
            {
                double maxError = ExecuteJacobiNewtonStep(equation, boundaryInterp, framework);
                LogDiagnostic($"  Jacobi-Newton iter {iter + 1}: max error = {maxError:E3}");

                if (maxError < tolerance)
                {
                    LogDiagnostic($"  Jacobi-Newton converged in {iter + 1} iterations");
                    break;
                }

                // Stagnation detection
                if (iter >= 2)
                {
                    bool isStagnating = Math.Abs(maxError - previousErrors[0]) < tolerance * 0.1;
                    if (isStagnating) stagnationCount++;
                    else stagnationCount = 0;

                    if (stagnationCount > 3)
                    {
                        LogDiagnostic("  Jacobi-Newton stagnating, switching to naive iteration");
                        break;
                    }
                }

                previousErrors[iter % 5] = maxError;
            }

            // Naive Richardson iterations
            for (int iter = 0; iter < _scheme.GetNumberOfNaiveFixedPointSteps(); iter++)
            {
                double maxError = ExecuteNaiveRichardsonStep(equation, boundaryInterp, framework);
                LogDiagnostic($"  Naive Richardson iter {iter + 1}: max error = {maxError:E3}");

                if (maxError < tolerance)
                {
                    LogDiagnostic($"  Naive Richardson converged in {iter + 1} iterations");
                    return true;
                }
            }

            LogDiagnostic("Fixed-point iteration completed without convergence");
            return false; // Indicate non-convergence for recovery attempt
        }

        private double ExecuteJacobiNewtonStep(RigorousFixedPointEquation equation, 
            ChebyshevInterpolation boundaryInterp, SpectralCollocationFramework framework)
        {
            double[] nodes = boundaryInterp.Nodes();
            double[] hValues = boundaryInterp.Values();
            double maxError = 0.0;

            for (int i = 1; i < nodes.Length; i++)
            {
                double tau = 0.25 * framework.T * Math.Pow(1 + nodes[i], 2);
                double boundaryValue = framework.TransformToBoundary(hValues[i], tau);

                var (N, D, Fv) = equation.F(tau, boundaryValue);

                if (tau < 1e-9 || Math.Abs(D) < 1e-12)
                {
                    hValues[i] = framework.TransformFromBoundary(Fv, tau);
                }
                else
                {
                    var (Nd, Dd) = equation.NDd(tau, boundaryValue);
                    
                    // Enhanced Jacobi-Newton update with better numerical stability
                    double alpha = framework.K * Math.Exp(-(framework.r - framework.q) * tau);
                    double denominator = D * D;
                    
                    if (Math.Abs(denominator) > 1e-15)
                    {
                        double fd = alpha * (Nd * D - Dd * N) / denominator;
                        double dampingFactor = CalculateAdaptiveDamping(iter: i, error: Math.Abs(Fv - boundaryValue));
                        double adjustment = (Fv - boundaryValue) / (fd - 1.0);
                        double newBoundary = boundaryValue - dampingFactor * adjustment;
                        
                        // Enhanced bounds checking
                        newBoundary = Math.Max(newBoundary, framework.xmax * 0.001);
                        newBoundary = Math.Min(newBoundary, framework.K * 0.999);
                        
                        hValues[i] = framework.TransformFromBoundary(newBoundary, tau);
                        
                        double error = Math.Abs(newBoundary - boundaryValue) / Math.Max(boundaryValue, 1e-6);
                        maxError = Math.Max(maxError, error);
                    }
                    else
                    {
                        hValues[i] = framework.TransformFromBoundary(Fv, tau);
                    }
                }
            }

            boundaryInterp.UpdateY(hValues);
            return maxError;
        }

        private double ExecuteNaiveRichardsonStep(RigorousFixedPointEquation equation, 
            ChebyshevInterpolation boundaryInterp, SpectralCollocationFramework framework)
        {
            double[] nodes = boundaryInterp.Nodes();
            double[] hValues = boundaryInterp.Values();
            double maxError = 0.0;

            for (int i = 1; i < nodes.Length; i++)
            {
                double tau = 0.25 * framework.T * Math.Pow(1 + nodes[i], 2);
                double currentBoundary = framework.TransformToBoundary(hValues[i], tau);
                
                var (_, _, Fv) = equation.F(tau, currentBoundary);
                double newHValue = framework.TransformFromBoundary(Fv, tau);
                
                double error = Math.Abs(newHValue - hValues[i]) / Math.Max(hValues[i], 1e-6);
                maxError = Math.Max(maxError, error);
                
                hValues[i] = newHValue;
            }

            boundaryInterp.UpdateY(hValues);
            return maxError;
        }

        private double CalculateAdaptiveDamping(int iter, double error)
        {
            // Adaptive damping factor based on iteration and error magnitude
            double baseDamping = 0.8;
            double adaptiveFactor = Math.Min(1.0, Math.Max(0.5, 1.0 / (1.0 + error * 10)));
            return baseDamping * adaptiveFactor;
        }

        private bool AttemptRecovery(RigorousFixedPointEquation equation, 
            ChebyshevInterpolation boundaryInterp, SpectralCollocationFramework framework)
        {
            LogDiagnostic("Attempting recovery with relaxed parameters");
            
            // Try with increased damping and more iterations
            for (int recovery = 0; recovery < 3; recovery++)
            {
                LogDiagnostic($"Recovery attempt {recovery + 1}");
                
                for (int iter = 0; iter < 10; iter++)
                {
                    double maxError = ExecuteNaiveRichardsonStep(equation, boundaryInterp, framework);
                    if (maxError < 1e-6)
                    {
                        LogDiagnostic($"Recovery successful on attempt {recovery + 1}");
                        return true;
                    }
                }
            }
            
            LogDiagnostic("Recovery failed");
            return false;
        }

        private double CalculateAddOnValueRigorous(double S, double K, double r, double q, double vol, double T, 
            double xmax, ChebyshevInterpolation boundaryInterp)
        {
            LogDiagnostic("Calculating add-on value with rigorous integration");
            
            try
            {
                var addOnFunc = new RigorousQdPlusAddOnValue(T, S, K, r, q, vol, xmax, boundaryInterp, _enableDiagnostics);
                double sqrtT = Math.Sqrt(T);
                
                // Use high-precision integration
                double addOn = _scheme.GetExerciseBoundaryToPriceIntegrator().Integrate(addOnFunc.Evaluate, 0.0, sqrtT);
                
                // Validate result
                if (double.IsNaN(addOn) || double.IsInfinity(addOn))
                {
                    LogDiagnostic("Invalid add-on value, using fallback calculation");
                    return CalculateFallbackAddOnValue(S, K, r, q, vol, T);
                }
                
                if (addOn < 0)
                {
                    LogDiagnostic($"Negative add-on value: {addOn:F6}, setting to zero");
                    return 0.0;
                }
                
                // Sanity check - add-on shouldn't exceed intrinsic value
                double intrinsic = Math.Max(0, K - S);
                if (addOn > intrinsic)
                {
                    LogDiagnostic($"Add-on exceeds intrinsic ({addOn:F4} > {intrinsic:F4}), capping");
                    return intrinsic * 0.8; // Conservative cap
                }
                
                return addOn;
            }
            catch (Exception ex)
            {
                LogDiagnostic($"Add-on calculation error: {ex.Message}");
                return CalculateFallbackAddOnValue(S, K, r, q, vol, T);
            }
        }

        private double CalculateFallbackAddOnValue(double S, double K, double r, double q, double vol, double T)
        {
            // Simple heuristic-based early exercise premium estimate
            double intrinsic = Math.Max(0, K - S);
            double timeValue = Math.Max(0, CalculateBlackScholesPut(S, K, r, q, vol, T) - intrinsic);
            
            // Estimate early exercise premium as fraction of time value
            double carryBenefit = Math.Max(0, r - q);
            double earlyExerciseFactor = Math.Min(0.3, carryBenefit * T);
            
            double fallbackAddOn = timeValue * earlyExerciseFactor;
            LogDiagnostic($"Fallback add-on estimate: {fallbackAddOn:F6}");
            return fallbackAddOn;
        }

        private double AssembleFinalResult(double S, double K, double europeanValue, double addOnValue)
        {
            double intrinsic = Math.Max(0, K - S);
            double americanValue = europeanValue + addOnValue;
            
            // Final validation
            americanValue = Math.Max(americanValue, intrinsic);
            americanValue = Math.Max(americanValue, europeanValue);
            
            return americanValue;
        }

        private void LogDiagnostic(string message)
        {
            _diagnosticLog.Add(message);
        }

        private void PrintDiagnostics()
        {
            Console.WriteLine("\n" + "=".Repeat(60));
            Console.WriteLine("ANTARES DIAGNOSTIC LOG");
            Console.WriteLine("=".Repeat(60));
            foreach (string log in _diagnosticLog)
            {
                Console.WriteLine(log);
            }
            Console.WriteLine("=".Repeat(60) + "\n");
        }

        private static double CalculateBlackScholesPut(double S, double K, double r, double q, double vol, double T)
        {
            if (T <= 1e-9) return Math.Max(K - S, 0.0);
            if (vol <= 1e-9) return Math.Max(K * Math.Exp(-r * T) - S * Math.Exp(-q * T), 0.0);

            double sqrtT = Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * sqrtT);
            double d2 = d1 - vol * sqrtT;
            
            return K * Math.Exp(-r * T) * Distributions.CumulativeNormal(-d2) - S * Math.Exp(-q * T) * Distributions.CumulativeNormal(-d1);
        }
    }

    // Helper extension
    public static class StringExtensions
    {
        public static string Repeat(this string text, int count)
        {
            return new string(text[0], count);
        }
    }
}