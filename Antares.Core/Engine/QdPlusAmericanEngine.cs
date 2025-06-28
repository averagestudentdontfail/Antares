using System;
using Antares.Root;
using Antares.Root.Solvers;
using Antares.Interpolation.Interpolators;
using Antares.Distribution;

namespace Antares.Engine
{
    public class QdPlusAmericanEngine
    {
        public enum SolverType { Halley, Brent }

        private readonly ISolver1D _solver;
        private readonly int _interpolationPoints;
        private readonly double _epsilon;

        public QdPlusAmericanEngine(
            int interpolationPoints = 8,
            SolverType solverType = SolverType.Halley,
            double epsilon = 1e-8)
        {
            _interpolationPoints = interpolationPoints;
            _epsilon = epsilon;

            _solver = solverType switch
            {
                SolverType.Halley => new Halley { MaxEvaluations = 50 },
                SolverType.Brent => new Brent { MaxEvaluations = 100 },
                _ => throw new ArgumentOutOfRangeException(nameof(solverType))
            };
        }

        public static double XMax(double K, double r, double q)
        {
            // Improved XMax calculation based on asymptotic analysis
            if (Math.Abs(q) < 1e-12)
            {
                return r <= 0 ? K * 0.9 : K * 0.01;
            }
            
            if (r <= q)
            {
                // When r <= q, the optimal exercise boundary
                double ratio = Math.Max(0.001, Math.Min(0.999, r / q));
                return K * ratio;
            }
            else
            {
                // When r > q, use perpetual option formula as guide
                // For small r-q, boundary approaches K
                // For large r-q, boundary decreases
                double rqRatio = (r - q) / Math.Max(q, 0.01);
                double boundaryFactor = 1.0 / (1.0 + rqRatio);
                return K * Math.Max(0.05, Math.Min(0.95, boundaryFactor));
            }
        }

        public ChebyshevInterpolation GetPutExerciseBoundary(double K, double r, double q, double vol, double T)
        {
            double xmax = XMax(K, r, q);
            
            // Ensure xmax is reasonable
            if (xmax <= K * 1e-6 || xmax >= K * 0.999)
            {
                xmax = K * 0.8;
            }

            double sqrtT = Math.Sqrt(T);
            
            Func<double, double> functionToInterpolate = z =>
            {
                // Map from [-1,1] to [0,T] using quadratic transformation
                double xi = 0.5 * (1.0 + z);
                double tau = xi * xi * T;
                
                if (tau < 1e-12)
                {
                    // Near expiration, boundary approaches appropriate limit
                    double nearExpiryBoundary = r >= q ? K : K * r / q;
                    double nearExpiryRatio = Math.Max(1e-12, nearExpiryBoundary) / xmax;
                    nearExpiryRatio = Math.Max(1e-8, Math.Min(1.0 - 1e-8, nearExpiryRatio));
                    return Math.Sqrt(Math.Max(0.0, -Math.Log(nearExpiryRatio)));
                }
                
                double boundary = PutExerciseBoundaryAtTau(K, r, q, vol, tau, xmax);
                
                // Transform boundary to H-space using variance-stabilizing transformation
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
                return r >= q ? K : K * r / q;
            }
            
            // For very long times, approach asymptotic boundary
            if (tau > 10.0)
            {
                return CalculateAsymptoticBoundary(K, r, q, vol);
            }
            
            var evaluator = new QdPlusBoundaryEvaluator(K, r, q, vol, tau);
            var solverFunction = new SolverFunction(evaluator);
            
            // Better initial guess based on time interpolation
            double asymptoticBoundary = CalculateAsymptoticBoundary(K, r, q, vol);
            double nearExpiryBoundary = r >= q ? K : K * r / q;
            double weight = Math.Min(1.0, tau * 4.0); // Adjust time scale
            double initialGuess = nearExpiryBoundary * (1.0 - weight) + asymptoticBoundary * weight;
            
            // Ensure initial guess is within bounds
            double xmin = evaluator.XMin();
            double xmax_bound = evaluator.XMax();
            initialGuess = Math.Max(xmin + 1e-6, Math.Min(xmax_bound - 1e-6, initialGuess));
            
            try
            {
                return _solver.Solve(solverFunction, _epsilon, initialGuess, xmin, xmax_bound);
            }
            catch (Exception)
            {
                // Fallback: return interpolated boundary
                return initialGuess;
            }
        }
        
        private double CalculateAsymptoticBoundary(double K, double r, double q, double vol)
        {
            if (r <= q)
            {
                return K * Math.Max(0.01, Math.Min(0.99, r / q));
            }
            
            // Perpetual American put boundary
            double mu = r - q - 0.5 * vol * vol;
            double discriminant = mu * mu + 2.0 * r * vol * vol;
            
            if (discriminant <= 0)
            {
                return K * 0.7;
            }
            
            double lambda_minus = (mu - Math.Sqrt(discriminant)) / (vol * vol);
            
            if (lambda_minus >= -1e-6)
            {
                return K * 0.7;
            }
            
            double boundary = K * lambda_minus / (lambda_minus - 1.0);
            return Math.Max(K * 0.01, Math.Min(K * 0.95, boundary));
        }
        
        private class QdPlusBoundaryEvaluator
        {
            private readonly double _tau, _K, _sigma, _sigma2, _v, _r, _q;
            private readonly double _dr, _dq, _ddr;
            private readonly double _omega, _lambda, _lambdaPrime, _alpha, _beta;
            private double _sc, _dp, _dm, _phi_dp, _npv, _theta, _charm, _Phi_dp, _Phi_dm;

            public QdPlusBoundaryEvaluator(double strike, double riskFreeRate, double dividendYield, double volatility, double timeToMaturity)
            {
                _tau = timeToMaturity;
                _K = strike;
                _sigma = volatility;
                _sigma2 = _sigma * _sigma;
                _v = _sigma * Math.Sqrt(_tau);
                _r = riskFreeRate;
                _q = dividendYield;
                _dr = Math.Exp(-_r * _tau);
                _dq = Math.Exp(-_q * _tau);
                
                // Improved calculation for numerical stability
                if (Math.Abs(_r * _tau) > 1e-6)
                {
                    _ddr = _r / (1.0 - _dr);
                }
                else
                {
                    double rt = _r * _tau;
                    _ddr = 1.0 / (_tau * (1.0 - 0.5 * rt * (1.0 - rt / 3.0)));
                }
                
                _omega = 2.0 * (_r - _q) / _sigma2;
                double discriminant = Math.Pow(_omega - 1, 2) + 8.0 * _ddr / _sigma2;
                
                if (discriminant >= 0)
                {
                    double sqrt_term = Math.Sqrt(discriminant);
                    _lambda = 0.5 * (-(_omega - 1.0) - sqrt_term);
                    _lambdaPrime = sqrt_term > 1e-12 ? 2.0 * _ddr * _ddr / (_sigma2 * sqrt_term) : 0.0;
                }
                else
                {
                    _lambda = -0.5 * (_omega - 1.0);
                    _lambdaPrime = 0.0;
                }
                
                double common_denom = 2.0 * _lambda + _omega - 1.0;
                
                if (Math.Abs(common_denom) > 1e-12)
                {
                    _alpha = 2.0 * _dr / (_sigma2 * common_denom);
                    _beta = _alpha * (_ddr + _lambdaPrime / common_denom) - _lambda;
                }
                else
                {
                    _alpha = 0.0;
                    _beta = -_lambda;
                }
                
                _sc = -1;
            }

            public double Value(double S)
            {
                PreCalculateIfNeeded(S);
                
                // Implement the value-matching condition for the boundary
                if (Math.Abs(_K - S - _npv) < 1e-12)
                {
                    return (1.0 - _dq * _Phi_dp) * S + _alpha * _theta / _dr;
                }
                
                double c0 = -_beta - _lambda + _alpha * _theta / (_dr * (_K - S - _npv));
                return (1.0 - _dq * _Phi_dp) * S + (c0 + _lambda) * (_K - S - _npv);
            }

            public double Derivative(double S)
            {
                PreCalculateIfNeeded(S);
                return 1.0 - _dq * _Phi_dp + _dq / _v * _phi_dp + _beta * (1.0 - _dq * _Phi_dp) + _alpha / _dr * _charm;
            }

            public double SecondDerivative(double S)
            {
                PreCalculateIfNeeded(S);
                double gamma = _phi_dp * _dq / (_v * S);
                double colour = gamma * (_q + (_r - _q) * _dp / _v + (1.0 - _dp * _dm) / (2.0 * _tau));
                return _dq * (_phi_dp / (S * _v) - _phi_dp * _dp / (S * _v * _v)) + _beta * gamma + _alpha / _dr * colour;
            }

            public double XMin() => Math.Max(1e-8, _K * 1e-6);
            public double XMax() => _K * 0.999;

            private void PreCalculateIfNeeded(double S)
            {
                if (Math.Abs(S - _sc) > 1e-12)
                {
                    _sc = Math.Max(1e-12, S);
                    
                    if (_v > 1e-12)
                    {
                        _dp = (Math.Log(_sc * _dq / (_K * _dr)) / _v) + 0.5 * _v;
                        _dm = _dp - _v;
                    }
                    else
                    {
                        double logRatio = Math.Log(_sc * _dq / (_K * _dr));
                        _dp = logRatio > 0 ? double.PositiveInfinity : double.NegativeInfinity;
                        _dm = _dp;
                    }
                    
                    _Phi_dp = Distributions.CumulativeNormal(-_dp);
                    _Phi_dm = Distributions.CumulativeNormal(-_dm);
                    _phi_dp = Distributions.NormalDensity(_dp);
                    _npv = _dr * _K * _Phi_dm - _sc * _dq * _Phi_dp;
                    _theta = _r * _K * _dr * _Phi_dm - _q * _sc * _dq * _Phi_dp;
                    
                    if (_v > 1e-12)
                    {
                        _theta -= _sigma2 * _sc / (2.0 * _v) * _dq * _phi_dp;
                        _charm = -_dq * (_phi_dp * ((_r - _q) / _v - _dm / (2.0 * _tau)) + _q * _Phi_dp);
                    }
                    else
                    {
                        _charm = -_q * _dq * _Phi_dp;
                    }
                }
            }
        }

        private class SolverFunction : IObjectiveFunctionWithSecondDerivative
        {
            private readonly QdPlusBoundaryEvaluator _eval;
            
            public SolverFunction(QdPlusBoundaryEvaluator evaluator) 
            { 
                _eval = evaluator; 
            }
            
            public double Value(double x) => _eval.Value(x) - x;
            public double Derivative(double x) => _eval.Derivative(x) - 1.0;
            public double SecondDerivative(double x) => _eval.SecondDerivative(x);
        }
    }
}