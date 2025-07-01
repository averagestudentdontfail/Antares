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

            double xmax = CalculateAsymptoticBoundary(K, r, q, vol);
            
            if (xmax <= K * 0.001)
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            ChebyshevInterpolation boundaryInterp;
            try
            {
                boundaryInterp = InitializeBoundaryInterpolation(K, r, q, vol, T, xmax);
            }
            catch (Exception)
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            var framework = new SpCollocation(T, xmax, K, r, q, vol);
            bool useFP_A = DetermineOptimalEquation(r, q, vol, T, xmax, K);

            DqFpEquation equation = useFP_A
                ? new DqFpEquation_A(K, r, q, vol, framework.GetBoundaryFunction(boundaryInterp), _scheme.GetFixedPointIntegrator())
                : new DqFpEquation_B(K, r, q, vol, framework.GetBoundaryFunction(boundaryInterp), _scheme.GetFixedPointIntegrator());

            bool converged = SolveFixedPoint(equation, boundaryInterp, framework);
            
            if (!converged)
            {
                converged = AttemptEnhancedRecovery(equation, boundaryInterp, framework);
                
                if (!converged)
                {
                    double europeanBase = CalculateBlackScholesPut(S, K, r, q, vol, T);
                    double intrinsic = Math.Max(0.0, K - S);
                    double conservativePremium = EstimateConservativePremium(S, K, r, q, vol, T);
                    return Math.Max(intrinsic, europeanBase + conservativePremium);
                }
            }

            double addOnValue = CalculateSpectralAddOnValue(S, K, r, q, vol, T, xmax, boundaryInterp);
            double europeanValue = CalculateBlackScholesPut(S, K, r, q, vol, T);
            double intrinsicValue = Math.Max(0.0, K - S);

            double americanPrice = europeanValue + Math.Max(0.0, addOnValue);
            return Math.Max(intrinsicValue, americanPrice);
        }

        private double CalculateAsymptoticBoundary(double K, double r, double q, double vol)
        {
            if (Math.Abs(q) < 1e-12)
            {
                return r <= 0 ? K * 0.9 : K * (r > 0 ? r/(r + 0.5*vol*vol) : 0.1);
            }

            if (r <= q)
            {
                double mu = r - q - 0.5 * vol * vol;
                double discriminant = mu * mu + 2.0 * r * vol * vol;
                
                if (discriminant > 0 && r > 0)
                {
                    double lambda_minus = (mu - Math.Sqrt(discriminant)) / (vol * vol);
                    if (lambda_minus < -1e-6)
                    {
                        double boundary = K * lambda_minus / (lambda_minus - 1.0);
                        return Math.Max(K * 0.1, Math.Min(K * 0.9, boundary));
                    }
                }
                
                double ratio = Math.Max(0.1, Math.Min(0.8, Math.Pow(Math.Abs(r/q), 0.5)));
                return K * ratio;
            }

            double mu_std = r - q - 0.5 * vol * vol;
            double disc_std = mu_std * mu_std + 2.0 * r * vol * vol;
            
            if (disc_std <= 0) return K * 0.7;
            
            double lambda_minus_std = (mu_std - Math.Sqrt(disc_std)) / (vol * vol);
            if (lambda_minus_std >= -1e-6) return K * 0.7;
            
            double boundary_std = K * lambda_minus_std / (lambda_minus_std - 1.0);
            return Math.Max(K * 0.1, Math.Min(K * 0.9, boundary_std));
        }

        private ChebyshevInterpolation InitializeBoundaryInterpolation(double K, double r, double q, double vol, double T, double xmax)
        {
            int n = _scheme.GetNumberOfChebyshevInterpolationNodes();
            
            Func<double, double> initialBoundaryFunction = z =>
            {
                double xi = 0.5 * (1.0 + z);
                double tau = xi * xi * T;
                
                if (tau < 1e-12)
                {
                    double nearExpiryBoundary = r >= q ? K : K * Math.Max(0.1, Math.Min(0.9, Math.Abs(r/q)));
                    double nearExpiryRatio = Math.Max(1e-12, nearExpiryBoundary) / xmax;
                    nearExpiryRatio = Math.Max(1e-8, Math.Min(1.0 - 1e-8, nearExpiryRatio));
                    return Math.Sqrt(Math.Max(0.0, -Math.Log(nearExpiryRatio)));
                }
                
                double asymptoticBoundary = CalculateAsymptoticBoundary(K, r, q, vol);
                double nearExpiryBoundary2 = r >= q ? K : K * Math.Max(0.1, Math.Min(0.9, Math.Abs(r/q)));
                
                double weight = Math.Min(1.0, tau / T);
                double boundary = nearExpiryBoundary2 * (1.0 - weight) + asymptoticBoundary * weight;
                
                double finalRatio = Math.Max(1e-12, boundary) / xmax;
                finalRatio = Math.Max(1e-8, Math.Min(1.0 - 1e-8, finalRatio));
                double G = Math.Log(finalRatio);
                return Math.Max(0.0, G * G);
            };
            
            return new ChebyshevInterpolation(n, initialBoundaryFunction);
        }

        private bool DetermineOptimalEquation(double r, double q, double vol, double T, double xmax, double K)
        {
            if (_fpEquation == FixedPointEquation.FP_A) return true;
            if (_fpEquation == FixedPointEquation.FP_B) return false;

            double rqDiff = Math.Abs(r - q);
            
            if (rqDiff < 0.001)
            {
                return true;
            }
            
            if (rqDiff < 0.05 && vol > 0.1 && T > 0.01)
            {
                return true;
            }
            
            return false;
        }

        private bool SolveFixedPoint(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            const double tolerance = 1e-8;
            const double stagnationTolerance = 1e-10;
            double[] errorHistory = new double[5];
            int stagnationCount = 0;

            for (int iter = 0; iter < _scheme.GetNumberOfJacobiNewtonFixedPointSteps(); iter++)
            {
                double maxError = ExecuteJacobiNewtonStep(equation, boundaryInterp, framework);

                if (maxError < tolerance)
                {
                    return ValidateAmericanBehavior(boundaryInterp, framework);
                }

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

            for (int iter = 0; iter < _scheme.GetNumberOfNaiveFixedPointSteps(); iter++)
            {
                double maxError = ExecuteNaiveFixedPointStep(equation, boundaryInterp, framework);

                if (maxError < tolerance)
                {
                    return ValidateAmericanBehavior(boundaryInterp, framework);
                }
            }

            return false;
        }

        private bool ValidateAmericanBehavior(ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            double[] testTaus = {0.01, 0.1, 0.5};
            foreach (double tau in testTaus)
            {
                if (tau < framework.T)
                {
                    double boundary = framework.GetBoundaryFunction(boundaryInterp)(tau);
                    
                    if (boundary >= framework.K * 0.999)
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        private double ExecuteJacobiNewtonStep(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            double[] nodes = boundaryInterp.Nodes();
            double[] hValues = boundaryInterp.Values();
            double maxError = 0.0;

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
                
                double alpha = framework.K * Math.Exp(-(framework.r - framework.q) * tau);
                double jacobian = alpha * (Nd * D - Dd * N) / (D * D);
                
                double dampingFactor = Math.Min(0.7, 1.0 / (1.0 + Math.Abs(jacobian) * 0.1));
                double newBoundary = Fv - dampingFactor * (N / D - boundaryValue) / (1.0 - jacobian * dampingFactor);
                
                newBoundary = Math.Max(framework.K * 1e-6, Math.Min(framework.K * 0.999, newBoundary));
                
                double newH = framework.TransformFromBoundary(newBoundary, tau);
                double error = Math.Abs(newH - hValues[i]);
                maxError = Math.Max(maxError, error);
                
                hValues[i] = newH;
            }

            boundaryInterp.UpdateValues(hValues);
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
                
                double boundaryValue = framework.TransformToBoundary(hValues[i], tau);
                var (N, D, Fv) = equation.F(tau, boundaryValue);

                double newBoundary = Math.Abs(D) > 1e-12 ? Fv : boundaryValue;
                newBoundary = Math.Max(framework.K * 1e-6, Math.Min(framework.K * 0.999, newBoundary));
                
                double newH = framework.TransformFromBoundary(newBoundary, tau);
                double error = Math.Abs(newH - hValues[i]);
                maxError = Math.Max(maxError, error);
                
                hValues[i] = newH;
            }

            boundaryInterp.UpdateValues(hValues);
            return maxError;
        }

        private bool AttemptEnhancedRecovery(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, SpCollocation framework)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                ReinitializeBoundaryWithPerturbation(boundaryInterp, framework, attempt * 0.1);
                
                if (SolveFixedPoint(equation, boundaryInterp, framework))
                {
                    return true;
                }
            }
            
            return false;
        }

        private void ReinitializeBoundaryWithPerturbation(ChebyshevInterpolation boundaryInterp, SpCollocation framework, double perturbation)
        {
            double[] hValues = boundaryInterp.Values();
            
            for (int i = 0; i < hValues.Length; i++)
            {
                hValues[i] *= (1.0 + perturbation * (2.0 * new Random().NextDouble() - 1.0));
            }
            
            boundaryInterp.UpdateValues(hValues);
        }

        private double EstimateConservativePremium(double S, double K, double r, double q, double vol, double T)
        {
            if (r <= q) return 0.0;
            
            double moneyness = S / K;
            double timeValue = vol * Math.Sqrt(T);
            double carryBenefit = (r - q) * T;
            
            return Math.Max(0.0, K * 0.01 * carryBenefit * timeValue * Math.Max(0.0, 1.0 - moneyness));
        }

        private double CalculateSpectralAddOnValue(double S, double K, double r, double q, double vol, double T, double xmax, ChebyshevInterpolation boundaryInterp)
        {
            var spectralIntegrator = _scheme.GetExerciseBoundaryToPriceIntegrator();
            var framework = new SpCollocation(T, xmax, K, r, q, vol);
            var boundaryFunction = framework.GetBoundaryFunction(boundaryInterp);
            
            var addOnCalculator = new QdPlusAddOnValue(T, S, K, r, q, vol, xmax, boundaryFunction);
            
            try
            {
                return spectralIntegrator.Integrate(addOnCalculator.Evaluate, 0.0, 1.0);
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        private double CalculateBlackScholesPut(double S, double K, double r, double q, double vol, double T)
        {
            if (T <= 0) return Math.Max(0.0, K - S);
            
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * Math.Sqrt(T));
            double d2 = d1 - vol * Math.Sqrt(T);
            
            var norm = new StandardNormalDistribution();
            
            return K * Math.Exp(-r * T) * norm.CumulativeDistribution(-d2) - 
                   S * Math.Exp(-q * T) * norm.CumulativeDistribution(-d1);
        }
    }
}