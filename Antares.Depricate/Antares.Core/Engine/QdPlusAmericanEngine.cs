using System;
using Antares.Interpolation.Interpolators;
using Antares.Root;
using Antares.Distribution;

namespace Antares.Engine
{
    public class QdPlusAmericanEngine
    {
        private readonly int _interpolationPoints;
        private readonly ISolver _solver;
        private readonly double _epsilon;

        public QdPlusAmericanEngine(int interpolationPoints = 8, double epsilon = 1e-6)
        {
            _interpolationPoints = interpolationPoints;
            _epsilon = epsilon;
            _solver = new BrentSolver();
        }

        public double CalculatePut(double S, double K, double r, double q, double vol, double T)
        {
            if (T < 1e-9) return Math.Max(0.0, K - S);

            double xmax = CalculateAsymptoticBoundary(K, r, q, vol);
            var boundaryInterp = GetPutExerciseBoundary(K, r, q, vol, T, xmax);
            
            var addOnCalculator = new QdPlusAddOnValue(T, S, K, r, q, vol, xmax, boundaryInterp);
            double addOnValue = addOnCalculator.CalculateTotal();
            
            double europeanValue = CalculateBlackScholesPut(S, K, r, q, vol, T);
            double intrinsicValue = Math.Max(0.0, K - S);
            
            double americanPrice = europeanValue + Math.Max(0.0, addOnValue);
            return Math.Max(intrinsicValue, americanPrice);
        }

        private ChebyshevInterpolation GetPutExerciseBoundary(double K, double r, double q, double vol, double T, double xmax)
        {
            Func<double, double> functionToInterpolate = z =>
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
                
                double boundary = PutExerciseBoundaryAtTau(K, r, q, vol, tau, xmax);
                
                double boundaryRatio = Math.Max(1e-12, boundary) / xmax;
                boundaryRatio = Math.Max(1e-8, Math.Min(1.0 - 1e-8, boundaryRatio));
                double G = Math.Log(boundaryRatio);
                return Math.Max(0.0, G * G);
            };
            
            return new ChebyshevInterpolation(_interpolationPoints, functionToInterpolate);
        }

        private double PutExerciseBoundaryAtTau(double K, double r, double q, double vol, double tau, double xmax)
        {
            if (tau < 1e-12) 
            {
                return r >= q ? K : K * Math.Max(0.1, Math.Min(0.9, Math.Abs(r/q)));
            }
            
            if (tau > 10.0)
            {
                return CalculateAsymptoticBoundary(K, r, q, vol);
            }
            
            var evaluator = new QdPlusBoundaryEvaluator(K, r, q, vol, tau);
            var solverFunction = new SolverFunction(evaluator);
            
            double asymptoticBoundary = CalculateAsymptoticBoundary(K, r, q, vol);
            double nearExpiryBoundary = r >= q ? K : K * Math.Max(0.1, Math.Min(0.9, Math.Abs(r/q)));
            double weight = Math.Min(1.0, tau * 4.0);
            double initialGuess = nearExpiryBoundary * (1.0 - weight) + asymptoticBoundary * weight;
            
            double xmin = evaluator.XMin();
            double xmax_bound = evaluator.XMax();
            initialGuess = Math.Max(xmin + 1e-6, Math.Min(xmax_bound - 1e-6, initialGuess));
            
            try
            {
                return _solver.Solve(evaluator, _epsilon, initialGuess, xmin, xmax_bound);
            }
            catch (Exception)
            {
                return initialGuess;
            }
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
            double discriminant_std = mu_std * mu_std + 2.0 * r * vol * vol;
            
            if (discriminant_std <= 0) 
            {
                return K * 0.7;
            }
            
            double lambda_minus_std = (mu_std - Math.Sqrt(discriminant_std)) / (vol * vol);
            
            if (lambda_minus_std >= -1e-6)
            {
                return K * 0.7;
            }
            
            double boundary_std = K * lambda_minus_std / (lambda_minus_std - 1.0);
            return Math.Max(K * 0.1, Math.Min(K * 0.9, boundary_std));
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