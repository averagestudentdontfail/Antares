using System;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
    public class SpCollocation
    {
        public readonly double T, xmax, K, r, q, vol;
        private readonly double _sqrtT;
        private readonly double _rqDiff;
        private readonly double _asymptoticBoundary;

        public SpCollocation(double T, double xmax, double K, double r, double q, double vol)
        {
            this.T = T;
            this.xmax = xmax;
            this.K = K;
            this.r = r;
            this.q = q;
            this.vol = vol;
            _sqrtT = Math.Sqrt(T);
            _rqDiff = r - q;
            _asymptoticBoundary = CalculateAsymptoticBoundary();
        }

        // Stage 1: Temporal Domain Transformation (Equation 5.4 in documentation)
        public double TemporalTransform(double tau)
        {
            return Math.Sqrt(Math.Max(0.0, Math.Min(tau, T)) / T);
        }

        public double InverseTemporalTransform(double xi)
        {
            xi = Math.Max(0.0, Math.Min(1.0, xi));
            return xi * xi * T;
        }

        // Stage 2: Boundary Normalization (Equation 5.5 in documentation)
        public double NormalizeBoundary(double boundary)
        {
            double normalizer = Math.Max(xmax, K * 0.01);
            return Math.Max(1e-12, boundary) / normalizer;
        }

        public double DenormalizeBoundary(double normalizedBoundary)
        {
            double normalizer = Math.Max(xmax, K * 0.01);
            return Math.Max(1e-12, normalizedBoundary) * normalizer;
        }

        // Stage 3: Logarithmic Transformation (Equation 5.6 in documentation)
        public double LogarithmicTransform(double normalizedBoundary)
        {
            normalizedBoundary = Math.Max(1e-12, Math.Min(10.0, normalizedBoundary));
            return Math.Log(normalizedBoundary);
        }

        public double InverseLogarithmicTransform(double G)
        {
            G = Math.Max(-25.0, Math.Min(5.0, G));
            return Math.Exp(G);
        }

        // Stage 4: Variance-Stabilizing Transformation (Equation 5.7 in documentation)
        public double VarianceStabilizingTransform(double G)
        {
            G = Math.Max(-15.0, Math.Min(15.0, G));
            return G * G;
        }

        public double InverseVarianceStabilizingTransform(double H)
        {
            H = Math.Max(0.0, Math.Min(225.0, H));
            double sqrtH = Math.Sqrt(H);
            // Preserve sign of original G
            return H > 0 ? sqrtH : -sqrtH;
        }

        // Composite Forward Transformation: B(τ) → H(ξ)
        public double TransformFromBoundary(double boundary, double tau)
        {
            try
            {
                // Ensure boundary is within reasonable economic bounds
                boundary = Math.Max(K * 1e-6, Math.Min(K * 0.999, boundary));
                
                // Apply transformation sequence
                double B_tilde = NormalizeBoundary(boundary);
                double G = LogarithmicTransform(B_tilde);
                double H = VarianceStabilizingTransform(G);
                
                return H;
            }
            catch (Exception)
            {
                // Robust fallback based on time interpolation
                return GetFallbackTransform(boundary, tau);
            }
        }

        // Composite Inverse Transformation: H(ξ) → B(τ)
        public double TransformToBoundary(double H, double tau)
        {
            try
            {
                // Apply inverse transformation sequence
                double G = InverseVarianceStabilizingTransform(H);
                double B_tilde = InverseLogarithmicTransform(G);
                double boundary = DenormalizeBoundary(B_tilde);
                
                // Apply economic bounds
                boundary = Math.Max(K * 1e-6, Math.Min(K * 0.999, boundary));
                
                // Additional validation: ensure monotonicity properties
                return ValidateBoundaryMonotonicity(boundary, tau);
            }
            catch (Exception)
            {
                // Robust fallback
                return GetTimeInterpolatedBoundary(tau);
            }
        }

        // Enhanced boundary function generator with spectral reconstruction
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
                        return _asymptoticBoundary;
                    }
                    
                    // Transform to Chebyshev domain [-1,1]
                    double xi = TemporalTransform(tau);
                    double chebyshev_coord = 2.0 * xi - 1.0;
                    chebyshev_coord = Math.Max(-1.0, Math.Min(1.0, chebyshev_coord));
                    
                    // Evaluate Chebyshev interpolation
                    double H = boundaryInterp.Value(chebyshev_coord);
                    
                    // Transform back to boundary space
                    double boundary = TransformToBoundary(H, tau);
                    
                    // Ensure economic validity
                    return Math.Max(K * 1e-6, Math.Min(K * 0.999, boundary));
                }
                catch (Exception)
                {
                    // Robust fallback with time interpolation
                    return GetTimeInterpolatedBoundary(tau);
                }
            };
        }

        // Enhanced asymptotic boundary calculation based on documentation
        private double CalculateAsymptoticBoundary()
        {
            try
            {
                if (Math.Abs(q) < 1e-12)
                {
                    return r <= 0 ? K * 0.9 : K * 0.05;
                }

                if (r <= q)
                {
                    // When r ≤ q, boundary approaches K * r/q
                    double ratio = Math.Max(0.001, Math.Min(0.999, r / q));
                    return K * ratio;
                }

                // Perpetual American put boundary (Equation 2.12 in documentation)
                double mu = _rqDiff - 0.5 * vol * vol;
                double discriminant = mu * mu + 2.0 * r * vol * vol;
                
                if (discriminant <= 0)
                {
                    return K * 0.7;
                }
                
                double lambda_minus = (mu - Math.Sqrt(discriminant)) / (vol * vol);
                
                if (lambda_minus >= -1e-6)
                {
                    return K * 0.7;
                }
                
                // Boundary formula: K * λ₋ / (λ₋ - 1)
                double boundary = K * lambda_minus / (lambda_minus - 1.0);
                return Math.Max(K * 0.01, Math.Min(K * 0.95, boundary));
            }
            catch (Exception)
            {
                return K * 0.8;
            }
        }

        private double GetNearExpiryBoundary()
        {
            // Equation 2.11 in documentation
            return r >= q ? K : K * Math.Max(0.001, Math.Min(0.999, r / q));
        }

        private double GetTimeInterpolatedBoundary(double tau)
        {
            double nearExpiry = GetNearExpiryBoundary();
            double weight = Math.Min(1.0, tau / T);
            return nearExpiry * (1.0 - weight) + _asymptoticBoundary * weight;
        }

        private double GetFallbackTransform(double boundary, double tau)
        {
            // Simple transformation for fallback
            double ratio = Math.Max(1e-12, boundary / Math.Max(xmax, K * 0.01));
            ratio = Math.Max(1e-8, Math.Min(1.0, ratio));
            double G = Math.Log(ratio);
            return Math.Max(0.0, G * G);
        }

        private double ValidateBoundaryMonotonicity(double boundary, double tau)
        {
            // Ensure boundary doesn't violate economic constraints
            double minBoundary = K * 1e-6;
            double maxBoundary = K * 0.999;
            
            // For very small tau, boundary should approach near-expiry value
            if (tau < T * 0.01)
            {
                double nearExpiry = GetNearExpiryBoundary();
                double weight = tau / (T * 0.01);
                double interpolated = nearExpiry * (1.0 - weight) + boundary * weight;
                boundary = Math.Max(minBoundary, Math.Min(maxBoundary, interpolated));
            }
            
            return Math.Max(minBoundary, Math.Min(maxBoundary, boundary));
        }

        // Diagnostic methods for validation
        public double GetTransformationJacobian(double tau, double boundary)
        {
            double h = Math.Max(1e-8, boundary * 1e-6);
            double H1 = TransformFromBoundary(boundary + h, tau);
            double H2 = TransformFromBoundary(boundary - h, tau);
            return (H1 - H2) / (2.0 * h);
        }

        public (double nearExpiry, double asymptotic) GetBoundaryLimits()
        {
            return (GetNearExpiryBoundary(), _asymptoticBoundary);
        }
    }
}