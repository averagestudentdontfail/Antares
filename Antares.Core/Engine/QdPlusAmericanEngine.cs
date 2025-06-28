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
            if (Math.Abs(q) < 1e-12)
            {
                return r > 0.0 ? 0.0 : K;
            }
            
            if (q > 0.0)
            {
                double ratio = r / q;
                // Prevent very small ratios that cause numerical instability
                if (ratio < 0.05)
                    return K * Math.Max(0.05, ratio);
                return K * Math.Min(1.0, ratio);
            }
            else
            {
                return K;
            }
        }

        public ChebyshevInterpolation GetPutExerciseBoundary(double K, double r, double q, double vol, double T)
        {
            double xmax = XMax(K, r, q);
            if (xmax <= K * 1e-6)
            {
                return new ChebyshevInterpolation(_interpolationPoints, z => 1e12);
            }

            double sqrtT = Math.Sqrt(T);
            Func<double, double> functionToInterpolate = z =>
            {
                double x_val = 0.5 * sqrtT * (1.0 + z);
                double tau = x_val * x_val;
                
                double boundary = PutExerciseBoundaryAtTau(K, r, q, vol, tau);
                
                double ratio = Math.Max(1e-12, boundary) / xmax;
                ratio = Math.Min(ratio, 1.0 - 1e-12);
                return Math.Sqrt(Math.Max(0.0, -Math.Log(ratio)));
            };
            
            return new ChebyshevInterpolation(_interpolationPoints, functionToInterpolate);
        }

        private double PutExerciseBoundaryAtTau(double K, double r, double q, double vol, double tau)
        {
            if (tau < 1e-9) return XMax(K, r, q);
            
            var evaluator = new QdPlusBoundaryEvaluator(K, r, q, vol, tau);
            var solverFunction = new SolverFunction(evaluator);
            
            double initialGuess = XMax(K, r, q);
            if (initialGuess <= evaluator.XMin() || initialGuess >= K)
            {
                 initialGuess = K * 0.95;
            }
            
            try
            {
                var brent = new Brent();
                return brent.Solve(solverFunction, _epsilon, initialGuess, evaluator.XMin(), K);
            }
            catch (Exception)
            {
                return K;
            }
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
                _ddr = (Math.Abs(_r * _tau) > 1e-5) ? _r / (1.0 - _dr) : 1.0 / (_tau * (1.0 - 0.5 * _r * _tau * (1.0 - _r * _tau / 3.0)));
                _omega = 2.0 * (_r - _q) / _sigma2;
                double sqrt_term = Math.Sqrt(Math.Pow(_omega - 1, 2) + 8.0 * _ddr / _sigma2);
                _lambda = 0.5 * (-(_omega - 1.0) - sqrt_term);
                _lambdaPrime = (sqrt_term > 1e-9) ? 2.0 * _ddr * _ddr / (_sigma2 * sqrt_term) : 0.0;
                double common_denom = 2.0 * _lambda + _omega - 1.0;
                _alpha = (Math.Abs(common_denom) > 1e-9) ? 2.0 * _dr / (_sigma2 * common_denom) : 0.0;
                _beta = _alpha * (_ddr + ((Math.Abs(common_denom) > 1e-9) ? _lambdaPrime / common_denom : 0.0)) - _lambda;
                _sc = -1;
            }

            public double Value(double S)
            {
                PreCalculateIfNeeded(S);
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

            public double XMin() => 1e-5;

            private void PreCalculateIfNeeded(double S)
            {
                if (Math.Abs(S - _sc) > 1e-12)
                {
                    _sc = Math.Max(1e-12, S);
                    _dp = (Math.Log(_sc * _dq / (_K * _dr)) / _v) + 0.5 * _v;
                    _dm = _dp - _v;
                    _Phi_dp = Distributions.CumulativeNormal(-_dp);
                    _Phi_dm = Distributions.CumulativeNormal(-_dm);
                    _phi_dp = Distributions.NormalDensity(_dp);
                    _npv = _dr * _K * _Phi_dm - _sc * _dq * _Phi_dp;
                    _theta = _r * _K * _dr * _Phi_dm - _q * _sc * _dq * _Phi_dp - _sigma2 * _sc / (2.0 * _v) * _dq * _phi_dp;
                    _charm = -_dq * (_phi_dp * ((_r - _q) / _v - _dm / (2.0 * _tau)) + _q * _Phi_dp);
                }
            }
        }

        private class SolverFunction : IObjectiveFunctionWithSecondDerivative
        {
            private readonly QdPlusBoundaryEvaluator _eval;
            public SolverFunction(QdPlusBoundaryEvaluator evaluator) { _eval = evaluator; }
            public double Value(double x) => _eval.Value(x) - x;
            public double Derivative(double x) => _eval.Derivative(x) - 1.0;
            public double SecondDerivative(double x) => _eval.SecondDerivative(x);
            public double XMin() => _eval.XMin();
            public double XMax() => _eval.XMin();
        }
    }
}