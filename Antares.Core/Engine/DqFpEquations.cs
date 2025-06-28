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
            this.K = k; this.r = r; this.q = q; this.vol = vol;
            this.GetBoundary = getBoundary; this.Integrator = integrator;
        }

        public abstract (double N, double D, double Fv) F(double tau, double b);
        public abstract (double Nd, double Dd) NDd(double tau, double b);

        protected (double dp, double dm) CalculateD(double t, double z)
        {
            if (t <= 1e-12)
            {
                double logRatio = Math.Log(Math.Max(z, 1e-12));
                double sign = Math.Sign(logRatio);
                return (sign * double.PositiveInfinity, sign * double.PositiveInfinity);
            }
            
            double v = vol * Math.Sqrt(t);
            if (v < 1e-14) 
            {
                double logRatio = Math.Log(Math.Max(z, 1e-12)) + (r - q) * t;
                double sign = Math.Sign(logRatio);
                return (sign * double.PositiveInfinity, sign * double.PositiveInfinity);
            }
            
            double m = (Math.Log(Math.Max(z, 1e-12)) + (r - q) * t) / v;
            return (m + 0.5 * v, m - 0.5 * v);
        }
        
        protected static bool IsClose(double a, double b)
        {
            if (Math.Abs(a) < 1e-12 && Math.Abs(b) < 1e-12) return true;
            return Math.Abs(a - b) / Math.Max(Math.Abs(a), Math.Abs(b)) < 1e-9;
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
                return (0.5, 0.5, K * Math.Exp(-(r - q) * tau));
            }

            double sqrt_tau = Math.Sqrt(tau);
            double v = vol * sqrt_tau;

            Func<double, double> k12_integrand = y =>
            {
                double m = 0.25 * tau * Math.Pow(1 + y, 2);
                if (m >= tau - 1e-12) return 0.0;
                
                double boundary_at_m = GetBoundary(tau - m);
                if (boundary_at_m <= 1e-12) return 0.0;
                
                (double dp, _) = CalculateD(m, b / boundary_at_m);
                
                double discount = Math.Exp(-q * m);
                double weight = 0.5 * tau * (y + 1);
                double normal_contrib = Distributions.CumulativeNormal(dp);
                double density_contrib = sqrt_tau / Math.Max(vol, 1e-12) * Distributions.NormalDensity(dp);
                
                return discount * (weight * normal_contrib + density_contrib);
            };
            
            double K12 = Math.Exp(q * tau) * Integrator.Integrate(k12_integrand, -1, 1);

            Func<double, double> k3_integrand = y =>
            {
                double m = 0.25 * tau * Math.Pow(1 + y, 2);
                if (m >= tau - 1e-12) return 0.0;
                
                double boundary_at_m = GetBoundary(tau - m);
                if (boundary_at_m <= 1e-12) return 0.0;
                
                (_, double dm) = CalculateD(m, b / boundary_at_m);
                
                double discount = Math.Exp(-r * m);
                double density_contrib = sqrt_tau / Math.Max(vol, 1e-12) * Distributions.NormalDensity(dm);
                
                return discount * density_contrib;
            };
            
            double K3 = Math.Exp(r * tau) * Integrator.Integrate(k3_integrand, -1, 1);
            
            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            
            double phi_d_minus = Distributions.NormalDensity(d_minus_K);
            double phi_d_plus = Distributions.NormalDensity(d_plus_K);
            double Phi_d_plus = Distributions.CumulativeNormal(d_plus_K);
            
            double N_val = phi_d_minus / Math.Max(v, 1e-12) + r * K3;
            double D_val = phi_d_plus / Math.Max(v, 1e-12) + Phi_d_plus + q * K12;
            
            double fv = 0.0;
            if (Math.Abs(D_val) > 1e-12)
            {
                fv = K * Math.Exp(-(r - q) * tau) * N_val / D_val;
                fv = Math.Max(0.0, Math.Min(fv, K));
            }
            
            return (N_val, D_val, fv);
        }

        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            if (tau < 1e-12 || b <= 1e-12) return (0.0, 0.0);
            
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            double v_tau_sq = vol * vol * tau;
            double sqrt_tau = Math.Sqrt(tau);
            double v = vol * sqrt_tau;
            
            double phi_d_plus = Distributions.NormalDensity(d_plus);
            double phi_d_minus = Distributions.NormalDensity(d_minus);
            
            double common_factor = 1.0 / (b * Math.Max(v_tau_sq, 1e-12));
            
            double Dd = -phi_d_plus * d_plus * common_factor + 
                       phi_d_plus / (b * Math.Max(v, 1e-12));
            double Nd = -phi_d_minus * d_minus * common_factor;

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
                if (IsClose(b, K))
                    return (0.5, 0.5, K * Math.Exp(-(r - q) * tau));
                else
                    return (b < K ? (0.0, 0.0, 0.0) : (1.0, 1.0, K * Math.Exp(-(r - q) * tau)));
            }

            Func<double, double> n_integrand = u =>
            {
                if (u >= tau - 1e-12)
                {
                    double boundary_u_val = GetBoundary(u);
                    if (IsClose(b, boundary_u_val))
                        return 0.5 * Math.Exp(r * u);
                    else
                        return (b < boundary_u_val ? 0.0 : 1.0) * Math.Exp(r * u);
                }
                
                double time_remaining = tau - u;
                double boundary_u_val = GetBoundary(u);
                
                if (boundary_u_val <= 1e-12) return 0.0;
                
                (_, double dm) = CalculateD(time_remaining, b / boundary_u_val);
                return Math.Exp(r * u) * Distributions.CumulativeNormal(dm);
            };

            double ni = 0.0;
            try
            {
                ni = Integrator.Integrate(n_integrand, 0, tau);
            }
            catch (Exception)
            {
                ni = tau * Math.Exp(r * tau / 2) * 0.5;
            }

            Func<double, double> d_integrand = u =>
            {
                if (u >= tau - 1e-12)
                {
                    double boundary_u_val = GetBoundary(u);
                    if (IsClose(b, boundary_u_val))
                        return 0.5 * Math.Exp(q * u);
                    else
                        return (b < boundary_u_val ? 0.0 : 1.0) * Math.Exp(q * u);
                }
                
                double time_remaining = tau - u;
                double boundary_u_val = GetBoundary(u);
                
                if (boundary_u_val <= 1e-12) return 0.0;
                
                (double dp, _) = CalculateD(time_remaining, b / boundary_u_val);
                return Math.Exp(q * u) * Distributions.CumulativeNormal(dp);
            };

            double di = 0.0;
            try
            {
                di = Integrator.Integrate(d_integrand, 0, tau);
            }
            catch (Exception)
            {
                di = tau * Math.Exp(q * tau / 2) * 0.5;
            }

            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            
            double Phi_d_minus = Distributions.CumulativeNormal(d_minus_K);
            double Phi_d_plus = Distributions.CumulativeNormal(d_plus_K);
            
            double N_val = Phi_d_minus + r * ni;
            double D_val = Phi_d_plus + q * di;
            
            double fv = 0.0;
            if (Math.Abs(D_val) > 1e-12)
            {
                fv = K * Math.Exp(-(r - q) * tau) * N_val / D_val;
                fv = Math.Max(0.0, Math.Min(fv, K * 1.5));
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
            
            double sqrt_tau = Math.Sqrt(tau);
            double v = vol * sqrt_tau;
            
            if (v < 1e-12) return (0.0, 0.0);
            
            double phi_d_minus = Distributions.NormalDensity(d_minus);
            double phi_d_plus = Distributions.NormalDensity(d_plus);
            
            double common = 1.0 / (b * v);
            
            return (phi_d_minus * common, phi_d_plus * common);
        }
    }
}