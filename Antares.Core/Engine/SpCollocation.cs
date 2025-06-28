using System;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
    public class SpCollocation
    {
        public readonly double T, xmax, K, r, q, vol;
        private readonly double _sqrtT;

        public SpCollocation(double T, double xmax, double K, double r, double q, double vol)
        {
            this.T = T;
            this.xmax = xmax;
            this.K = K;
            this.r = r;
            this.q = q;
            this.vol = vol;
            _sqrtT = Math.Sqrt(T);
        }

        public double TemporalTransform(double tau)
        {
            return Math.Sqrt(Math.Max(0.0, tau) / T);
        }

        public double InverseTemporalTransform(double xi)
        {
            xi = Math.Max(0.0, Math.Min(1.0, xi));
            return xi * xi * T;
        }

        public double NormalizeBoundary(double boundary)
        {
            return boundary / Math.Max(xmax, K * 0.01);
        }

        public double DenormalizeBoundary(double normalizedBoundary)
        {
            return normalizedBoundary * xmax;
        }

        public double LogarithmicTransform(double normalizedBoundary)
        {
            normalizedBoundary = Math.Max(normalizedBoundary, 1e-8);
            normalizedBoundary = Math.Min(normalizedBoundary, 10.0);
            return Math.Log(normalizedBoundary);
        }

        public double InverseLogarithmicTransform(double G)
        {
            G = Math.Max(G, -20.0);
            G = Math.Min(G, 5.0);
            return Math.Exp(G);
        }

        public double VarianceStabilizingTransform(double G)
        {
            G = Math.Max(G, -10.0);
            G = Math.Min(G, 10.0);
            return G * G;
        }

        public double InverseVarianceStabilizingTransform(double H)
        {
            H = Math.Max(0.0, H);
            return Math.Sqrt(H);
        }

        public double TransformFromBoundary(double boundary, double tau)
        {
            try
            {
                double B_tilde = NormalizeBoundary(boundary);
                double G = LogarithmicTransform(B_tilde);
                double H = VarianceStabilizingTransform(G);
                return H;
            }
            catch (Exception)
            {
                return Math.Max(0.0, Math.Min(100.0, boundary / xmax));
            }
        }

        public double TransformToBoundary(double H, double tau)
        {
            try
            {
                double G = InverseVarianceStabilizingTransform(H);
                double B_tilde = InverseLogarithmicTransform(G);
                double boundary = DenormalizeBoundary(B_tilde);
                
                boundary = Math.Max(boundary, xmax * 1e-6);
                boundary = Math.Min(boundary, K * 2.0);
                
                return boundary;
            }
            catch (Exception)
            {
                return Math.Max(xmax * 0.1, K * 0.5);
            }
        }

        public Func<double, double> GetBoundaryFunction(ChebyshevInterpolation boundaryInterp)
        {
            return tau =>
            {
                try
                {
                    if (tau <= 1e-12)
                    {
                        return xmax;
                    }
                    
                    if (tau >= T - 1e-12)
                    {
                        return GetAsymptoticBoundary();
                    }
                    
                    double xi = TemporalTransform(tau);
                    double chebyshev_coord = (2.0 * xi) - 1.0;
                    
                    chebyshev_coord = Math.Max(-1.0, Math.Min(1.0, chebyshev_coord));
                    
                    double H = boundaryInterp.Value(chebyshev_coord);
                    double boundary = TransformToBoundary(H, tau);
                    
                    boundary = Math.Max(boundary, xmax * 0.001);
                    boundary = Math.Min(boundary, K * 0.999);
                    
                    return boundary;
                }
                catch (Exception)
                {
                    double asymptoticValue = GetAsymptoticBoundary();
                    double weight = Math.Min(1.0, tau / T);
                    return xmax * (1.0 - weight) + asymptoticValue * weight;
                }
            };
        }

        private double GetAsymptoticBoundary()
        {
            try
            {
                double mu = r - q - 0.5 * vol * vol;
                double discriminant = mu * mu + 2.0 * r * vol * vol;
                
                if (discriminant < 0)
                {
                    return K * 0.8;
                }
                
                double lambda_minus = (mu - Math.Sqrt(discriminant)) / (vol * vol);
                
                if (lambda_minus >= -1e-12)
                {
                    return K * 0.8;
                }
                
                double asymptotic = K * lambda_minus / (lambda_minus - 1.0);
                
                asymptotic = Math.Max(asymptotic, K * 0.1);
                asymptotic = Math.Min(asymptotic, K * 0.95);
                
                return asymptotic;
            }
            catch (Exception)
            {
                return K * 0.8;
            }
        }
    }
}