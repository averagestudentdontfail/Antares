using System;
using Anderson.Distribution;
using Anderson.Integrator;

namespace Anderson.Engine
{
    /// <summary>
    /// Abstract base class for the fixed-point equation formulations (FP-A and FP-B).
    /// These formulations represent the American option exercise boundary as an integral equation.
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

        /// <summary>
        /// Evaluates the fixed-point function F(B) = K*exp(...) * N/D.
        /// </summary>
        /// <returns>A tuple containing the numerator (N), denominator (D), and the resulting value F(B).</returns>
        public abstract (double N, double D, double Fv) F(double tau, double b);
        
        /// <summary>
        /// Evaluates the derivative of the numerator (Nd) and denominator (Dd) with respect to the boundary B.
        /// This is used in the Jacobi-Newton iteration step.
        /// </summary>
        public abstract (double Nd, double Dd) NDd(double tau, double b);

        protected (double dp, double dm) CalculateD(double t, double z)
        {
            double v = vol * Math.Sqrt(t);
            // Handle edge case where t is very small
            if (v < 1e-14) return (double.PositiveInfinity * Math.Sign(Math.Log(z)), double.PositiveInfinity * Math.Sign(Math.Log(z)));
            
            double m = (Math.Log(z) + (r - q) * t) / v;
            return (m + 0.5 * v, m - 0.5 * v);
        }
    }

    /// <summary>
    /// Implements the FP-A formulation from Andersen et al. (2015).
    /// This formulation is generally more stable for low drift (r ≈ q) cases.
    /// </summary>
    public class DqFpEquation_A : DqFpEquation
    {
        public DqFpEquation_A(double k, double r, double q, double vol, Func<double, double> getBoundary, IIntegrator integrator)
            : base(k, r, q, vol, getBoundary, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            if (tau < 1e-12)
            {
                // Limiting case for tau -> 0, where B(tau) -> XMax.
                // N and D both approach 0.5, so Fv -> K*exp(-(r-q)tau).
                return (0.5, 0.5, K * Math.Exp(-(r - q) * tau));
            }

            double sqrt_tau = Math.Sqrt(tau);
            double v_inv = 1.0 / (vol * sqrt_tau);

            // Integrals K1, K2, K3 from the paper (eq. 18-20), adapted for the sqrt-time transformation
            Func<double, double> k1_integrand = y =>
            {
                double u_of_y = tau - 0.25 * tau * Math.Pow(1 + y, 2);
                (double dp, _) = CalculateD(tau - u_of_y, b / GetBoundary(u_of_y));
                return 0.5 * tau * (1 + y) * Math.Exp(q * u_of_y) * Distributions.CumulativeNormal(dp);
            };
            
            Func<double, double> k2_integrand = y =>
            {
                double u_of_y = tau - 0.25 * tau * Math.Pow(1 + y, 2);
                (double dp, _) = CalculateD(tau - u_of_y, b / GetBoundary(u_of_y));
                return sqrt_tau * Math.Exp(q * u_of_y) * Distributions.NormalDensity(dp) / vol;
            };

            Func<double, double> k3_integrand = y =>
            {
                double u_of_y = tau - 0.25 * tau * Math.Pow(1 + y, 2);
                (_, double dm) = CalculateD(tau - u_of_y, b / GetBoundary(u_of_y));
                return sqrt_tau * Math.Exp(r * u_of_y) * Distributions.NormalDensity(dm) / vol;
            };

            double K1 = Integrator.Integrate(k1_integrand, -1, 1);
            double K2 = Integrator.Integrate(k2_integrand, -1, 1);
            double K3 = Integrator.Integrate(k3_integrand, -1, 1);
            
            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            double N_val = Distributions.NormalDensity(d_minus_K) * v_inv + r * K3;
            double D_val = Distributions.NormalDensity(d_plus_K) * v_inv + Distributions.CumulativeNormal(d_plus_K) + q * (K1 + K2);
            
            double fv = (D_val > 1e-12) ? K * Math.Exp(-(r - q) * tau) * N_val / D_val : 0.0;
            return (N_val, D_val, fv);
        }

        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            double v_tau = vol * vol * tau;
            
            // Derivatives ignoring integral terms, which is sufficient for partial Jacobi-Newton step
            double Dd = -Distributions.NormalDensity(d_plus) * d_plus / (b * v_tau) + Distributions.NormalDensity(d_plus) / (b * vol * Math.Sqrt(tau));
            double Nd = -Distributions.NormalDensity(d_minus) * d_minus / (b * v_tau);

            return (Nd, Dd);
        }
    }
    
    /// <summary>
    /// Implements the FP-B formulation from Andersen et al. (2015).
    /// This is the more general and often more robust case, especially for high drift.
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