using System;
using Antares.Distribution;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
    /// <summary>
    /// Rigorous implementation of the add-on value calculation with enhanced numerical stability
    /// Based on Section 3.5 of the documentation with comprehensive error handling
    /// </summary>
    public class RigorousQdPlusAddOnValue
    {
        private readonly double _T, _T_sqrt, _S, _K, _xmax, _r, _q, _vol, _vol2;
        private readonly ChebyshevInterpolation _boundaryInterpolation;
        private readonly bool _enableDiagnostics;
        private int _evaluationCount;
        private int _errorCount;

        public RigorousQdPlusAddOnValue(double T, double S, double K, double r, double q, double vol, 
            double xmax, ChebyshevInterpolation boundaryInterpolation, bool enableDiagnostics = false)
        {
            _T = T;
            _T_sqrt = Math.Sqrt(T);
            _S = S; _K = K; _r = r; _q = q; _vol = vol; _vol2 = vol * vol;
            _xmax = xmax;
            _boundaryInterpolation = boundaryInterpolation;
            _enableDiagnostics = enableDiagnostics;
            _evaluationCount = 0;
            _errorCount = 0;

            if (_enableDiagnostics)
            {
                Console.WriteLine($"RigorousAddOn initialized: T={T:F4}, S={S:F2}, K={K:F2}, xmax={xmax:F4}");
            }
        }

        /// <summary>
        /// Enhanced evaluation of the early exercise premium integrand
        /// Implements equation (3.5) from the documentation with comprehensive numerical safeguards
        /// </summary>
        public double Evaluate(double z)
        {
            _evaluationCount++;
            
            try
            {
                // Step 1: Transform integration variable z to time s
                double s = z * z;
                if (s < 1e-12) return 0.0; // Near zero time
                if (s >= _T - 1e-12) return 0.0; // Beyond maturity

                // Step 2: Calculate time elapsed and validate
                double time_elapsed = _T - s;
                if (time_elapsed <= 0) return 0.0;
                
                // Step 3: Enhanced boundary reconstruction with validation
                double boundary_at_T_minus_s = ReconstructBoundaryRobust(time_elapsed);
                if (boundary_at_T_minus_s <= 1e-12) return 0.0;
                
                // Step 4: Calculate discount factors with overflow protection
                var (dr_s, dq_s) = CalculateDiscountFactors(s);
                
                // Step 5: Enhanced d-function calculation
                var (dp, dm) = CalculateDFunctionsRobust(s, boundary_at_T_minus_s, dr_s, dq_s);
                
                // Step 6: Calculate premium integrand with comprehensive bounds checking
                double premium_integrand = CalculatePremiumIntegrandRobust(dr_s, dq_s, dp, dm);
                
                // Step 7: Apply Jacobian factor and final validation
                double result = 2.0 * z * premium_integrand;
                
                // Step 8: Rigorous result validation
                if (!ValidateResult(result, z, s))
                {
                    _errorCount++;
                    return 0.0;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _errorCount++;
                if (_enableDiagnostics && _errorCount <= 5) // Limit diagnostic spam
                {
                    Console.WriteLine($"AddOn evaluation error at z={z:F6}: {ex.Message}");
                }
                return 0.0;
            }
        }

        /// <summary>
        /// Robust boundary reconstruction with enhanced interpolation validation
        /// </summary>
        private double ReconstructBoundaryRobust(double time_elapsed)
        {
            try
            {
                // Calculate Chebyshev coordinate with bounds checking
                double boundary_coord_x = Math.Sqrt(Math.Max(0.0, time_elapsed));
                double chebyshev_coord = (2.0 * boundary_coord_x / _T_sqrt) - 1.0;
                
                // Enhanced coordinate clamping
                chebyshev_coord = Math.Max(-1.0 + 1e-12, Math.Min(1.0 - 1e-12, chebyshev_coord));
                
                // Get h-value with interpolation validation
                double h_val = _boundaryInterpolation.Value(chebyshev_coord);
                
                // Enhanced h-value validation and bounds
                if (double.IsNaN(h_val) || double.IsInfinity(h_val))
                {
                    if (_enableDiagnostics) Console.WriteLine($"Invalid h_val: {h_val} at coord: {chebyshev_coord:F6}");
                    return _xmax * 0.5; // Fallback value
                }
                
                // Apply reasonable bounds to h-value
                h_val = Math.Max(0.0, Math.Min(h_val, 100.0));
                
                // Transform back to boundary value with overflow protection
                double sqrt_h = Math.Sqrt(h_val);
                sqrt_h = Math.Min(sqrt_h, 50.0); // Prevent exp overflow
                
                double boundary_value = _xmax * Math.Exp(-sqrt_h);
                
                // Final boundary validation
                boundary_value = Math.Max(boundary_value, _S * 1e-8);
                boundary_value = Math.Min(boundary_value, _K * 1.5);
                
                return boundary_value;
            }
            catch (Exception ex)
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"Boundary reconstruction error: {ex.Message}");
                }
                return _xmax * 0.5; // Conservative fallback
            }
        }

        /// <summary>
        /// Calculate discount factors with overflow protection
        /// </summary>
        private (double dr_s, double dq_s) CalculateDiscountFactors(double s)
        {
            // Prevent overflow in exponential calculations
            double r_term = -_r * s;
            double q_term = -_q * s;
            
            r_term = Math.Max(r_term, -100.0); // Prevent underflow
            q_term = Math.Max(q_term, -100.0);
            
            double dr_s = Math.Exp(r_term);
            double dq_s = Math.Exp(q_term);
            
            // Additional bounds checking
            dr_s = Math.Max(dr_s, 1e-15);
            dq_s = Math.Max(dq_s, 1e-15);
            
            return (dr_s, dq_s);
        }

        /// <summary>
        /// Enhanced d-function calculation with comprehensive numerical handling
        /// </summary>
        private (double dp, double dm) CalculateDFunctionsRobust(double s, double boundary_value, double dr_s, double dq_s)
        {
            double v_s = _vol * Math.Sqrt(s);
            
            if (v_s < 1e-12)
            {
                // Zero volatility case - handle analytically
                double ratio = _S * dq_s / (boundary_value * dr_s);
                if (ratio <= 0)
                {
                    return (-50.0, -50.0); // Large negative values
                }
                
                double log_ratio = Math.Log(ratio);
                double sign = Math.Sign(log_ratio);
                return (sign * 50.0, sign * 50.0); // Large finite values
            }
            
            // Standard case with enhanced numerical stability
            try
            {
                double numerator = _S * dq_s;
                double denominator = boundary_value * dr_s;
                
                // Validate ratio components
                if (denominator <= 1e-15 || numerator <= 1e-15)
                {
                    return (-50.0, -50.0);
                }
                
                double ratio = numerator / denominator;
                double log_ratio = Math.Log(Math.Max(ratio, 1e-15));
                
                // Bounds checking on log ratio
                log_ratio = Math.Max(log_ratio, -100.0);
                log_ratio = Math.Min(log_ratio, 100.0);
                
                double dp = log_ratio / v_s + 0.5 * v_s;
                double dm = dp - v_s;
                
                // Final bounds on d-values
                dp = Math.Max(dp, -50.0);
                dp = Math.Min(dp, 50.0);
                dm = Math.Max(dm, -50.0);
                dm = Math.Min(dm, 50.0);
                
                return (dp, dm);
            }
            catch (Exception)
            {
                return (0.0, 0.0); // Neutral values on error
            }
        }

        /// <summary>
        /// Calculate premium integrand with comprehensive bounds checking
        /// </summary>
        private double CalculatePremiumIntegrandRobust(double dr_s, double dq_s, double dp, double dm)
        {
            try
            {
                // Safe evaluation of normal distribution functions
                double Phi_minus_dm = SafeCumulativeNormal(-dm);
                double Phi_minus_dp = SafeCumulativeNormal(-dp);
                
                // Calculate individual components
                double rK_term = _r * _K * dr_s * Phi_minus_dm;
                double qS_term = _q * _S * dq_s * Phi_minus_dp;
                
                // Validate individual terms
                if (double.IsNaN(rK_term) || double.IsInfinity(rK_term)) rK_term = 0.0;
                if (double.IsNaN(qS_term) || double.IsInfinity(qS_term)) qS_term = 0.0;
                
                double premium_integrand = rK_term - qS_term;
                
                // Bounds checking on final result
                if (double.IsNaN(premium_integrand) || double.IsInfinity(premium_integrand))
                {
                    return 0.0;
                }
                
                // Economic reasonableness check
                double max_reasonable = Math.Max(_r * _K, _q * _S) * 10; // Allow some flexibility
                premium_integrand = Math.Max(premium_integrand, -max_reasonable);
                premium_integrand = Math.Min(premium_integrand, max_reasonable);
                
                return premium_integrand;
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        /// <summary>
        /// Safe evaluation of cumulative normal distribution with bounds checking
        /// </summary>
        private double SafeCumulativeNormal(double x)
        {
            // Clamp extreme values to prevent numerical issues
            x = Math.Max(x, -50.0);
            x = Math.Min(x, 50.0);
            
            try
            {
                double result = Distributions.CumulativeNormal(x);
                
                // Additional validation
                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    return x > 0 ? 1.0 : 0.0; // Asymptotic values
                }
                
                return Math.Max(0.0, Math.Min(1.0, result)); // Ensure [0,1] range
            }
            catch (Exception)
            {
                return x > 0 ? 1.0 : 0.0;
            }
        }

        /// <summary>
        /// Comprehensive result validation
        /// </summary>
        private bool ValidateResult(double result, double z, double s)
        {
            // Basic numerical validity
            if (double.IsNaN(result) || double.IsInfinity(result))
            {
                return false;
            }
            
            // Economic reasonableness bounds
            double max_reasonable_integrand = Math.Max(_r * _K, _q * _S) * _T * 10;
            if (Math.Abs(result) > max_reasonable_integrand)
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"Result exceeds reasonable bounds: {result:F6} at z={z:F6}");
                }
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Get diagnostic information about the evaluation process
        /// </summary>
        public (int evaluations, int errors, double errorRate) GetDiagnostics()
        {
            double errorRate = _evaluationCount > 0 ? (double)_errorCount / _evaluationCount : 0.0;
            return (_evaluationCount, _errorCount, errorRate);
        }
    }
}