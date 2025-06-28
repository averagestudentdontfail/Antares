using System;
using Antares.Distribution;
using Antares.Integrator;

namespace Antares.Engine
{
    public abstract class DqFpEquation
    {
        protected readonly double K, r, q, vol;
        protected readonly Func<double, double> GetBoundary;
        protected readonly IIntegrator Integrator;

        protected DqFpEquation(double k, double r, double q, double vol, Func<double, double> getBoundary, IIntegrator integrator)
        {
            this.K = k; 
            this.r = r; 
            this.q = q; 
            this.vol = vol;
            this.GetBoundary = getBoundary; 
            this.Integrator = integrator;
        }

        public abstract (double N, double D, double Fv) F(double tau, double b);
        public abstract (double Nd, double Dd) NDd(double tau, double b);

        protected (double dp, double dm) CalculateD(double t, double z)
        {
            if (t <= 1e-12)
            {
                // Handle zero time case
                double logZ = Math.Log(Math.Max(z, 1e-12));
                double sign = Math.Sign(logZ);
                return (sign * 1e6, sign * 1e6);
            }
            
            double v = vol * Math.Sqrt(t);
            if (v < 1e-12) 
            {
                // Handle zero volatility case
                double drift = (r - q) * t;
                double logZ = Math.Log(Math.Max(z, 1e-12)) + drift;
                double sign = Math.Sign(logZ);
                return (sign * 1e6, sign * 1e6);
            }
            
            double logZ_drift = Math.Log(Math.Max(z, 1e-12)) + (r - q) * t;
            double m = logZ_drift / v;
            return (m + 0.5 * v, m - 0.5 * v);
        }
        
        protected static bool IsClose(double a, double b, double tolerance = 1e-10)
        {
            if (Math.Abs(a) < tolerance && Math.Abs(b) < tolerance) return true;
            if (Math.Max(Math.Abs(a), Math.Abs(b)) < tolerance) return true;
            return Math.Abs(a - b) / Math.Max(Math.Abs(a), Math.Abs(b)) < tolerance;
        }
        
        protected double SafeIntegrate(Func<double, double> integrand, double a, double b)
        {
            try
            {
                if (Math.Abs(b - a) < 1e-12) return 0.0;
                return Integrator.Integrate(integrand, a, b);
            }
            catch (Exception)
            {
                // Fallback: simple trapezoidal rule
                int n = 50;
                double h = (b - a) / n;
                double sum = 0.5 * (integrand(a) + integrand(b));
                
                for (int i = 1; i < n; i++)
                {
                    double x = a + i * h;
                    sum += integrand(x);
                }
                
                return sum * h;
            }
        }
    }

    public class DqFpEquation_A : DqFpEquation
    {
        public DqFpEquation_A(double k, double r, double q, double vol, Func<double, double> getBoundary, IIntegrator integrator)
            : base(k, r, q, vol, getBoundary, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            if (tau < 1e-12)
            {
                // Near expiration case
                double limitValue = r >= q ? K : K * r / q;
                return (0.5, 0.5, limitValue);
            }

            // Transform integration domain for numerical stability
            Func<double, double> transformedIntegrand = y =>
            {
                // Map [-1,1] to [0,tau] using quadratic transformation
                double xi = 0.5 * (1.0 + y);
                double u = xi * xi * tau;
                
                if (u >= tau - 1e-12) return 0.0;
                
                double time_remaining = tau - u;
                double boundary_u = GetBoundary(u);
                
                if (boundary_u <= 1e-12 || time_remaining <= 1e-12) return 0.0;
                
                // Calculate Black-Scholes parameters
                (double dp, double dm) = CalculateD(time_remaining, b / boundary_u);
                
                // Jacobian for the transformation: du = tau * xi * dy
                double jacobian = tau * xi;
                
                // Discount factors
                double dr_u = Math.Exp(-r * u);
                double dq_u = Math.Exp(-q * u);
                
                // Integrand components with proper scaling
                double phi_dp = Distributions.NormalDensity(dp);
                double phi_dm = Distributions.NormalDensity(dm);
                
                if (time_remaining > 1e-12 && vol > 1e-12)
                {
                    double v_rem = vol * Math.Sqrt(time_remaining);
                    double density_factor = phi_dp / v_rem;
                    double interest_term = phi_dm / v_rem;
                    
                    return jacobian * (q * dq_u * density_factor + r * dr_u * interest_term);
                }
                
                return 0.0;
            };

            double integral = SafeIntegrate(transformedIntegrand, -1.0, 1.0);
            
            // Black-Scholes parameters at boundary
            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            
            double phi_d_minus = Distributions.NormalDensity(d_minus_K);
            double phi_d_plus = Distributions.NormalDensity(d_plus_K);
            double Phi_d_plus = Distributions.CumulativeNormal(d_plus_K);
            
            double v_tau = vol * Math.Sqrt(tau);
            
            double N_val = phi_d_minus / Math.Max(v_tau, 1e-12) + integral;
            double D_val = phi_d_plus / Math.Max(v_tau, 1e-12) + Phi_d_plus + q * integral / r;
            
            double fv = 0.0;
            if (Math.Abs(D_val) > 1e-12)
            {
                fv = K * Math.Exp(-(r - q) * tau) * N_val / D_val;
                fv = Math.Max(0.0, Math.Min(fv, K));
            }
            else
            {
                fv = K * Math.Exp(-(r - q) * tau);
            }
            
            return (N_val, D_val, fv);
        }

        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            if (tau < 1e-12 || b <= 1e-12) return (0.0, 0.0);
            
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            double v_tau = vol * Math.Sqrt(tau);
            
            if (v_tau < 1e-12) return (0.0, 0.0);
            
            double phi_d_plus = Distributions.NormalDensity(d_plus);
            double phi_d_minus = Distributions.NormalDensity(d_minus);
            
            // Derivatives with respect to log(boundary)
            double common_factor = 1.0 / (v_tau);
            
            double Dd = -phi_d_plus * d_plus * common_factor / b;
            double Nd = -phi_d_minus * d_minus * common_factor / b;

            return (Nd, Dd);
        }
    }
    
    public class DqFpEquation_B : DqFpEquation
    {
        public DqFpEquation_B(double k, double r, double q, double vol, Func<double, double> getBoundary, IIntegrator integrator)
            : base(k, r, q, vol, getBoundary, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            if (tau < 1e-12)
            {
                // Near expiration case with proper limiting behavior
                double limitValue = r >= q ? K : K * r / q;
                if (IsClose(b, limitValue))
                    return (0.5, 0.5, limitValue);
                else
                    return (b < limitValue ? (0.0, 0.0, 0.0) : (1.0, 1.0, limitValue));
            }

            // Calculate the integral using transformed coordinates
            Func<double, double> n_integrand = u =>
            {
                if (u >= tau - 1e-12)
                {
                    double boundary_at_u = GetBoundary(u);
                    if (IsClose(b, boundary_at_u))
                        return 0.5 * Math.Exp(r * u);
                    else
                        return (b < boundary_at_u ? 0.0 : 1.0) * Math.Exp(r * u);
                }
                
                double time_remaining = tau - u;
                double boundary_value_u = GetBoundary(u);
                
                if (boundary_value_u <= 1e-12 || time_remaining <= 1e-12) return 0.0;
                
                (_, double dm) = CalculateD(time_remaining, b / boundary_value_u);
                double Phi_dm = Distributions.CumulativeNormal(dm);
                
                return Math.Exp(r * u) * Phi_dm;
            };

            double ni = SafeIntegrate(n_integrand, 0, tau);

            Func<double, double> d_integrand = u =>
            {
                if (u >= tau - 1e-12)
                {
                    double boundary_at_u_d = GetBoundary(u);
                    if (IsClose(b, boundary_at_u_d))
                        return 0.5 * Math.Exp(q * u);
                    else
                        return (b < boundary_at_u_d ? 0.0 : 1.0) * Math.Exp(q * u);
                }
                
                double time_remaining = tau - u;
                double boundary_value_u_d = GetBoundary(u);
                
                if (boundary_value_u_d <= 1e-12 || time_remaining <= 1e-12) return 0.0;
                
                (double dp, _) = CalculateD(time_remaining, b / boundary_value_u_d);
                double Phi_dp = Distributions.CumulativeNormal(dp);
                
                return Math.Exp(q * u) * Phi_dp;
            };

            double di = SafeIntegrate(d_integrand, 0, tau);

            // Boundary conditions at terminal time
            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            
            double Phi_d_minus = Distributions.CumulativeNormal(d_minus_K);
            double Phi_d_plus = Distributions.CumulativeNormal(d_plus_K);
            
            double N_val = Phi_d_minus + r * ni;
            double D_val = Phi_d_plus + q * di;
            
            double fv = 0.0;
            if (Math.Abs(D_val) > 1e-12)
            {
                fv = K * Math.Exp(-(r - q) * tau) * N_val / D_val;
                fv = Math.Max(0.0, Math.Min(fv, K * 1.2));
            }
            else
            {
                fv = K * Math.Exp(-(r - q) * tau);
            }
            
            return (N_val, D_val, fv);
        }
        
        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            if (tau < 1e-12 || b <= 1e-12) return (0.0, 0.0);
            
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            
            double v_tau = vol * Math.Sqrt(tau);
            
            if (v_tau < 1e-12) return (0.0, 0.0);
            
            double phi_d_minus = Distributions.NormalDensity(d_minus);
            double phi_d_plus = Distributions.NormalDensity(d_plus);
            
            double common = 1.0 / (b * v_tau);
            
            return (phi_d_minus * common, phi_d_plus * common);
        }
    }
}