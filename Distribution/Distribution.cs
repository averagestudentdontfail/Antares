using System;
using MathNet.Numerics.Distributions; 

namespace Anderson.Distribution
{
    /// <summary>
    /// Provides standard statistical distribution functions required for option pricing models,
    /// such as the Black-Scholes formula. This implementation leverages the robust and
    /// highly-optimized Math.NET Numerics library.
    /// </summary>
    public static class Distributions
    {
        // A single, static instance of the standard normal distribution for performance.
        private static readonly Normal StandardNormal = new Normal(0, 1);

        /// <summary>
        /// Calculates the Probability Density Function (PDF) of the standard normal distribution (mu=0, sigma=1).
        /// </summary>
        /// <param name="x">The value at which to evaluate the PDF.</param>
        /// <returns>The value of the standard normal PDF at x, i.e., phi(x).</returns>
        public static double NormalDensity(double x)
        {
            return StandardNormal.Density(x);
        }

        /// <summary>
        /// Calculates the Cumulative Distribution Function (CDF) of the standard normal distribution (mu=0, sigma=1).
        /// </summary>
        /// <param name="x">The value at which to evaluate the CDF.</param>
        /// <returns>The cumulative probability up to x, i.e., N(x).</returns>
        public static double CumulativeNormal(double x)
        {
            return StandardNormal.CumulativeDistribution(x);
        }

        /// <summary>
        /// Calculates the Inverse Cumulative Distribution Function (Inverse CDF or Quantile Function)
        /// of the standard normal distribution (mu=0, sigma=1).
        /// </summary>
        /// <param name="p">The cumulative probability (must be between 0 and 1).</param>
        /// <returns>The value x such that CDF(x) = p.</returns>
        public static double InverseCumulativeNormal(double p)
        {
            if (p <= 0.0) return double.NegativeInfinity;
            if (p >= 1.0) return double.PositiveInfinity;
            
            // This leverages Math.NET's high-precision implementation, which is often
            // based on Peter Acklam's algorithm, the same as used in modern QuantLib.
            return StandardNormal.InverseCumulativeDistribution(p);
        }
    }
}