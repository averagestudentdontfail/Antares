using System;
using Antares.Distribution;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
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

        public double Evaluate(double z)
        {
            double s = z * z;
            if (s < 1e-9) return 0.0;

            double time_elapsed = _T - s;
            if (time_elapsed < 0) return 0.0;
            
            double boundary_coord_x = Math.Sqrt(time_elapsed);
            double chebyshev_coord = (2.0 * boundary_coord_x / _T_sqrt) - 1.0;
            
            chebyshev_coord = Math.Max(-1.0, Math.Min(1.0, chebyshev_coord));
            
            double h_val = _boundaryInterpolation.Value(chebyshev_coord);
            h_val = Math.Max(0.0, Math.Min(h_val, 50.0));
            
            double boundary_at_T_minus_s = _xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, h_val)));

            if (boundary_at_T_minus_s <= 1e-12) return 0.0;
            
            double dr_s = Math.Exp(-_r * s);
            double dq_s = Math.Exp(-_q * s);
            double v_s = _vol * Math.Sqrt(s);

            double dp, dm;
            if (v_s < 1e-12)
            {
                double ratio = _S * dq_s / (boundary_at_T_minus_s * dr_s);
                dp = ratio > 1.0 ? double.PositiveInfinity : double.NegativeInfinity;
                dm = dp;
            }
            else
            {
                double logRatio = Math.Log(_S * dq_s / (boundary_at_T_minus_s * dr_s));
                dp = logRatio / v_s + 0.5 * v_s;
                dm = dp - v_s;
            }
            
            double premium_integrand = 
                _r * _K * dr_s * Distributions.CumulativeNormal(-dm) -
                _q * _S * dq_s * Distributions.CumulativeNormal(-dp);

            return 2.0 * z * premium_integrand;
        }
    }
}