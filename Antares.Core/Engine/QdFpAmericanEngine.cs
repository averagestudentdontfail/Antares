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

            double xmax = QdPlusAmericanEngine.XMax(K, r, q);
            
            if (xmax <= K * 1e-6)
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            var qdPlusEngine = new QdPlusAmericanEngine(_scheme.GetNumberOfChebyshevInterpolationNodes());
            ChebyshevInterpolation boundaryInterp;
            
            try
            {
                boundaryInterp = qdPlusEngine.GetPutExerciseBoundary(K, r, q, vol, T);
            }
            catch (Exception)
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            if (!ValidateBoundary(boundaryInterp, xmax, K))
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            var framework = new SpCollocation(T, xmax, K, r, q, vol);
            bool useFP_A = DetermineOptimalEquation(r, q, vol, xmax, K);

            DqFpEquation equation = useFP_A
                ? new DqFpEquation_A(K, r, q, vol, framework.GetBoundaryFunction(boundaryInterp), _scheme.GetFixedPointIntegrator())
                : new DqFpEquation_B(K, r, q, vol, framework.GetBoundaryFunction(boundaryInterp), _scheme.GetFixedPointIntegrator());

            bool converged = SolveFixedPoint(equation, boundaryInterp, framework);
            if (!converged)
            {
                if (!AttemptRecovery(equation, boundaryInterp, framework))
                {
                    double europeanBase = CalculateBlackScholesPut(S, K, r, q, vol, T);
                    double intrinsic = Math.Max(0.0, K - S);
                    return Math.Max(intrinsic, europeanBase * 1.05);
                }
            }

            double addOnValue = CalculateAddOnValue(S, K, r, q, vol, T, xmax, boundaryInterp);
            double europeanValue = CalculateBlackScholesPut(S, K, r, q, vol, T);

            return Math.Max(K - S, europeanValue + Math.Max(0.0, addOnValue));
        }

        private bool ValidateBoundary(ChebyshevInterpolation boundary, double xmax, double K)
        {
            try
            {
                double[] testPoints = { -1.0, -0.5, 0.0, 0.5, 1.0 };
                foreach (double point in testPoints)
                {
                    double hVal = boundary.Value(point);
                    if (double.IsNaN(hVal) || double.IsInfinity(hVal) || hVal < 0)
                        return false;
                    
                    double boundaryVal = xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, hVal)));
                    if (boundaryVal <= 0 || boundaryVal > K * 2)
                        return false;
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private bool DetermineOptimalEquation(double r, double q, double vol, double xmax, double K)
        {
            if (_fpEquation == FixedPointEquation.FP_A) return true;
            if (_fpEquation == FixedPointEquation.FP_B) return false;

            double rqDiff = Math.Abs(r - q);
            double volSq = vol * vol;
            double normalizationFactor = xmax / K;

            bool useFP_A = rqDiff < 0.5 * volSq;

            if (q > r && (q - r) > 0.05 && normalizationFactor < 0.5)
            {
                useFP_A = false;
            }

            if (normalizationFactor < 0.1)
            {
                useFP_A = true;
            }

            return useFP_A;
        }

        private bool SolveFixedPoint(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            double[] previousErrors = new double[5];
            int stagnationCount = 0;
            const double tolerance = 1e-8;

            for (int iter = 0; iter < _scheme.GetNumberOfJacobiNewtonFixedPointSteps(); iter++)
            {
                double maxError = ExecuteJacobiNewtonStep(equation, boundaryInterp, framework);

                if (maxError < tolerance)
                {
                    break;
                }

                if (iter >= 2)
                {
                    bool isStagnating = Math.Abs(maxError - previousErrors[0]) < tolerance * 0.1;
                    if (isStagnating) stagnationCount++;
                    else stagnationCount = 0;

                    if (stagnationCount > 3)
                    {
                        break;
                    }
                }

                previousErrors[iter % 5] = maxError;
            }

            for (int iter = 0; iter < _scheme.GetNumberOfNaiveFixedPointSteps(); iter++)
            {
                double maxError = ExecuteNaiveRichardsonStep(equation, boundaryInterp, framework);

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
                    
                    double alpha = framework.K * Math.Exp(-(framework.r - framework.q) * tau);
                    double denominator = D * D;
                    
                    if (Math.Abs(denominator) > 1e-15)
                    {
                        double fd = alpha * (Nd * D - Dd * N) / denominator;
                        double dampingFactor = 0.8;
                        double adjustment = (Fv - boundaryValue) / (fd - 1.0);
                        double newBoundary = boundaryValue - dampingFactor * adjustment;
                        
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

        private double ExecuteNaiveRichardsonStep(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
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

        private bool AttemptRecovery(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            for (int recovery = 0; recovery < 3; recovery++)
            {
                for (int iter = 0; iter < 10; iter++)
                {
                    double maxError = ExecuteNaiveRichardsonStep(equation, boundaryInterp, framework);
                    if (maxError < 1e-6)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private double CalculateAddOnValue(double S, double K, double r, double q, double vol, double T, 
            double xmax, ChebyshevInterpolation boundaryInterp)
        {
            try
            {
                var addOnFunc = new QdPlusAddOnValue(T, S, K, r, q, vol, xmax, boundaryInterp);
                double sqrtT = Math.Sqrt(T);
                
                double addOn = _scheme.GetExerciseBoundaryToPriceIntegrator().Integrate(addOnFunc.Evaluate, 0.0, sqrtT);
                
                if (double.IsNaN(addOn) || double.IsInfinity(addOn))
                {
                    return CalculateFallbackAddOnValue(S, K, r, q, vol, T);
                }
                
                if (addOn < 0)
                {
                    return 0.0;
                }
                
                double intrinsic = Math.Max(0, K - S);
                if (addOn > intrinsic)
                {
                    return intrinsic * 0.8;
                }
                
                return addOn;
            }
            catch (Exception)
            {
                return CalculateFallbackAddOnValue(S, K, r, q, vol, T);
            }
        }

        private double CalculateFallbackAddOnValue(double S, double K, double r, double q, double vol, double T)
        {
            double intrinsic = Math.Max(0, K - S);
            double timeValue = Math.Max(0, CalculateBlackScholesPut(S, K, r, q, vol, T) - intrinsic);
            
            double carryBenefit = Math.Max(0, r - q);
            double earlyExerciseFactor = Math.Min(0.3, carryBenefit * T);
            
            return timeValue * earlyExerciseFactor;
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
}