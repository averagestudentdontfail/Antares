// C# code for GammaDistribution.cs

using System;
using MathNet.Numerics;
using MathNet.Numerics.Distributions;

namespace Antares.Math.Distribution
{
    /// <summary>
    /// Cumulative gamma distribution function.
    /// </summary>
    /// <remarks>
    /// This implementation replaces the custom series-expansion logic from the
    /// original C++ code with a call to the robust and optimized Gamma distribution
    /// functions available in the MathNet.Numerics library.
    /// </remarks>
    public class CumulativeGammaDistribution
    {
        private readonly double _a; // The shape parameter (alpha)

        /// <summary>
        /// Initializes a new instance of the CumulativeGammaDistribution class.
        /// </summary>
        /// <param name="a">The shape parameter 'a' (alpha) of the distribution. Must be positive.</param>
        public CumulativeGammaDistribution(double a)
        {
            if (a <= 0.0)
                throw new ArgumentException("Invalid parameter for gamma distribution. 'a' must be positive.", nameof(a));
            _a = a;
        }

        /// <summary>
        /// Calculates the cumulative distribution function (CDF) at a given point x.
        /// </summary>
        /// <param name="x">The value at which to evaluate the CDF.</param>
        /// <returns>The probability P(X &lt;= x) for a Gamma(a, 1.0) random variable X.</returns>
        public double Value(double x)
        {
            if (x <= 0.0)
                return 0.0;

            // The standard gamma distribution has a rate (beta) of 1.0.
            return Gamma.CDF(_a, 1.0, x);
        }
    }

    /// <summary>
    /// Gamma function class.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The gamma function is defined as Gamma(z) = integral from 0 to infinity of t^(z-1)*e^(-t) dt.
    /// </para>
    /// <para>
    /// The implementation of the algorithm from the original C++ code (a Lanczos
    /// approximation) has been replaced by calls to the highly optimized special
    /// functions in the MathNet.Numerics library.
    /// </para>
    /// </remarks>
    public class GammaFunction
    {
        /// <summary>
        /// Calculates the value of the Gamma function, Gamma(x).
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The value of Gamma(x).</returns>
        public double Value(double x)
        {
            return SpecialFunctions.Gamma(x);
        }

        /// <summary>
        /// Calculates the natural logarithm of the Gamma function, ln(Gamma(x)).
        /// </summary>
        /// <param name="x">The input value. Must be positive.</param>
        /// <returns>The value of ln(Gamma(x)).</returns>
        public double LogValue(double x)
        {
            if (x <= 0.0)
                throw new ArgumentException("Positive argument required for LogValue.", nameof(x));

            return SpecialFunctions.GammaLn(x);
        }
    }
}