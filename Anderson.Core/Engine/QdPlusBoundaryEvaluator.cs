using System;
using Anderson.Distribution;

namespace Anderson.Engine
{
    /// <summary>
    /// Encapsulates the pure QD+ function and its derivatives.
    /// This class is a pure mathematical representation and does not include the root-finding subtractions.
    /// </summary>
    public class QdPlusBoundaryEvaluator
    {
        private readonly double _tau, _K, _sigma, _sigma2, _v, _r, _q;
        private readonly double _dr, _dq, _ddr;
        private readonly double _omega, _lambda, _lambdaPrime, _alpha, _beta;
        private readonly double _hardLowerBound, _hardUpperBound;
        private double _sc, _dp, _dm, _phi_dp, _npv, _theta, _charm, _Phi_dp, _Phi_dm;

        public QdPlusBoundaryEvaluator(double spot, double strike, double riskFreeRate, double dividendYield, double volatility, double timeToMaturity)
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
            _lambdaPrime = 2.0 * _ddr * _ddr / (_sigma2 * sqrt_term);
            double common_denom = 2.0 * _lambda + _omega - 1.0;
            _alpha = 2.0 * _dr / (_sigma2 * common_denom);
            _beta = _alpha * (_ddr + _lambdaPrime / common_denom) - _lambda;
            _hardUpperBound = _K;
            _hardLowerBound = 1e-5;
            _sc = -1;
        }

        // Returns the pure QD+(S) value
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

        // Returns the pure first derivative: d(QD+)/dS
        public double Derivative(double S)
        {
            PreCalculateIfNeeded(S);
            // This now correctly returns d(QD+)/dS WITHOUT the "- 1"
            return 1.0 - _dq * _Phi_dp + _dq / _v * _phi_dp + _beta * (1.0 - _dq * _Phi_dp) + _alpha / _dr * _charm;
        }

        // Returns the pure second derivative: d^2(QD+)/dS^2
        public double SecondDerivative(double S)
        {
            PreCalculateIfNeeded(S);
            double gamma = _phi_dp * _dq / (_v * S);
            double colour = gamma * (_q + (_r - _q) * _dp / _v + (1.0 - _dp * _dm) / (2.0 * _tau));
            return _dq * (_phi_dp / (S * _v) - _phi_dp * _dp / (S * _v * _v)) + _beta * gamma + _alpha / _dr * colour;
        }

        public double XMin() => _hardLowerBound;
        public double XMax() => _hardUpperBound;

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
}