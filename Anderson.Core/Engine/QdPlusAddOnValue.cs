using System;
using Anderson.Interpolation;
using Anderson.Distribution;

namespace Anderson.Engine
{
    /// <summary>
    /// Represents the integrand for calculating the early exercise premium of an American put option.
    /// The integration is performed over a transformed time variable z = sqrt(tau).
    /// </summary>
    public class QdPlusAddOnValue
    {
        private readonly double _T, _S, _K, _xmax, _r, _q, _vol;
        private readonly Anderson.Interpolation.Interpolation _boundaryInterpolation;

        public QdPlusAddOnValue(double T, double S, double K, double r, double q, double vol, double xmax, Anderson.Interpolation.Interpolation boundaryInterpolation)
        {
            _T = T; _S = S; _K = K; _r = r; _q = q; _vol = vol;
            _xmax = xmax;
            _boundaryInterpolation = boundaryInterpolation;
        }

        /// <summary>
        /// Evaluates the integrand at a point z in the transformed time domain.
        /// </summary>
        /// <param name="z">Transformed time, where z = sqrt(tau).</param>
        public double Evaluate(double z)
        {
            double t = z * z; // tau = z^2

            // Get the boundary value B(t) from the interpolation.
            // First, map t back to the canonical [-1, 1] domain for the interpolator.
            double chebyshevNode = 2.0 * Math.Sqrt(Math.Max(0.0, _T - t) / _T) - 1.0;
            double h_val = _boundaryInterpolation.Value(chebyshevNode);
            double b_t = _xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, h_val)));

            if (b_t <= 1e-12) return 0.0;

            double dr = Math.Exp(-_r * t);
            double dq = Math.Exp(-_q * t);
            double v = _vol * Math.Sqrt(t);

            if (v < 1e-9) // Handle t -> 0 case
            {
                return Math.Abs(dr * _K - dq * _S) < 1e-9 ? z * (_r * _K * dr - _q * _S * dq) : 0.0;
            }
            
            double dp = (Math.Log(_S * dq / (b_t * dr)) / v) + 0.5 * v;
            
            // Integrand is 2*z * (r*K*N(-d_minus) - q*S*N(-d_plus))
            return 2.0 * z * (_r * _K * dr * Distributions.CumulativeNormal(-dp + v) - _q * _S * dq * Distributions.CumulativeNormal(-dp));
        }
    }
}