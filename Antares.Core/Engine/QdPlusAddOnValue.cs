using System;
using Antares.Distribution;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
    public class QdPlusAddOnValue
    {
        private readonly double _T, _T_sqrt, _S, _K, _xmax, _r, _q, _vol;
        private readonly ChebyshevInterpolation _boundaryInterpolation;

        public QdPlusAddOnValue(double T, double S, double K, double r, double q, double vol, 
            double xmax, ChebyshevInterpolation boundaryInterpolation)
        {
            _T = T;
            _T_sqrt = Math.Sqrt(T);
            _S = S; 
            _K = K; 
            _r = r; 
            _q = q; 
            _vol = vol;
            _xmax = xmax;
            _boundaryInterpolation = boundaryInterpolation;
        }

        public double Evaluate(double z)
        {
            // z represents sqrt(s) where s is the integration variable from 0 to sqrt(T)
            if (z <= 1e-12) return 0.0;
            
            double s = z * z;  // s is the actual time variable
            if (s >= _T - 1e-12) return 0.0;

            double time_remaining = _T - s;
            if (time_remaining <= 1e-12) return 0.0;
            
            // Reconstruct the boundary at time (T-s)
            double boundary_at_time = ReconstructBoundary(time_remaining);
            if (boundary_at_time <= 1e-12) return 0.0;
            
            // Calculate discount factors
            double dr_s = Math.Exp(-_r * s);
            double dq_s = Math.Exp(-_q * s);
            
            // Calculate Black-Scholes d parameters
            double d_plus, d_minus;
            
            if (time_remaining > 1e-12 && _vol > 1e-12)
            {
                double v_s = _vol * Math.Sqrt(s);
                if (v_s > 1e-12)
                {
                    double ratio = (_S * dq_s) / (boundary_at_time * dr_s);
                    if (ratio > 1e-12)
                    {
                        double logRatio = Math.Log(ratio);
                        d_plus = logRatio / v_s + 0.5 * v_s;
                        d_minus = d_plus - v_s;
                    }
                    else
                    {
                        // Handle case where ratio is very small
                        d_plus = double.NegativeInfinity;
                        d_minus = double.NegativeInfinity;
                    }
                }
                else
                {
                    // Handle very small volatility * sqrt(time)
                    double ratio = (_S * dq_s) / (boundary_at_time * dr_s);
                    d_plus = ratio > 1.0 ? double.PositiveInfinity : double.NegativeInfinity;
                    d_minus = d_plus;
                }
            }
            else
            {
                // Handle zero time remaining
                double ratio = (_S * dq_s) / (boundary_at_time * dr_s);
                d_plus = ratio > 1.0 ? double.PositiveInfinity : double.NegativeInfinity;
                d_minus = d_plus;
            }
            
            // Calculate the cumulative normal probabilities
            double Phi_d_minus = Distributions.CumulativeNormal(-d_minus);
            double Phi_d_plus = Distributions.CumulativeNormal(-d_plus);
            
            // Early exercise premium integrand
            double interest_benefit = _r * _K * dr_s * Phi_d_minus;
            double dividend_cost = _q * _S * dq_s * Phi_d_plus;
            double premium_integrand = interest_benefit - dividend_cost;

            // Apply the Jacobian transformation: ds = 2z dz
            return 2.0 * z * premium_integrand;
        }

        private double ReconstructBoundary(double time_remaining)
        {
            try
            {
                if (time_remaining <= 1e-12)
                {
                    // Near expiration boundary
                    return _r >= _q ? _K : _K * _r / _q;
                }
                
                if (time_remaining >= _T - 1e-12)
                {
                    // Far from expiration, use asymptotic value
                    return CalculateAsymptoticBoundary();
                }
                
                // Map time_remaining to the Chebyshev coordinate system
                double xi = Math.Sqrt(time_remaining / _T);
                double chebyshev_coord = 2.0 * xi - 1.0;
                
                // Ensure coordinate is within valid range
                chebyshev_coord = Math.Max(-1.0, Math.Min(1.0, chebyshev_coord));
                
                // Evaluate the Chebyshev interpolation
                double h_val = _boundaryInterpolation.Value(chebyshev_coord);
                
                // Bounds checking for H value
                h_val = Math.Max(0.0, Math.Min(h_val, 100.0));
                
                // Transform back to boundary value: B = xmax * exp(-sqrt(H))
                double sqrt_h = Math.Sqrt(Math.Max(0.0, h_val));
                double boundary_value = _xmax * Math.Exp(-sqrt_h);
                
                // Apply reasonable bounds
                boundary_value = Math.Max(boundary_value, _xmax * 1e-6);
                boundary_value = Math.Min(boundary_value, _K * 0.999);
                
                // Additional validation: ensure boundary makes economic sense
                if (boundary_value > _K || boundary_value <= 0)
                {
                    // Fallback to time-interpolated boundary
                    double nearExpiry = _r >= _q ? _K : _K * _r / _q;
                    double asymptotic = CalculateAsymptoticBoundary();
                    double weight = Math.Min(1.0, time_remaining / Math.Max(_T, 1e-6));
                    boundary_value = nearExpiry * (1.0 - weight) + asymptotic * weight;
                }
                
                return boundary_value;
            }
            catch (Exception)
            {
                // Robust fallback: linear interpolation between boundary limits
                double nearExpiry = _r >= _q ? _K : _K * _r / _q;
                double asymptotic = CalculateAsymptoticBoundary();
                double weight = Math.Min(1.0, time_remaining / Math.Max(_T, 1e-6));
                return nearExpiry * (1.0 - weight) + asymptotic * weight;
            }
        }
        
        private double CalculateAsymptoticBoundary()
        {
            if (_r <= _q)
            {
                return _K * Math.Max(0.01, Math.Min(0.99, _r / _q));
            }
            
            // Perpetual American put boundary formula
            double mu = _r - _q - 0.5 * _vol * _vol;
            double discriminant = mu * mu + 2.0 * _r * _vol * _vol;
            
            if (discriminant <= 0)
            {
                return _K * 0.7;
            }
            
            double lambda_minus = (mu - Math.Sqrt(discriminant)) / (_vol * _vol);
            
            if (lambda_minus >= -1e-6)
            {
                return _K * 0.7;
            }
            
            double boundary = _K * lambda_minus / (lambda_minus - 1.0);
            return Math.Max(_K * 0.01, Math.Min(_K * 0.95, boundary));
        }
    }
}