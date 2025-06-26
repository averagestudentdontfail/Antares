using System;
using Anderson.Root;
using Anderson.Distribution;

namespace Anderson.Engine
{
    /// <summary>
    /// Encapsulates the QD+ implicit equation for the American option exercise boundary.
    /// The root of the function f(S) = QD+(S) - S = 0 gives the exercise boundary S*.
    /// This class provides the value and derivatives needed by root-finding solvers like Halley's method.
    /// Reference: Li, M. (2009), "Analytical Approximations for the Critical Stock Prices of American Options"
    /// </summary>
    public class QdPlusBoundaryEvaluator : IObjectiveFunctionWithSecondDerivative
    {
        private readonly double _tau, _K, _sigma, _sigma2, _v, _r, _q;
        private readonly double _dr, _dq, _ddr;
        private readonly double _omega, _lambda, _lambdaPrime, _alpha, _beta;
        private readonly double _xMax, _xMin;

        // Mutable state for caching calculations to avoid re-computation
        private double _sc; // Spot cache
        private double _dp, _dm, _phi_dp;
        private double _npv, _theta, _charm;
        private double _Phi_dp, _Phi_dm; // Cumulative distributions

        public QdPlusBoundaryEvaluator(double spot, double strike, double riskFreeRate, double dividendYield, double volatility, double timeToMaturity, double totalTime)
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

            // ddr is a stable calculation for r / (1 - exp(-r*tau))
            _ddr = (Math.Abs(_r * _tau) > 1e-5)
                ? _r / (1.0 - _dr)
                : 1.0 / (_tau * (1.0 - 0.5 * _r * _tau * (1.0 - _r * _tau / 3.0)));

            _omega = 2.0 * (_r - _q) / _sigma2;
            
            double sqrt_term = Math.Sqrt(Math.Pow(_omega - 1, 2) + 8.0 * _ddr / _sigma2);
            _lambda = 0.5 * (-(_omega - 1.0) - sqrt_term);
            _lambdaPrime = 2.0 * _ddr * _ddr / (_sigma2 * sqrt_term);

            double common_denom = 2.0 * _lambda + _omega - 1.0;
            _alpha = 2.0 * _dr / (_sigma2 * common_denom);
            _beta = _alpha * (_ddr + _lambdaPrime / common_denom) - _lambda;

            _xMax = QdPlusAmericanEngine.XMax(_K, _r, _q);
            // Define a practical minimum bound to prevent solver from going to zero
            _xMin = 1e-4 * Math.Min(0.5 * (strike + spot), _xMax);
            
            _sc = -1; // Initialize cache as invalid
        }

        // The QD+ function: QD+(S)
        private double QdPlusValue(double S)
        {
            PreCalculateIfNeeded(S);
            
            if (Math.Abs(_K - S - _npv) < 1e-12)
            {
                // Handle near-zero denominator case
                return (1.0 - _dq * _Phi_dp) * S + _alpha * _theta / _dr;
            }
            
            double c0 = -_beta - _lambda + _alpha * _theta / (_dr * (_K - S - _npv));
            return (1.0 - _dq * _Phi_dp) * S + (c0 + _lambda) * (_K - S - _npv);
        }

        // Implementation of IObjectiveFunction: f(S) = QD+(S) - S
        public double Value(double S) => QdPlusValue(S) - S;
        
        public double Derivative(double S)
        {
            PreCalculateIfNeeded(S);
            // Derivative of (QD+(S) - S) is (d(QD+)/dS - 1)
            return (1.0 - _dq * _Phi_dp + _dq / _v * _phi_dp + _beta * (1.0 - _dq * _Phi_dp) + _alpha / _dr * _charm) - 1.0;
        }

        public double SecondDerivative(double S)
        {
            PreCalculateIfNeeded(S);
            double gamma = _phi_dp * _dq / (_v * S);
            double colour = gamma * (_q + (_r - _q) * _dp / _v + (1.0 - _dp * _dm) / (2.0 * _tau));
            return _dq * (_phi_dp / (S * _v) - _phi_dp * _dp / (S * _v * _v)) + _beta * gamma + _alpha / _dr * colour;
        }

        public double XMin() => _xMin;
        public double XMax() => _xMax;

        private void PreCalculateIfNeeded(double S)
        {
            if (Math.Abs(S - _sc) > 1e-12)
            {
                _sc = Math.Max(1e-12, S); // Ensure spot is positive
                
                _dp = (Math.Log(_sc * _dq / (_K * _dr)) / _v) + 0.5 * _v;
                _dm = _dp - _v;

                _Phi_dp = Distributions.CumulativeNormal(-_dp);
                _Phi_dm = Distributions.CumulativeNormal(-_dm);
                _phi_dp = Distributions.NormalDensity(_dp);

                // European put price
                _npv = _dr * _K * _Phi_dm - _sc * _dq * _Phi_dp;
                
                // European put theta
                _theta = _r * _K * _dr * _Phi_dm - _q * _sc * _dq * _Phi_dp - _sigma2 * _sc / (2.0 * _v) * _dq * _phi_dp;
                
                // European put charm (dDelta/dExpiry)
                _charm = -_dq * (_phi_dp * ((_r - _q) / _v - _dm / (2.0 * _tau)) + _q * _Phi_dp);
            }
        }
    }
}