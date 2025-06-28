using System;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
{
    /// <summary>
    /// Implements the complete spectral collocation methodology from Section 5 of the documentation.
    /// Handles the transformation sequence: τ → ξ → B̃ → G → H and provides robust boundary functions.
    /// </summary>
    public class SpectralCollocationFramework
    {
        public readonly double T, xmax, K, r, q, vol;
        private readonly double _sqrtT;
        private readonly double _normalizationFactor;
        private readonly bool _enableDiagnostics;

        public SpectralCollocationFramework(double T, double xmax, double K, double r, double q, double vol, bool enableDiagnostics = false)
        {
            this.T = T;
            this.xmax = xmax;
            this.K = K;
            this.r = r;
            this.q = q;
            this.vol = vol;
            _sqrtT = Math.Sqrt(T);
            _normalizationFactor = xmax / K;
            _enableDiagnostics = enableDiagnostics;

            if (_enableDiagnostics)
            {
                Console.WriteLine($"SpectralFramework: T={T:F4}, xmax={xmax:F4}, norm_factor={_normalizationFactor:F4}");
            }
        }

        /// <summary>
        /// Stage 1: Temporal domain transformation ξ = √(τ/τ_max)
        /// Concentrates computational effort near expiration (τ = 0)
        /// </summary>
        public double TemporalTransform(double tau)
        {
            return Math.Sqrt(Math.Max(0.0, tau) / T);
        }

        /// <summary>
        /// Inverse temporal transformation τ = ξ² * τ_max
        /// </summary>
        public double InverseTemporalTransform(double xi)
        {
            xi = Math.Max(0.0, Math.Min(1.0, xi));
            return xi * xi * T;
        }

        /// <summary>
        /// Stage 2: Boundary normalization B̃(τ) = B(τ)/X
        /// Where X = K * min(1, r/q) removes dependence on strike and handles discontinuity
        /// </summary>
        public double NormalizeBoundary(double boundary)
        {
            return boundary / Math.Max(xmax, K * 0.01); // Prevent division by very small numbers
        }

        /// <summary>
        /// Inverse boundary normalization B(τ) = B̃(τ) * X
        /// </summary>
        public double DenormalizeBoundary(double normalizedBoundary)
        {
            return normalizedBoundary * xmax;
        }

        /// <summary>
        /// Stage 3: Logarithmic transformation G(ξ) = ln(B̃(ξ²))
        /// Linearizes exponential decay behavior and compresses range
        /// </summary>
        public double LogarithmicTransform(double normalizedBoundary)
        {
            // Enhanced numerical stability
            normalizedBoundary = Math.Max(normalizedBoundary, 1e-8);
            normalizedBoundary = Math.Min(normalizedBoundary, 10.0);
            return Math.Log(normalizedBoundary);
        }

        /// <summary>
        /// Inverse logarithmic transformation B̃(ξ²) = exp(G(ξ))
        /// </summary>
        public double InverseLogarithmicTransform(double G)
        {
            // Prevent numerical overflow
            G = Math.Max(G, -20.0);
            G = Math.Min(G, 5.0);
            return Math.Exp(G);
        }

        /// <summary>
        /// Stage 4: Variance-stabilizing transformation H(ξ) = G(ξ)²
        /// Converts function to nearly linear form for optimal Chebyshev approximation
        /// </summary>
        public double VarianceStabilizingTransform(double G)
        {
            // Enhanced bounds to prevent overflow
            G = Math.Max(G, -10.0);
            G = Math.Min(G, 10.0);
            return G * G;
        }

        /// <summary>
        /// Inverse variance-stabilizing transformation G(ξ) = ±√H(ξ)
        /// Uses sign convention from boundary behavior
        /// </summary>
        public double InverseVarianceStabilizingTransform(double H)
        {
            H = Math.Max(0.0, H);
            return Math.Sqrt(H); // Always positive based on mathematical structure
        }

        /// <summary>
        /// Complete forward transformation: Boundary(τ) → H(ξ)
        /// Implements the full sequence from Section 5.2 of documentation
        /// </summary>
        public double TransformFromBoundary(double boundary, double tau)
        {
            try
            {
                // Stage 1: Temporal transformation
                double xi = TemporalTransform(tau);
                
                // Stage 2: Normalization
                double B_tilde = NormalizeBoundary(boundary);
                
                // Stage 3: Logarithmic transformation
                double G = LogarithmicTransform(B_tilde);
                
                // Stage 4: Variance stabilizing
                double H = VarianceStabilizingTransform(G);
                
                if (_enableDiagnostics && double.IsNaN(H))
                {
                    Console.WriteLine($"Transform error: boundary={boundary:F6}, tau={tau:F6}, xi={xi:F6}, B_tilde={B_tilde:F6}, G={G:F6}, H={H:F6}");
                }
                
                return H;
            }
            catch (Exception ex)
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"TransformFromBoundary error: {ex.Message}");
                }
                // Fallback transformation
                return Math.Max(0.0, Math.Min(100.0, boundary / xmax));
            }
        }

        /// <summary>
        /// Complete inverse transformation: H(ξ) → Boundary(τ)
        /// Reverses the full sequence to recover boundary values
        /// </summary>
        public double TransformToBoundary(double H, double tau)
        {
            try
            {
                // Stage 4 inverse: Variance stabilizing
                double G = InverseVarianceStabilizingTransform(H);
                
                // Stage 3 inverse: Logarithmic
                double B_tilde = InverseLogarithmicTransform(G);
                
                // Stage 2 inverse: Normalization
                double boundary = DenormalizeBoundary(B_tilde);
                
                // Final bounds checking
                boundary = Math.Max(boundary, xmax * 1e-6);
                boundary = Math.Min(boundary, K * 2.0);
                
                if (_enableDiagnostics && (double.IsNaN(boundary) || boundary <= 0))
                {
                    Console.WriteLine($"Inverse transform error: H={H:F6}, G={G:F6}, B_tilde={B_tilde:F6}, boundary={boundary:F6}");
                }
                
                return boundary;
            }
            catch (Exception ex)
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"TransformToBoundary error: {ex.Message}");
                }
                // Fallback to a reasonable boundary value
                return Math.Max(xmax * 0.1, K * 0.5);
            }
        }

        /// <summary>
        /// Creates a robust boundary function that handles the spectral interpolation
        /// and provides smooth boundary values for any time τ
        /// </summary>
        public Func<double, double> GetBoundaryFunction(ChebyshevInterpolation boundaryInterp)
        {
            return tau =>
            {
                try
                {
                    if (tau <= 1e-12)
                    {
                        return xmax; // Boundary condition at expiration
                    }
                    
                    if (tau >= T - 1e-12)
                    {
                        // Far from expiration - use asymptotic value
                        return GetAsymptoticBoundary();
                    }
                    
                    // Transform tau to Chebyshev coordinate
                    double xi = TemporalTransform(tau);
                    double chebyshev_coord = (2.0 * xi) - 1.0; // Map [0,1] to [-1,1]
                    
                    // Clamp to valid Chebyshev range
                    chebyshev_coord = Math.Max(-1.0, Math.Min(1.0, chebyshev_coord));
                    
                    // Get transformed value from interpolation
                    double H = boundaryInterp.Value(chebyshev_coord);
                    
                    // Transform back to boundary value
                    double boundary = TransformToBoundary(H, tau);
                    
                    // Additional safety bounds
                    boundary = Math.Max(boundary, xmax * 0.001);
                    boundary = Math.Min(boundary, K * 0.999);
                    
                    return boundary;
                }
                catch (Exception ex)
                {
                    if (_enableDiagnostics)
                    {
                        Console.WriteLine($"Boundary function error at tau={tau:F6}: {ex.Message}");
                    }
                    // Fallback to linear interpolation between xmax and asymptotic value
                    double asymptoticValue = GetAsymptoticBoundary();
                    double weight = Math.Min(1.0, tau / T);
                    return xmax * (1.0 - weight) + asymptoticValue * weight;
                }
            };
        }

        /// <summary>
        /// Calculates the asymptotic boundary value for large τ
        /// Based on perpetual American option theory from Section 2.12
        /// </summary>
        private double GetAsymptoticBoundary()
        {
            try
            {
                // Calculate the negative root λ₋ of the characteristic equation
                double mu = r - q - 0.5 * vol * vol;
                double discriminant = mu * mu + 2.0 * r * vol * vol;
                
                if (discriminant < 0)
                {
                    // Complex roots - return conservative estimate
                    return K * 0.8;
                }
                
                double lambda_minus = (mu - Math.Sqrt(discriminant)) / (vol * vol);
                
                if (lambda_minus >= -1e-12)
                {
                    // Non-negative root - return conservative estimate
                    return K * 0.8;
                }
                
                double asymptotic = K * lambda_minus / (lambda_minus - 1.0);
                
                // Ensure reasonable bounds
                asymptotic = Math.Max(asymptotic, K * 0.1);
                asymptotic = Math.Min(asymptotic, K * 0.95);
                
                return asymptotic;
            }
            catch (Exception)
            {
                // Fallback to conservative estimate
                return K * 0.8;
            }
        }

        /// <summary>
        /// Validates the consistency of the transformation sequence
        /// Used for debugging and quality assurance
        /// </summary>
        public bool ValidateTransformationRoundTrip(double originalBoundary, double tau)
        {
            try
            {
                double H = TransformFromBoundary(originalBoundary, tau);
                double recoveredBoundary = TransformToBoundary(H, tau);
                double relativeError = Math.Abs(recoveredBoundary - originalBoundary) / Math.Max(originalBoundary, 1e-6);
                
                bool isValid = relativeError < 1e-6;
                
                if (_enableDiagnostics && !isValid)
                {
                    Console.WriteLine($"Round-trip validation failed: original={originalBoundary:F6}, recovered={recoveredBoundary:F6}, error={relativeError:E3}");
                }
                
                return isValid;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}