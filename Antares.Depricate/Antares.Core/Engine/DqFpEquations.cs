using System;
using Antares.Distribution;
using Antares.Integrator;

namespace Antares.Engine
{
    public abstract class DqFpEquation
    {
        protected readonly double K;
        protected readonly double r;
        protected readonly double q;
        protected readonly double vol;
        protected readonly Func<double, double> boundaryFunction;
        protected readonly IIntegrator integrator;
        protected readonly StandardNormalDistribution phi;

        protected DqFpEquation(double K, double r, double q, double vol, Func<double, double> boundaryFunction, IIntegrator integrator)
        {
            this.K = K;
            this.r = r;
            this.q = q;
            this.vol = vol;
            this.boundaryFunction = boundaryFunction;
            this.integrator = integrator;
            this.phi = new StandardNormalDistribution();
        }

        public abstract (double N, double D, double Fv) F(double tau, double b);
        public abstract (double Nd, double Dd) NDd(double tau, double b);

        protected (double dPlus, double dMinus) CalculateD(double tau, double z)
        {
            if (tau <= 1e-12) return (0.0, 0.0);
            
            double sqrtTau = Math.Sqrt(tau);
            double logZ = Math.Log(Math.Max(1e-12, z));
            double drift = (r - q) * tau;
            double volTerm = 0.5 * vol * vol * tau;
            
            double dPlus = (logZ + drift + volTerm) / (vol * sqrtTau);
            double dMinus = (logZ + drift - volTerm) / (vol * sqrtTau);
            
            return (dPlus, dMinus);
        }
    }

    public class DqFpEquation_A : DqFpEquation
    {
        public DqFpEquation_A(double K, double r, double q, double vol, Func<double, double> boundaryFunction, IIntegrator integrator)
            : base(K, r, q, vol, boundaryFunction, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            try
            {
                if (tau <= 1e-12)
                {
                    double fv = r >= q ? K : K * Math.Max(0.1, Math.Min(0.9, Math.Abs(r/q)));
                    return (1.0, 1.0, fv);
                }

                var (dPlus, dMinus) = CalculateD(tau, b / K);
                
                double europeanTerm = Math.Exp(-q * tau) * phi.CumulativeDistribution(-dPlus);
                
                Func<double, double> integrandR = u =>
                {
                    if (u >= tau - 1e-12) return 0.0;
                    double Bu = boundaryFunction(u);
                    var (dPlusU, dMinusU) = CalculateD(tau - u, b / Bu);
                    return r * K * Math.Exp(-r * (tau - u)) * phi.CumulativeDistribution(-dMinusU);
                };
                
                Func<double, double> integrandQ = u =>
                {
                    if (u >= tau - 1e-12) return 0.0;
                    double Bu = boundaryFunction(u);
                    var (dPlusU, dMinusU) = CalculateD(tau - u, b / Bu);
                    return q * b * Math.Exp(-q * (tau - u)) * phi.CumulativeDistribution(-dPlusU);
                };

                double integralR = integrator.Integrate(integrandR, 0.0, tau);
                double integralQ = integrator.Integrate(integrandQ, 0.0, tau);

                double N = K - integralR + integralQ;
                double D = 1.0 + europeanTerm;
                double fv = Math.Abs(D) > 1e-12 ? N / D : b;

                fv = Math.Max(K * 1e-6, Math.Min(K * 0.999, fv));

                return (N, D, fv);
            }
            catch (Exception)
            {
                double fv = r >= q ? K * 0.9 : K * 0.5;
                return (1.0, 1.0, fv);
            }
        }

        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            try
            {
                if (tau <= 1e-12) return (0.0, 1.0);

                var (dPlus, dMinus) = CalculateD(tau, b / K);
                
                double sqrtTau = Math.Sqrt(tau);
                double Nd = -q * Math.Exp(-q * tau) * phi.Density(-dPlus) / (vol * sqrtTau);
                
                Func<double, double> derivativeIntegrand = u =>
                {
                    if (u >= tau - 1e-12) return 0.0;
                    double Bu = boundaryFunction(u);
                    var (dPlusU, dMinusU) = CalculateD(tau - u, b / Bu);
                    double sqrtTauU = Math.Sqrt(tau - u);
                    return (r * K / b) * Math.Exp(-r * (tau - u)) * phi.Density(-dMinusU) / (vol * sqrtTauU) -
                           q * Math.Exp(-q * (tau - u)) * (phi.Density(-dPlusU) / (vol * sqrtTauU) + phi.CumulativeDistribution(-dPlusU));
                };

                double Dd = integrator.Integrate(derivativeIntegrand, 0.0, tau);

                return (Nd, Dd);
            }
            catch (Exception)
            {
                return (0.0, 1.0);
            }
        }
    }

    public class DqFpEquation_B : DqFpEquation
    {
        public DqFpEquation_B(double K, double r, double q, double vol, Func<double, double> boundaryFunction, IIntegrator integrator)
            : base(K, r, q, vol, boundaryFunction, integrator) { }

        public override (double N, double D, double Fv) F(double tau, double b)
        {
            try
            {
                if (tau <= 1e-12)
                {
                    double fv = r >= q ? K : K * Math.Max(0.1, Math.Min(0.9, Math.Abs(r/q)));
                    return (1.0, 1.0, fv);
                }

                var (dPlus, dMinus) = CalculateD(tau, b / K);
                
                double smoothPastingBase = -Math.Exp(-q * tau) * phi.CumulativeDistribution(-dPlus);
                
                Func<double, double> integrandR = u =>
                {
                    if (u >= tau - 1e-12) return 0.0;
                    double Bu = boundaryFunction(u);
                    var (dPlusU, dMinusU) = CalculateD(tau - u, b / Bu);
                    double sqrtTauU = Math.Sqrt(tau - u);
                    return (r * K / b) * Math.Exp(-r * (tau - u)) * phi.Density(-dMinusU) / (vol * sqrtTauU);
                };
                
                Func<double, double> integrandQ = u =>
                {
                    if (u >= tau - 1e-12) return 0.0;
                    double Bu = boundaryFunction(u);
                    var (dPlusU, dMinusU) = CalculateD(tau - u, b / Bu);
                    double sqrtTauU = Math.Sqrt(tau - u);
                    return q * Math.Exp(-q * (tau - u)) * 
                           (phi.Density(-dPlusU) / (vol * sqrtTauU) + phi.CumulativeDistribution(-dPlusU));
                };

                double integralR = integrator.Integrate(integrandR, 0.0, tau);
                double integralQ = integrator.Integrate(integrandQ, 0.0, tau);

                double N = -1.0 + smoothPastingBase + integralR - integralQ;
                double D = 1.0;

                double alpha = Math.Abs(r - q) < 1e-6 ? r : Math.Abs(r / q);
                double fv = Math.Abs(N) < 1e-12 ? b : alpha * K / Math.Max(1e-12, Math.Abs(N));
                
                fv = Math.Max(K * 1e-6, Math.Min(K * 0.999, fv));

                return (N, D, fv);
            }
            catch (Exception)
            {
                double fv = r >= q ? K * 0.9 : K * 0.5;
                return (1.0, 1.0, fv);
            }
        }

        public override (double Nd, double Dd) NDd(double tau, double b)
        {
            try
            {
                if (tau <= 1e-12) return (0.0, 1.0);

                var (dPlus, dMinus) = CalculateD(tau, b / K);
                double sqrtTau = Math.Sqrt(tau);
                
                double Nd = phi.Density(-dPlus) / (b * vol * sqrtTau);
                double Dd = phi.Density(-dMinus) / (b * vol * sqrtTau);

                return (Nd, Dd);
            }
            catch (Exception)
            {
                return (0.0, 1.0);
            }
        }
    }
}