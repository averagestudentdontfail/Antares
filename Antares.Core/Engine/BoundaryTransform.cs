using System;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
    public class SpCollocation
    {
        public readonly double T;
        public readonly double xmax;
        public readonly double K;
        public readonly double r;
        public readonly double q;
        public readonly double vol;
        private readonly double _rqDiff;

        public SpCollocation(double T, double xmax, double K, double r, double q, double vol)
        {
            this.T = T;
            this.xmax = xmax;
            this.K = K;
            this.r = r;
            this.q = q;
            this.vol = vol;
            this._rqDiff = r - q;
        }

        public double TemporalTransform(double tau)
        {
            return Math.Sqrt(Math.Max(0.0, Math.Min(1.0, tau / T)));
        }

        public double InverseTemporalTransform(double xi)
        {
            return xi * xi * T;
        }

        public double TransformToBoundary(double H, double tau)
        {
            try
            {
                double G = Math.Sqrt(Math.Max(0.0, H));
                double ratio = Math.Exp(-G);
                ratio = Math.Max(1e-12, Math.Min(1.0 - 1e-12, ratio));
                return xmax * ratio;
            }
            catch (Exception)
            {
                return GetTimeInterpolatedBoundary(tau);
            }
        }

        public double TransformFromBoundary(double boundary, double tau)
        {
            try
            {
                boundary = Math.Max(K * 1e-6, Math.Min(K * 0.999, boundary));
                double ratio = Math.Max(1e-12, boundary / xmax);
                ratio = Math.Max(1e-12, Math.Min(1.0 - 1e-12, ratio));
                double G = -Math.Log(ratio);
                return G * G;
            }
            catch (Exception)
            {
                return 1.0;
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
                        return GetNearExpiryBoundary();
                    }
                    
                    if (tau >= T - 1e-12)
                    {
                        return CalculateAsymptoticBoundary();
                    }
                    
                    double xi = TemporalTransform(tau);
                    double chebyshev_coord = 2.0 * xi - 1.0;
                    chebyshev_coord = Math.Max(-1.0, Math.Min(1.0, chebyshev_coord));
                    
                    double H = boundaryInterp.Value(chebyshev_coord);
                    double boundary = TransformToBoundary(H, tau);
                    
                    return Math.Max(K * 1e-6, Math.Min(K * 0.999, boundary));
                }
                catch (Exception)
                {
                    return GetTimeInterpolatedBoundary(tau);
                }
            };
        }

        private double CalculateAsymptoticBoundary()
        {
            try
            {
                if (Math.Abs(q) < 1e-12)
                {
                    return r <= 0 ? K * 0.9 : K * (r > 0 ? r/(r + 0.5*vol*vol) : 0.1);
                }

                if (r <= q)
                {
                    double mu = r - q - 0.5 * vol * vol;
                    double discriminant = mu * mu + 2.0 * r * vol * vol;
                    
                    if (discriminant > 0 && r > 0)
                    {
                        double lambda_minus = (mu - Math.Sqrt(discriminant)) / (vol * vol);
                        if (lambda_minus < -1e-6)
                        {
                            double boundary = K * lambda_minus / (lambda_minus - 1.0);
                            return Math.Max(K * 0.1, Math.Min(K * 0.9, boundary));
                        }
                    }
                    
                    double ratio = Math.Max(0.1, Math.Min(0.8, Math.Pow(Math.Abs(r/q), 0.5)));
                    return K * ratio;
                }

                double mu_std = _rqDiff - 0.5 * vol * vol;
                double discriminant_std = mu_std * mu_std + 2.0 * r * vol * vol;
                
                if (discriminant_std <= 0)
                {
                    return K * 0.7;
                }
                
                double lambda_minus_std = (mu_std - Math.Sqrt(discriminant_std)) / (vol * vol);
                
                if (lambda_minus_std >= -1e-6)
                {
                    return K * 0.7;
                }
                
                double boundary_std = K * lambda_minus_std / (lambda_minus_std - 1.0);
                return Math.Max(K * 0.1, Math.Min(K * 0.9, boundary_std));
            }
            catch (Exception)
            {
                return K * 0.8;
            }
        }

        private double GetNearExpiryBoundary()
        {
            return r >= q ? K : K * Math.Max(0.1, Math.Min(0.9, Math.Abs(r/q)));
        }

        private double GetTimeInterpolatedBoundary(double tau)
        {
            double nearExpiry = GetNearExpiryBoundary();
            double asymptotic = CalculateAsymptoticBoundary();
            double weight = Math.Min(1.0, tau / Math.Max(T, 1e-6));
            return nearExpiry * (1.0 - weight) + asymptotic * weight;
        }
    }
}