using System;
using Anderson.Distribution;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    /// <summary>
    /// Represents the integrand for calculating the early exercise premium of an American put option.
    /// The integration is performed over a transformed time variable z = sqrt(s), where s is the
    /// time-to-maturity for the European component within the premium integral.
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
        /// <param name="z">Transformed time, z = sqrt(s). 's' is the time-to-maturity for the d-functions.</param>
        public double Evaluate(double z)
        {
            // s is the time-to-maturity for the d-functions in the integral. It's the integration variable.
            double s = z * z; 
            if (s < 1e-9) return 0.0;

            // --- THE FINAL, CRITICAL FIX ---
            // The boundary B is a function of time-to-maturity. 
            // In the convolution integral, the boundary B(τ-s) must be evaluated.
            double time_to_maturity_for_boundary = _T - s;
            if (time_to_maturity_for_boundary < 0.0) return 0.0;

            // To look up B(T-s), we need its transformed coordinate.
            double boundary_coord_x = Math.Sqrt(time_to_maturity_for_boundary);
            // Map x to the canonical [-1, 1] domain for the interpolator.
            double chebyshev_coord = (2.0 * boundary_coord_x / _T_sqrt) - 1.0;
            
            double h_val = _boundaryInterpolation.Value(chebyshev_coord);
            double boundary_at_T_minus_s = _xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, h_val)));

            if (boundary_at_T_minus_s <= 1e-12) return 0.0;
            
            // The d-functions for the premium integrand use 's' as the time parameter
            // and B(T-s) as the strike.
            double v_s = _vol * Math.Sqrt(s);
            double dp = (Math.Log(_S / boundary_at_T_minus_s) + (_r - _q + 0.5 * _vol2) * s) / v_s;
            double dm = dp - v_s;
            
            // The premium integrand from the reformulated convolution integral is:
            // [r*K*exp(-r*s) - q*S*exp(-q*s)] * N(d2(S, B(T-s), s))
            // where N(d2) is the probability of S > B(T-s) at time s, which corresponds to N(-d_minus).
            // This is a subtle point from Kim (1990) and others. The term is (rK-qS) times a probability density.
            // Let's use the explicit Andersen formula which is [r*K*exp*N(-d_)] - [q*S*exp*N(-d+)]
            double premium_integrand = _r * _K * Math.Exp(-_r * s) * Distributions.CumulativeNormal(-dm) -
                                     _q * _S * Math.Exp(-_q * s) * Distributions.CumulativeNormal(-dp);

            // The 2*z factor comes from the change of variables ds = 2z dz
            return 2.0 * z * premium_integrand;
        }
    }
}