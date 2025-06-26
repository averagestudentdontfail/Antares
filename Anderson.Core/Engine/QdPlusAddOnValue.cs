using System;
using Anderson.Distribution;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    /// <summary>
    /// Represents the integrand for calculating the early exercise premium of an American put option.
    /// This is a direct C# port of the detail::QdPlusAddOnValue implementation from QuantLib's
    /// qdplusamericanengine.cpp, which correctly interprets the convolution integral.
    /// </summary>
    public class QdPlusAddOnValue
    {
        private readonly double _T, _T_sqrt, _S, _K, _xmax, _r, _q, _vol, _vol2;
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
        public double Evaluate(double z)
        {
            // s is the time-to-maturity of the component part of the integral.
            double s = z * z;
            if (s < 1e-9) return 0.0;

            // 1. The boundary B is a function of time elapsed (T-s).
            double time_elapsed = _T - s;
            if (time_elapsed < 0) return 0.0;
            
            // To look up B(T-s), we need its transformed coordinate x = sqrt(T-s).
            double boundary_coord_x = Math.Sqrt(time_elapsed);
            // Map x to the canonical [-1, 1] domain for the interpolator.
            double chebyshev_coord = (2.0 * boundary_coord_x / _T_sqrt) - 1.0;
            
            double h_val = _boundaryInterpolation.Value(chebyshev_coord);
            double boundary_at_T_minus_s = _xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, h_val)));

            if (boundary_at_T_minus_s <= 1e-12) return 0.0;
            
            // 2. The discount factors and Black-Scholes 'd' functions use 's' as the time parameter.
            double dr_s = Math.Exp(-_r * s);
            double dq_s = Math.Exp(-_q * s);
            double v_s = _vol * Math.Sqrt(s);

            // The d-functions use 's' as the time parameter and B(T-s) as the strike.
            double dp = (Math.Log(_S * dq_s / (boundary_at_T_minus_s * dr_s)) / v_s) + 0.5 * v_s;
            double dm = dp - v_s;
            
            // 3. Assemble the integrand using discount factors based on 's'.
            double premium_integrand = 
                _r * _K * dr_s * Distributions.CumulativeNormal(-dm) -
                _q * _S * dq_s * Distributions.CumulativeNormal(-dp);

            // 4. The 2*z factor comes from the change of variables from s to z (ds = 2z dz).
            return 2.0 * z * premium_integrand;
        }
    }
}