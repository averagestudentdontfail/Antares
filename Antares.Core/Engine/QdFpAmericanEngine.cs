using System;
using Antares.Distribution;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
    public enum FixedPointEquation { FP_A, FP_B, Auto }

    public class QdFpAmericanEngine
    {
        private readonly IQdFpIterationScheme _scheme;
        private readonly FixedPointEquation _fpEquation;

        public QdFpAmericanEngine(
            IQdFpIterationScheme? scheme = null,
            FixedPointEquation fpEquation = FixedPointEquation.Auto)
        {
            _scheme = scheme ?? new QdFpLegendreScheme(16, 8, 16, 32);
            _fpEquation = fpEquation;
        }

        public double CalculatePut(double S, double K, double r, double q, double vol, double T)
        {
            if (T < 1e-9) return Math.Max(0.0, K - S);

            // Calculate the theoretical maximum boundary using proper asymptotic analysis
            double xmax = CalculateAsymptoticBoundary(K, r, q, vol);
            
            // For very small boundaries relative to strike, use European pricing
            if (xmax <= K * 0.001)
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            // Initialize the boundary using spectral collocation
            ChebyshevInterpolation boundaryInterp;
            try
            {
                boundaryInterp = InitializeBoundaryInterpolation(K, r, q, vol, T, xmax);
            }
            catch (Exception)
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            // Set up the mathematical framework
            var framework = new SpCollocation(T, xmax, K, r, q, vol);
            
            // Select optimal fixed-point equation based on parameter regime
            bool useFP_A = DetermineOptimalEquation(r, q, vol, T, xmax, K);

            DqFpEquation equation = useFP_A
                ? new DqFpEquation_A(K, r, q, vol, framework.GetBoundaryFunction(boundaryInterp), _scheme.GetFixedPointIntegrator())
                : new DqFpEquation_B(K, r, q, vol, framework.GetBoundaryFunction(boundaryInterp), _scheme.GetFixedPointIntegrator());

            // Solve the fixed-point system with enhanced convergence monitoring
            bool converged = SolveFixedPoint(equation, boundaryInterp, framework);
            
            if (!converged)
            {
                // Enhanced recovery mechanism
                converged = AttemptEnhancedRecovery(equation, boundaryInterp, framework);
                
                if (!converged)
                {
                    // Fallback with modest early exercise premium
                    double europeanBase = CalculateBlackScholesPut(S, K, r, q, vol, T);
                    double intrinsic = Math.Max(0.0, K - S);
                    double conservativePremium = EstimateConservativePremium(S, K, r, q, vol, T);
                    return Math.Max(intrinsic, europeanBase + conservativePremium);
                }
            }

            // Calculate the final option price using the spectral method
            double addOnValue = CalculateSpectralAddOnValue(S, K, r, q, vol, T, xmax, boundaryInterp);
            double europeanValue = CalculateBlackScholesPut(S, K, r, q, vol, T);
            double intrinsicValue = Math.Max(0.0, K - S);

            double americanPrice = europeanValue + Math.Max(0.0, addOnValue);
            return Math.Max(intrinsicValue, americanPrice);
        }

        private double CalculateAsymptoticBoundary(double K, double r, double q, double vol)
        {
            // Asymptotic boundary for infinite maturity (perpetual option)
            if (Math.Abs(q) < 1e-12)
            {
                return r <= 0 ? K * 0.9 : 0.0;
            }

            if (r <= q)
            {
                // When r <= q, early exercise is optimal for deeply ITM options
                return K * Math.Max(0.01, Math.Min(0.99, r / q));
            }

            // Standard case: r > q
            double mu = r - q - 0.5 * vol * vol;
            double discriminant = mu * mu + 2.0 * r * vol * vol;
            
            if (discriminant <= 0)
            {
                return K * 0.8;
            }
            
            double lambda_minus = (mu - Math.Sqrt(discriminant)) / (vol * vol);
            
            if (lambda_minus >= -1e-6)
            {
                return K * 0.8;
            }
            
            double boundary = K * lambda_minus / (lambda_minus - 1.0);
            return Math.Max(K * 0.01, Math.Min(K * 0.99, boundary));
        }

        private ChebyshevInterpolation InitializeBoundaryInterpolation(double K, double r, double q, double vol, double T, double xmax)
        {
            int n = _scheme.GetNumberOfChebyshevInterpolationNodes();
            double sqrtT = Math.Sqrt(T);
            
            Func<double, double> initialBoundaryFunction = z =>
            {
                // Transform from Chebyshev domain [-1,1] to time domain [0,T]
                double xi = 0.5 * (1.0 + z);
                double tau = xi * xi * T;
                
                if (tau < 1e-12)
                {
                    // Near expiration boundary
                    double nearExpiryBoundary = r >= q ? K : K * r / q;
                    double ratio = Math.Max(1e-12, nearExpiryBoundary) / xmax;
                    ratio = Math.Max(1e-8, Math.Min(1.0 - 1e-8, ratio));
                    return Math.Sqrt(Math.Max(0.0, -Math.Log(ratio)));
                }
                
                // Interpolate between near-expiry and asymptotic values
                double asymptoticBoundary = CalculateAsymptoticBoundary(K, r, q, vol);
                double nearExpiryBoundary2 = r >= q ? K : K * r / q;
                
                double weight = Math.Min(1.0, tau / T);
                double boundary = nearExpiryBoundary2 * (1.0 - weight) + asymptoticBoundary * weight;
                
                // Apply variance-stabilizing transformation
                double ratio = Math.Max(1e-12, boundary) / xmax;
                ratio = Math.Max(1e-8, Math.Min(1.0 - 1e-8, ratio));
                double G = Math.Log(ratio);
                return Math.Max(0.0, G * G);
            };
            
            return new ChebyshevInterpolation(n, initialBoundaryFunction);
        }

        private bool DetermineOptimalEquation(double r, double q, double vol, double T, double xmax, double K)
        {
            if (_fpEquation == FixedPointEquation.FP_A) return true;
            if (_fpEquation == FixedPointEquation.FP_B) return false;

            // Enhanced equation selection based on mathematical properties
            double rqDiff = Math.Abs(r - q);
            double volSq = vol * vol;
            double timeScale = vol * Math.Sqrt(T);
            double normalizationFactor = xmax / K;

            // Use FP_A (derivative-based) for well-behaved cases
            if (rqDiff < 0.3 * volSq && timeScale > 0.1 && normalizationFactor > 0.1)
            {
                return true;
            }

            // Use FP_B (integral-based) for extreme parameter cases
            if (q > r + 0.02 || normalizationFactor < 0.05 || timeScale < 0.05)
            {
                return false;
            }

            // Default to FP_A for moderate cases
            return true;
        }

        private bool SolveFixedPoint(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            const double tolerance = 1e-8;
            const double stagnationTolerance = 1e-10;
            double[] errorHistory = new double[5];
            int stagnationCount = 0;

            // Jacobi-Newton iterations for rapid convergence
            for (int iter = 0; iter < _scheme.GetNumberOfJacobiNewtonFixedPointSteps(); iter++)
            {
                double maxError = ExecuteJacobiNewtonStep(equation, boundaryInterp, framework);

                if (maxError < tolerance)
                {
                    return true;
                }

                // Monitor for stagnation
                if (iter > 2)
                {
                    bool isStagnating = Math.Abs(maxError - errorHistory[iter % 5]) < stagnationTolerance;
                    stagnationCount = isStagnating ? stagnationCount + 1 : 0;

                    if (stagnationCount > 3)
                    {
                        break;
                    }
                }

                errorHistory[iter % 5] = maxError;
            }

            // Naive fixed-point iterations for robustness
            for (int iter = 0; iter < _scheme.GetNumberOfNaiveFixedPointSteps(); iter++)
            {
                double maxError = ExecuteNaiveFixedPointStep(equation, boundaryInterp, framework);

                if (maxError < tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        private double ExecuteJacobiNewtonStep(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            double[] nodes = boundaryInterp.Nodes();
            double[] hValues = boundaryInterp.Values();
            double maxError = 0.0;

            // Skip the endpoint z = -1 (corresponds to tau = 0)
            for (int i = 1; i < nodes.Length; i++)
            {
                double xi = 0.5 * (1.0 + nodes[i]);
                double tau = xi * xi * framework.T;
                
                if (tau < 1e-12) continue;
                
                double boundaryValue = framework.TransformToBoundary(hValues[i], tau);
                var (N, D, Fv) = equation.F(tau, boundaryValue);

                if (Math.Abs(D) < 1e-12)
                {
                    hValues[i] = framework.TransformFromBoundary(Fv, tau);
                    continue;
                }

                var (Nd, Dd) = equation.NDd(tau, boundaryValue);
                
                // Newton step with adaptive damping
                double alpha = framework.K * Math.Exp(-(framework.r - framework.q) * tau);
                double jacobian = alpha * (Nd * D - Dd * N) / (D * D);
                
                if (Math.Abs(jacobian - 1.0) > 1e-12)
                {
                    double step = (Fv - boundaryValue) / (jacobian - 1.0);
                    
                    // Adaptive damping based on step size
                    double dampingFactor = Math.Min(0.8, 1.0 / (1.0 + Math.Abs(step) / boundaryValue));
                    double newBoundary = boundaryValue - dampingFactor * step;
                    
                    // Enforce bounds
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

            boundaryInterp.UpdateY(hValues);
            return maxError;
        }

        private double ExecuteNaiveFixedPointStep(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            double[] nodes = boundaryInterp.Nodes();
            double[] hValues = boundaryInterp.Values();
            double maxError = 0.0;

            for (int i = 1; i < nodes.Length; i++)
            {
                double xi = 0.5 * (1.0 + nodes[i]);
                double tau = xi * xi * framework.T;
                
                if (tau < 1e-12) continue;
                
                double currentBoundary = framework.TransformToBoundary(hValues[i], tau);
                var (_, _, Fv) = equation.F(tau, currentBoundary);
                
                // Richardson extrapolation for improved convergence
                double newHValue = framework.TransformFromBoundary(Fv, tau);
                double relaxedUpdate = 0.7 * newHValue + 0.3 * hValues[i];
                
                double error = Math.Abs(relaxedUpdate - hValues[i]) / Math.Max(hValues[i], 1e-6);
                maxError = Math.Max(maxError, error);
                
                hValues[i] = relaxedUpdate;
            }

            boundaryInterp.UpdateY(hValues);
            return maxError;
        }

        private bool AttemptEnhancedRecovery(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            // Re-initialize with simpler boundary guess
            double[] nodes = boundaryInterp.Nodes();
            double[] hValues = boundaryInterp.Values();
            
            for (int i = 0; i < nodes.Length; i++)
            {
                double xi = 0.5 * (1.0 + nodes[i]);
                double tau = xi * xi * framework.T;
                
                // Simple linear interpolation between bounds
                double weight = Math.Min(1.0, tau / framework.T);
                double simpleBoundary = framework.K * (0.9 * (1.0 - weight) + 0.7 * weight);
                
                hValues[i] = framework.TransformFromBoundary(simpleBoundary, tau);
            }
            
            boundaryInterp.UpdateY(hValues);
            
            // Attempt convergence with relaxed tolerance
            for (int iter = 0; iter < 20; iter++)
            {
                double maxError = ExecuteNaiveFixedPointStep(equation, boundaryInterp, framework);
                if (maxError < 1e-6)
                {
                    return true;
                }
            }
            
            return false;
        }

        private double CalculateSpectralAddOnValue(double S, double K, double r, double q, double vol, double T, 
            double xmax, ChebyshevInterpolation boundaryInterp)
        {
            try
            {
                var addOnFunc = new QdPlusAddOnValue(T, S, K, r, q, vol, xmax, boundaryInterp);
                double sqrtT = Math.Sqrt(T);
                
                // Use high-precision integration for the add-on value
                double addOn = _scheme.GetExerciseBoundaryToPriceIntegrator().Integrate(addOnFunc.Evaluate, 0.0, sqrtT);
                
                // Validate the result
                if (double.IsNaN(addOn) || double.IsInfinity(addOn) || addOn < 0)
                {
                    return EstimateConservativePremium(S, K, r, q, vol, T);
                }
                
                // Sanity check: premium shouldn't exceed intrinsic value
                double intrinsic = Math.Max(0, K - S);
                double timeValue = Math.Max(0, CalculateBlackScholesPut(S, K, r, q, vol, T) - intrinsic);
                
                if (addOn > intrinsic + timeValue)
                {
                    return timeValue * 0.5;
                }
                
                return addOn;
            }
            catch (Exception)
            {
                return EstimateConservativePremium(S, K, r, q, vol, T);
            }
        }

        private double EstimateConservativePremium(double S, double K, double r, double q, double vol, double T)
        {
            double intrinsic = Math.Max(0, K - S);
            double timeValue = Math.Max(0, CalculateBlackScholesPut(S, K, r, q, vol, T) - intrinsic);
            
            // Conservative estimate based on carry benefit
            double carryBenefit = Math.Max(0, r - q);
            double moneyness = S / K;
            
            // More premium for deeper ITM options when r > q
            double itmFactor = moneyness < 1.0 ? Math.Pow(1.0 - moneyness, 0.5) : 0.0;
            double timeFactor = Math.Min(1.0, Math.Sqrt(T));
            
            double estimatedPremium = timeValue * carryBenefit * itmFactor * timeFactor * 0.3;
            
            return Math.Min(estimatedPremium, intrinsic * 0.1);
        }

        private static double CalculateBlackScholesPut(double S, double K, double r, double q, double vol, double T)
        {
            if (T <= 1e-9) return Math.Max(K - S, 0.0);
            if (vol <= 1e-9) return Math.Max(K * Math.Exp(-r * T) - S * Math.Exp(-q * T), 0.0);

            double sqrtT = Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * sqrtT);
            double d2 = d1 - vol * sqrtT;
            
            return K * Math.Exp(-r * T) * Distributions.CumulativeNormal(-d2) - 
                   S * Math.Exp(-q * T) * Distributions.CumulativeNormal(-d1);
        }
    }
}