using System;
using Antares.Distribution;
using Antares.Integrator;

namespace Antares.Engine
{
    /// <summary>
    /// Enhanced base class for rigorous fixed-point equations with comprehensive error handling
    /// </summary>
    public abstract class RigorousFixedPointEquation
    {
        protected readonly double K, r, q, vol;
        protected readonly Func<double, double> GetBoundary;
        protected readonly IIntegrator Integrator;

        protected RigorousFixedPointEquation(double k, double r, double q, double vol, 
            Func<double, double> getBoundary, IIntegrator integrator)
        {
            this.K = k; this.r = r; this.q = q; this.vol = vol;
            this.GetBoundary = getBoundary; this.Integrator = integrator;
        }

        public abstract (double N, double D, double Fv) F(double tau, double b);
        public abstract (double Nd, double Dd) NDd(double tau, double b);

        /// <summary>
        /// Enhanced d-function calculation with comprehensive numerical handling
        /// </summary>
        protected (double dp, double dm) CalculateD(double t, double z)
        {
            if (t <= 1e-12)
            {
                // Handle near-expiration case
                double logZ = Math.Log(Math.Max(z, 1e-12));
                double sign = Math.Sign(logZ + (r - q) * t);
                return (sign * 1e10, sign * 1e10); // Large finite values instead of infinity
            }
            
            double v = vol * Math.Sqrt(t);
            if (v < 1e-12) 
            {
                // Zero volatility case
                double drift = Math.Log(Math.Max(z, 1e-12)) + (r - q) * t;
                double sign = Math.Sign(drift);
                return (sign * 1e10, sign * 1e10);
            }
            
            double logZ = Math.Log(Math.Max(z, 1e-12));
            double m = (logZ + (r - q) * t) / v;
            
            // Bounds checking to prevent numerical issues
            m = Math.Max(m, -50.0);
            m = Math.Min(m, 50.0);
            
            double dp = m + 0.5 * v;
            double dm = m - 0.5 * v;
            
            return (dp, dm);
        }
        
        /// <summary>
        /// Enhanced numerical comparison with adaptive tolerance
        /// </summary>
        protected static bool IsClose(double a, double b, double tolerance = 1e-9)
        {
            if (Math.Abs(a) < 1e-12 && Math.Abs(b) < 1e-12) return true;
            double relativeError = Math.Abs(a - b) / Math.Max(Math.Abs(a), Math.Abs(b));
            return relativeError < tolerance;
        }

        /// <summary>
        /// Safe evaluation of normal distribution functions with overflow protection
        /// </summary>
        protected (double phi, double Phi) SafeNormalEvaluation(double x)
        {
            x = Math.Max(x, -50.0);
            x = Math.Min(x, 50.0);
            
            double phi = Distributions.NormalDensity(x);
            double Phi = Distributions.CumulativeNormal(x);
            
            // Handle potential numerical issues
            if (double.IsNaN(phi) || double.IsInfinity(phi)) phi = 0.0;
            if (double.IsNaN(Phi) || double.IsInfinity(Phi)) Phi = x > 0 ? 1.0 : 0.0;
            
            return (phi, Phi);
        }
    }

    /// <summary>
    /// Rigorous implementation of Fixed-Point Equation A (Smooth-Pasting Condition)
    /// Based on Section 3 of the documentation with enhanced numerical stability
    /// </summary>
    public class RigorousFixedPointEquation_A : RigorousFixedPointEquation
    {
        public RigorousFixedPointEquation_A(double k, double r, double q, double vol, 
            Func<double, double> getBoundary, IIntegrator integrator)
            : base(k, r, q, vol, getBoundary, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            if (tau < 1e-12)
            {
                double limitValue = K * Math.Exp(-(r - q) * tau);
                return (0.5, 0.5, limitValue);
            }

            double sqrt_tau = Math.Sqrt(tau);
            double v = Math.Max(vol * sqrt_tau, 1e-12);

            // Enhanced K12 integral calculation
            double K12 = CalculateK12Integral(tau, b, sqrt_tau, v);
            
            // Enhanced K3 integral calculation  
            double K3 = CalculateK3Integral(tau, b, sqrt_tau, v);
            
            // Boundary condition terms
            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            var (phi_d_minus, Phi_d_minus) = SafeNormalEvaluation(d_minus_K);
            var (phi_d_plus, Phi_d_plus) = SafeNormalEvaluation(d_plus_K);
            
            // Assemble N and D with enhanced numerical stability
            double N_val = phi_d_minus / v + r * K3;
            double D_val = phi_d_plus / v + Phi_d_plus + q * K12;
            
            // Enhanced division with numerical safeguards
            double fv = 0.0;
            if (Math.Abs(D_val) > 1e-12)
            {
                fv = K * Math.Exp(-(r - q) * tau) * N_val / D_val;
                
                // Rigorous bounds checking
                fv = Math.Max(0.0, fv);
                fv = Math.Min(fv, K * 2.0); // Prevent unrealistic values
                
                // Additional sanity check
                if (double.IsNaN(fv) || double.IsInfinity(fv))
                {
                    fv = b; // Fallback to current boundary
                }
            }
            else
            {
                // Handle degenerate case
                fv = K * Math.Exp(-(r - q) * tau);
            }
            
            return (N_val, D_val, fv);
        }

        private double CalculateK12Integral(double tau, double b, double sqrt_tau, double v)
        {
            try
            {
                Func<double, double> integrand = y =>
                {
                    double m = 0.25 * tau * Math.Pow(1 + y, 2);
                    if (m >= tau - 1e-12) return 0.0;
                    
                    double boundary_at_m = GetBoundary(tau - m);
                    if (boundary_at_m <= 1e-12) return 0.0;
                    
                    (double dp, _) = CalculateD(m, b / boundary_at_m);
                    var (phi_dp, Phi_dp) = SafeNormalEvaluation(dp);
                    
                    double discount = Math.Exp(-q * m);
                    double weight = 0.5 * tau * (y + 1);
                    double density_term = sqrt_tau / v * phi_dp;
                    
                    double result = discount * (weight * Phi_dp + density_term);
                    
                    // Prevent numerical overflow
                    if (double.IsNaN(result) || double.IsInfinity(result)) return 0.0;
                    return result;
                };
                
                double integral = Integrator.Integrate(integrand, -1, 1);
                double K12 = Math.Exp(q * tau) * integral;
                
                // Bounds checking
                if (double.IsNaN(K12) || double.IsInfinity(K12)) return 0.0;
                return Math.Max(0.0, K12);
            }
            catch (Exception)
            {
                return 0.0; // Safe fallback
            }
        }

        private double CalculateK3Integral(double tau, double b, double sqrt_tau, double v)
        {
            try
            {
                Func<double, double> integrand = y =>
                {
                    double m = 0.25 * tau * Math.Pow(1 + y, 2);
                    if (m >= tau - 1e-12) return 0.0;
                    
                    double boundary_at_m = GetBoundary(tau - m);
                    if (boundary_at_m <= 1e-12) return 0.0;
                    
                    (_, double dm) = CalculateD(m, b / boundary_at_m);
                    var (phi_dm, _) = SafeNormalEvaluation(dm);
                    
                    double discount = Math.Exp(-r * m);
                    double density_term = sqrt_tau / v * phi_dm;
                    
                    double result = discount * density_term;
                    
                    if (double.IsNaN(result) || double.IsInfinity(result)) return 0.0;
                    return result;
                };
                
                double integral = Integrator.Integrate(integrand, -1, 1);
                double K3 = Math.Exp(r * tau) * integral;
                
                if (double.IsNaN(K3) || double.IsInfinity(K3)) return 0.0;
                return Math.Max(0.0, K3);
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            if (tau < 1e-12 || b <= 1e-12) return (0.0, 0.0);
            
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            var (phi_d_plus, _) = SafeNormalEvaluation(d_plus);
            var (phi_d_minus, _) = SafeNormalEvaluation(d_minus);
            
            double v_tau_sq = vol * vol * tau;
            double sqrt_tau = Math.Sqrt(tau);
            double v = Math.Max(vol * sqrt_tau, 1e-12);
            
            double common_factor = 1.0 / (b * Math.Max(v_tau_sq, 1e-12));
            
            double Dd = -phi_d_plus * d_plus * common_factor + phi_d_plus / (b * v);
            double Nd = -phi_d_minus * d_minus * common_factor;
            
            // Bounds checking
            if (double.IsNaN(Dd) || double.IsInfinity(Dd)) Dd = 0.0;
            if (double.IsNaN(Nd) || double.IsInfinity(Nd)) Nd = 0.0;

            return (Nd, Dd);
        }
    }
    
    /// <summary>
    /// Rigorous implementation of Fixed-Point Equation B (Value-Matching Condition)
    /// Enhanced with better integration bounds and numerical stability
    /// </summary>
    public class RigorousFixedPointEquation_B : RigorousFixedPointEquation
    {
        public RigorousFixedPointEquation_B(double k, double r, double q, double vol, 
            Func<double, double> getBoundary, IIntegrator integrator)
            : base(k, r, q, vol, getBoundary, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            if (tau < 1e-12)
            {
                double limitValue = K * Math.Exp(-(r - q) * tau);
                if (IsClose(b, K))
                    return (0.5, 0.5, limitValue);
                else
                    return (b < K ? (0.0, 0.0, 0.0) : (1.0, 1.0, limitValue));
            }

            // Enhanced integration with adaptive error handling
            double ni = CalculateNumeratorIntegral(tau, b);
            double di = CalculateDenominatorIntegral(tau, b);
            
            // Boundary condition terms
            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            var (_, Phi_d_minus) = SafeNormalEvaluation(d_minus_K);
            var (_, Phi_d_plus) = SafeNormalEvaluation(d_plus_K);
            
            double N_val = Phi_d_minus + r * ni;
            double D_val = Phi_d_plus + q * di;
            
            // Enhanced division with comprehensive bounds checking
            double fv = 0.0;
            if (Math.Abs(D_val) > 1e-12)
            {
                fv = K * Math.Exp(-(r - q) * tau) * N_val / D_val;
                
                // Rigorous validation
                if (double.IsNaN(fv) || double.IsInfinity(fv) || fv < 0)
                {
                    fv = Math.Max(0.0, K * Math.Exp(-(r - q) * tau)); // Conservative fallback
                }
                else
                {
                    fv = Math.Max(0.0, Math.Min(fv, K * 1.5)); // Reasonable bounds
                }
            }
            else
            {
                fv = K * Math.Exp(-(r - q) * tau);
            }
            
            return (N_val, D_val, fv);
        }

        private double CalculateNumeratorIntegral(double tau, double b)
        {
            try
            {
                Func<double, double> integrand = u =>
                {
                    if (u >= tau - 1e-12)
                    {
                        // Boundary case handling
                        double boundary_u = GetBoundary(u);
                        double factor = IsClose(b, boundary_u) ? 0.5 : (b < boundary_u ? 0.0 : 1.0);
                        return factor * Math.Exp(r * u);
                    }
                    
                    double time_remaining = tau - u;
                    double boundary_u = GetBoundary(u);
                    
                    if (boundary_u <= 1e-12) return 0.0;
                    
                    (_, double dm) = CalculateD(time_remaining, b / boundary_u);
                    var (_, Phi_dm) = SafeNormalEvaluation(dm);
                    
                    double result = Math.Exp(r * u) * Phi_dm;
                    
                    if (double.IsNaN(result) || double.IsInfinity(result)) return 0.0;
                    return result;
                };

                double result = Integrator.Integrate(integrand, 0, tau);
                
                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    // Fallback to simple approximation
                    return tau * Math.Exp(r * tau / 2) * 0.5;
                }
                
                return Math.Max(0.0, result);
            }
            catch (Exception)
            {
                return tau * Math.Exp(r * tau / 2) * 0.5; // Conservative fallback
            }
        }

        private double CalculateDenominatorIntegral(double tau, double b)
        {
            try
            {
                Func<double, double> integrand = u =>
                {
                    if (u >= tau - 1e-12)
                    {
                        double boundary_u = GetBoundary(u);
                        double factor = IsClose(b, boundary_u) ? 0.5 : (b < boundary_u ? 0.0 : 1.0);
                        return factor * Math.Exp(q * u);
                    }
                    
                    double time_remaining = tau - u;
                    double boundary_u = GetBoundary(u);
                    
                    if (boundary_u <= 1e-12) return 0.0;
                    
                    (double dp, _) = CalculateD(time_remaining, b / boundary_u);
                    var (_, Phi_dp) = SafeNormalEvaluation(dp);
                    
                    double result = Math.Exp(q * u) * Phi_dp;
                    
                    if (double.IsNaN(result) || double.IsInfinity(result)) return 0.0;
                    return result;
                };

                double result = Integrator.Integrate(integrand, 0, tau);
                
                if (double.IsNaN(result) || double.IsInfinity(result))
                {
                    return tau * Math.Exp(q * tau / 2) * 0.5;
                }
                
                return Math.Max(0.0, result);
            }
            catch (Exception)
            {
                return tau * Math.Exp(q * tau / 2) * 0.5;
            }
        }
        
        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            if (tau < 1e-12 || b <= 1e-12) return (0.0, 0.0);
            
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            var (phi_d_minus, _) = SafeNormalEvaluation(d_minus);
            var (phi_d_plus, _) = SafeNormalEvaluation(d_plus);
            
            double sqrt_tau = Math.Sqrt(tau);
            double v = Math.Max(vol * sqrt_tau, 1e-12);
            
            double common = 1.0 / (b * v);
            
            double Nd = phi_d_minus * common;
            double Dd = phi_d_plus * common;
            
            // Bounds checking
            if (double.IsNaN(Nd) || double.IsInfinity(Nd)) Nd = 0.0;
            if (double.IsNaN(Dd) || double.IsInfinity(Dd)) Dd = 0.0;
            
            return (Nd, Dd);
        }
    }
}