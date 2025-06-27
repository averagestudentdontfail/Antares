using System;
using Anderson.Distribution;
using Anderson.Integrator;

namespace Anderson.Engine
{
    /// <summary>
    /// Abstract base class for the fixed-point iteration equations (FP-A and FP-B).
    /// It defines the contract for calculating the numerator (N), denominator (D),
    /// and the resulting fixed-point value (Fv) of the boundary equation.
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
        /// Calculates the Numerator (N), Denominator (D), and the resulting Fixed-point Value (Fv) for the boundary equation at a given time tau and boundary value b.
        /// </summary>
        public abstract (double N, double D, double Fv) F(double tau, double b);

        /// <summary>
        /// Calculates the derivatives of the non-integral parts of the Numerator (Nd) and Denominator (Dd) with respect to the boundary value.
        /// Used for the Jacobi-Newton iteration step.
        /// </summary>
        public abstract (double Nd, double Dd) NDd(double tau, double b);

        /// <summary>
        /// Helper method to calculate the Black-Scholes d-plus and d-minus values.
        /// </summary>
        protected (double dp, double dm) CalculateD(double t, double z)
        {
            double v = vol * Math.Sqrt(t);
            if (v < 1e-14) return (double.PositiveInfinity * Math.Sign(Math.Log(z)), double.PositiveInfinity * Math.Sign(Math.Log(z)));
            
            double m = (Math.Log(z) + (r - q) * t) / v;
            return (m + 0.5 * v, m - 0.5 * v);
        }
        
        /// <summary>
        /// Helper method for floating point comparison.
        /// </summary>
        protected static bool IsClose(double a, double b)
        {
            return Math.Abs(a - b) < 1e-9;
        }
    }

    /// <summary>
    /// Implements the "System A" (FP-A) fixed-point equation, derived from the smooth-pasting condition.
    /// This version contains the corrected discount factor implementation.
    /// </summary>
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

            // Integrand for the K1 and K2 terms in the denominator, using the corrected discount factor.
            Func<double, double> k12_integrand = y =>
            {
                double m = 0.25 * tau * Math.Pow(1 + y, 2);
                (double dp, _) = CalculateD(m, b / GetBoundary(tau - m));
                // CORRECTED: The exponential term's sign is now negative, aligning with the paper's variable transformation.
                return Math.Exp(-q * m) * (0.5 * tau * (y + 1) * Distributions.CumulativeNormal(dp) + sqrt_tau / vol * Distributions.NormalDensity(dp));
            };
            // The e^(q*tau) term is applied here, outside the integral, as per the paper's formulation.
            double K12 = Math.Exp(q * tau) * Integrator.Integrate(k12_integrand, -1, 1);

            // Integrand for the K3 term in the numerator, using the corrected discount factor.
            Func<double, double> k3_integrand = y =>
            {
                double m = 0.25 * tau * Math.Pow(1 + y, 2);
                (_, double dm) = CalculateD(m, b / GetBoundary(tau - m));
                // CORRECTED: The exponential term's sign is now negative.
                return Math.Exp(-r * m) * sqrt_tau / vol * Distributions.NormalDensity(dm);
            };
            // The e^(r*tau) term is applied here.
            double K3 = Math.Exp(r * tau) * Integrator.Integrate(k3_integrand, -1, 1);
            
            (double d_plus_K, double d_minus_K) = CalculateD(tau, b / K);
            double N_val = Distributions.NormalDensity(d_minus_K) / v + r * K3;
            double D_val = Distributions.NormalDensity(d_plus_K) / v + Distributions.CumulativeNormal(d_plus_K) + q * K12;
            
            double fv = (D_val > 1e-12) ? K * Math.Exp(-(r - q) * tau) * N_val / D_val : 0.0;
            return (N_val, D_val, fv);
        }

        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            (double d_plus, double d_minus) = CalculateD(tau, b / K);
            double v_tau_sq = vol * vol * tau;
            
            double Dd = -Distributions.NormalDensity(d_plus) * d_plus / (b * v_tau_sq) + Distributions.NormalDensity(d_plus) / (b * vol * Math.Sqrt(tau));
            double Nd = -Distributions.NormalDensity(d_minus) * d_minus / (b * v_tau_sq);

            return (Nd, Dd);
        }
    }
    
    /// <summary>
    /// Implements the "System B" (FP-B) fixed-point equation, derived from the value-matching condition.
    /// </summary>
    public class DqFpEquation_B : DqFpEquation
    {
        public DqFpEquation_B(double k, double r, double q, double vol, Func<double, double> getBoundary, IIntegrator integrator)
            : base(k, r, q, vol, getBoundary, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            if (tau < 1e-12)
            {
                return (IsClose(b, K)) ? (0.5, 0.5, K * Math.Exp(-(r - q) * tau)) : ((b < K) ? (0.0, 0.0, 0.0) : (1.0, 1.0, K * Math.Exp(-(r-q)*tau)));
            }

            Func<double, double> n_integrand = u =>
            {
                double df = Math.Exp(r * u);
                if (u >= tau * (1 - 5e-9))
                {
                    return IsClose(b, GetBoundary(u)) ? 0.5 * df : df * ((b < GetBoundary(u)) ? 0.0 : 1.0);
                }
                return df * Distributions.CumulativeNormal(CalculateD(tau - u, b / GetBoundary(u)).dm);
            };
            double ni = Integrator.Integrate(n_integrand, 0, tau);

            Func<double, double> d_integrand = u =>
            {
                double df = Math.Exp(q * u);
                if (u >= tau * (1 - 5e-9))
                {
                    return IsClose(b, GetBoundary(u)) ? 0.5 * df : df * ((b < GetBoundary(u)) ? 0.0 : 1.0);
                }
                return df * Distributions.CumulativeNormal(CalculateD(tau - u, b / GetBoundary(u)).dp);
            };
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