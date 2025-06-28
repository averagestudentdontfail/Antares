using System;
using Antares.Distribution;

namespace Antares.Engine
{
    public class QdPlusBoundaryEvaluator
    {
        private readonly double _tau, _K, _sigma, _sigma2, _v, _r, _q;
        private readonly double _dr, _dq, _ddr;
        private readonly double _omega, _lambda, _lambdaPrime, _alpha, _beta;
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
            
            // Improved calculation of _ddr for numerical stability
            if (Math.Abs(_r * _tau) > 1e-6)
            {
                _ddr = _r / (1.0 - _dr);
            }
            else
            {
                // Taylor expansion for small r*tau
                double rt = _r * _tau;
                _ddr = 1.0 / (_tau * (1.0 - 0.5 * rt * (1.0 - rt / 3.0 * (1.0 - 0.25 * rt))));
            }
            
            _omega = 2.0 * (_r - _q) / _sigma2;
            double discriminant = Math.Pow(_omega - 1, 2) + 8.0 * _ddr / _sigma2;
            
            if (discriminant < 0)
            {
                // Fallback for negative discriminant
                _lambda = -0.5 * (_omega - 1.0);
                _lambdaPrime = 0.0;
            }
            else
            {
                double sqrt_term = Math.Sqrt(discriminant);
                _lambda = 0.5 * (-(_omega - 1.0) - sqrt_term);
                _lambdaPrime = sqrt_term > 1e-12 ? 2.0 * _ddr * _ddr / (_sigma2 * sqrt_term) : 0.0;
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
            
            // Value-matching condition: V(tau, B(tau)) = K - B(tau)
            double continuation_value = _dr * _K * _Phi_dm - S * _dq * _Phi_dp;
            double exercise_value = _K - S;
            
            // The boundary equation: early exercise premium equals intrinsic advantage
            if (Math.Abs(_K - S - _npv) < 1e-12)
            {
                // At the boundary, smooth-pasting condition
                return (1.0 - _dq * _Phi_dp) * S + _alpha * _theta / _dr;
            }
            
            // General case: combine continuation and exercise values
            double c0 = -_beta - _lambda;
            if (Math.Abs(_K - S - _npv) > 1e-12)
            {
                c0 += _alpha * _theta / (_dr * (_K - S - _npv));
            }
            
            return (1.0 - _dq * _Phi_dp) * S + (c0 + _lambda) * (_K - S - _npv);
        }

        public double Derivative(double S)
        {
            PreCalculateIfNeeded(S);
            
            // Smooth-pasting condition: dV/dS = -1 at boundary
            double term1 = 1.0 - _dq * _Phi_dp;
            double term2 = _dq / _v * _phi_dp;
            double term3 = _beta * (1.0 - _dq * _Phi_dp);
            double term4 = _alpha / _dr * _charm;
            
            return term1 + term2 + term3 + term4;
        }

        public double SecondDerivative(double S)
        {
            PreCalculateIfNeeded(S);
            
            double gamma = _phi_dp * _dq / (_v * S);
            double colour = gamma * (_q + (_r - _q) * _dp / _v + (1.0 - _dp * _dm) / (2.0 * _tau));
            
            double term1 = _dq * (_phi_dp / (S * _v) - _phi_dp * _dp / (S * _v * _v));
            double term2 = _beta * gamma;
            double term3 = _alpha / _dr * colour;
            
            return term1 + term2 + term3;
        }

        public double XMin() => Math.Max(1e-8, _K * 1e-6);
        public double XMax() => _K * 0.999;

        private void PreCalculateIfNeeded(double S)
        {
            if (Math.Abs(S - _sc) > 1e-12)
            {
                _sc = Math.Max(1e-12, S);
                
                // Black-Scholes d+ and d- parameters
                if (_v > 1e-12)
                {
                    double logRatio = Math.Log(_sc * _dq / (_K * _dr));
                    _dp = logRatio / _v + 0.5 * _v;
                    _dm = _dp - _v;
                }
                else
                {
                    // Handle very small volatility
                    double logRatio = Math.Log(_sc * _dq / (_K * _dr));
                    _dp = logRatio > 0 ? double.PositiveInfinity : double.NegativeInfinity;
                    _dm = _dp;
                }
                
                _Phi_dp = Distributions.CumulativeNormal(-_dp);
                _Phi_dm = Distributions.CumulativeNormal(-_dm);
                _phi_dp = Distributions.NormalDensity(_dp);
                
                // Net present value of option at current boundary
                _npv = _dr * _K * _Phi_dm - _sc * _dq * _Phi_dp;
                
                // Theta calculation (time decay)
                _theta = _r * _K * _dr * _Phi_dm - _q * _sc * _dq * _Phi_dp;
                if (_v > 1e-12)
                {
                    _theta -= _sigma2 * _sc / (2.0 * _v) * _dq * _phi_dp;
                }
                
                // Charm calculation (delta decay)
                if (_v > 1e-12)
                {
                    _charm = -_dq * (_phi_dp * ((_r - _q) / _v - _dm / (2.0 * _tau)) + _q * _Phi_dp);
                }
                else
                {
                    _charm = -_q * _dq * _Phi_dp;
                }
            }
        }
    }
}