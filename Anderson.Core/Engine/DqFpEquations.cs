using System;
using Anderson.Integrator;
using Anderson.Distribution;

namespace Anderson.Engine
{
    /// <summary>
    /// Abstract base class for the fixed-point equation formulations (FP-A and FP-B).
    /// </summary>
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

        // Returns (N, D, Fv), where Fv = K*exp(...) * N/D
        public abstract (double N, double D, double Fv) F(double tau, double b);
        public abstract (double Nd, double Dd) NDd(double tau, double b);

        protected (double dp, double dm) CalculateD(double t, double z)
        {
            double v = vol * Math.Sqrt(t);
            if (v < 1e-12) return (double.PositiveInfinity * Math.Sign(Math.Log(z)), double.PositiveInfinity * Math.Sign(Math.Log(z)));
            double m = (Math.Log(z) + (r - q) * t) / v + 0.5 * v;
            return (m, m - v);
        }
    }

    /// <summary>
    /// Implements the FP-A formulation, which is more stable for low drift (r ~= q) cases.
    /// </summary>
    public class DqFpEquation_A : DqFpEquation
    {
        public DqFpEquation_A(double k, double r, double q, double vol, Func<double, double> getBoundary, IIntegrator integrator)
            : base(k, r, q, vol, getBoundary, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            double v = vol * Math.Sqrt(tau);

            if (tau < 1e-12)
            {
                // Limiting case for tau -> 0
                return (0.5, 0.5, K * Math.Exp(-(r - q) * tau));
            }

            double stv = Math.Sqrt(tau) / vol;

            // Integrals K12 and K3 from the paper
            Func<double, double> k12_integrand = y =>
            {
                double m = 0.25 * tau * Math.Pow(1 + y, 2);
                (double dp, _) = CalculateD(m, b / GetBoundary(tau - m));
                return Math.Exp(q * (tau - m)) * (0.5 * tau * (y + 1) * Distributions.CumulativeNormal(dp) + stv * Distributions.NormalDensity(dp));
            };
            double K12 = Integrator.Integrate(k12_integrand, -1, 1);

            Func<double, double> k3_integrand = y =>
            {
                double m = 0.25 * tau * Math.Pow(1 + y, 2);
                (_, double dm) = CalculateD(m, b / GetBoundary(tau - m));
                return Math.Exp(r * (tau - m)) * stv * Distributions.NormalDensity(dm);
            };
            double K3 = Integrator.Integrate(k3_integrand, -1, 1);

            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            double N_val = Distributions.NormalDensity(d_minus_K) / v + r * K3;
            double D_val = Distributions.NormalDensity(d_plus_K) / v + Distributions.CumulativeNormal(d_plus_K) + q * K12;

            double fv = (D_val > 1e-12) ? K * Math.Exp(-(r - q) * tau) * N_val / D_val : 0.0;
            return (N_val, D_val, fv);
        }

        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            double v_tau = vol * vol * tau;

            double Dd = -Distributions.NormalDensity(d_plus) * d_plus / (b * v_tau) + Distributions.NormalDensity(d_plus) / (b * vol * Math.Sqrt(tau));
            double Nd = -Distributions.NormalDensity(d_minus) * d_minus / (b * v_tau);

            return (Nd, Dd);
        }
    }
    
    /// <summary>
    /// Implements the FP-B formulation, which is the general case.
    /// </summary>
    public class DqFpEquation_B : DqFpEquation
    {
        public DqFpEquation_B(double k, double r, double q, double vol, Func<double, double> getBoundary, IIntegrator integrator)
            : base(k, r, q, vol, getBoundary, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            if (tau < 1e-12)
            {
                return (0.5, 0.5, K * Math.Exp(-(r - q) * tau));
            }

            Func<double, double> n_integrand = u => Math.Exp(r * u) * Distributions.CumulativeNormal(CalculateD(tau - u, b / GetBoundary(u)).dm);
            double ni = Integrator.Integrate(n_integrand, 0, tau);

            Func<double, double> d_integrand = u => Math.Exp(q * u) * Distributions.CumulativeNormal(CalculateD(tau - u, b / GetBoundary(u)).dp);
            double di = Integrator.Integrate(d_integrand, 0, tau);

            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            double N_val = Distributions.CumulativeNormal(d_minus_K) + r * ni;
            double D_val = Distributions.CumulativeNormal(d_plus_K) + q * di;
            
            double fv = (D_val > 1e-12) ? K * Math.Exp(-(r - q) * tau) * N_val / D_val : 0.0;
            return (N_val, D_val, fv);
        }
        
        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            double common = 1.0 / (b * vol * Math.Sqrt(tau));
            return (Distributions.NormalDensity(d_minus) * common, Distributions.NormalDensity(d_plus) * common);
        }
    }
}