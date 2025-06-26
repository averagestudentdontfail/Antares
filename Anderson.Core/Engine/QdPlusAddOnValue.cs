using System;
using Anderson.Distribution;
using Anderson.Interpolation.Interpolators; // Correct using statement

namespace Anderson.Engine
{
    /// <summary>
    /// Represents the integrand for calculating the early exercise premium of an American put option.
    /// The integration is performed over a transformed time variable z = sqrt(τ).
    /// </summary>
    public class QdPlusAddOnValue
    {
        private readonly double _T, _T_sqrt, _S, _K, _xmax, _r, _q, _vol, _vol2;
        // CORRECTED: Use the concrete ChebyshevInterpolation type to avoid ambiguity.
        private readonly ChebyshevInterpolation _boundaryInterpolation;

        public QdPlusAddOnValue(double T, double S, double K, double r, double q, double vol, double xmax, ChebyshevInterpolation boundaryInterpolation)
        {
            _T = T;
            _T_sqrt = Math.Sqrt(T);
            _S = S; _K = K; _r = r; _q = q; _vol = vol; _vol2 = vol * vol;
            _xmax = xmax;
            _boundaryInterpolation = boundaryInterpolation;
        }

        /// <summary>
        /// Evaluates the integrand for the early exercise premium.
        /// </summary>
        /// <param name="z">Transformed time, where z = sqrt(u) and u is the time of exercise.</param>
        public double Evaluate(double z)
        {
            double u = z * z; 
            
            double chebyshev_coord = (2.0 * z / _T_sqrt) - 1.0;
            
            double h_val = _boundaryInterpolation.Value(chebyshev_coord);
            double b_u = _xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, h_val)));

            if (b_u <= 1e-12) return 0.0;
            
            double time_remaining = _T - u;
            if (time_remaining < 1e-9) return 0.0;
            
            double v_rem = _vol * Math.Sqrt(time_remaining);
            
            double dp = (Math.Log(_S / b_u) + (_r - _q + 0.5 * _vol2) * time_remaining) / v_rem;
            double dm = dp - v_rem;
            
            double premium_integrand = _r * _K * Math.Exp(-_r * u) * Distributions.CumulativeNormal(-dm) -
                                     _q * _S * Math.Exp(-_q * u) * Distributions.CumulativeNormal(-dp);

            return 2.0 * z * premium_integrand;
        }
    }
}