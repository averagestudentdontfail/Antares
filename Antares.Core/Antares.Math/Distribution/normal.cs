// C# code for NormalDistribution.cs

using System;
using MathNet.Numerics.Distributions;

namespace Antares.Math.Distribution
{
    /// <summary>
    /// Normal distribution function (PDF).
    /// </summary>
    /// <remarks>
    /// Given x, it returns its probability in a Gaussian normal distribution.
    /// It provides the first derivative too.
    /// This implementation wraps the high-performance MathNet.Numerics library.
    /// </remarks>
    public class NormalDistribution
    {
        private readonly double _average;
        private readonly double _sigma;
        private readonly Normal _distribution;

        /// <summary>
        /// Initializes a new instance of the NormalDistribution class.
        /// </summary>
        /// <param name="average">The mean (mu) of the distribution.</param>
        /// <param name="sigma">The standard deviation (sigma) of the distribution.</param>
        public NormalDistribution(double average = 0.0, double sigma = 1.0)
        {
            if (sigma <= 0.0)
                throw new ArgumentException($"Sigma must be greater than 0.0 ({sigma} not allowed).");

            _average = average;
            _sigma = sigma;
            _distribution = new Normal(_average, _sigma);
        }

        /// <summary>
        /// Calculates the probability density function (PDF) at a given point x.
        /// </summary>
        public double Value(double x) => _distribution.Density(x);

        /// <summary>
        /// Calculates the derivative of the probability density function at a given point x.
        /// </summary>
        public double Derivative(double x) => -((x - _average) / (_sigma * _sigma)) * Value(x);
    }

    /// <summary>
    /// Alias for NormalDistribution.
    /// </summary>
    public class GaussianDistribution : NormalDistribution
    {
        public GaussianDistribution(double average = 0.0, double sigma = 1.0) : base(average, sigma) { }
    }


    /// <summary>
    /// Cumulative normal distribution function (CDF).
    /// </summary>
    /// <remarks>
    /// Given x, it provides the integral of the Gaussian normal distribution up to x.
    /// This implementation wraps the high-performance MathNet.Numerics library, replacing
    /// the custom approximation and tail-expansion logic from the original C++ code.
    /// </remarks>
    public class CumulativeNormalDistribution
    {
        private readonly Normal _distribution;
        private readonly NormalDistribution _gaussian;

        /// <summary>
        /// Initializes a new instance of the CumulativeNormalDistribution class.
        /// </summary>
        /// <param name="average">The mean (mu) of the distribution.</param>
        /// <param name="sigma">The standard deviation (sigma) of the distribution.</param>
        public CumulativeNormalDistribution(double average = 0.0, double sigma = 1.0)
        {
            if (sigma <= 0.0)
                throw new ArgumentException($"Sigma must be greater than 0.0 ({sigma} not allowed).");
            
            _distribution = new Normal(average, sigma);
            _gaussian = new NormalDistribution(average, sigma);
        }

        /// <summary>
        /// Calculates the cumulative distribution function (CDF) at a given point x.
        /// </summary>
        public double Value(double x) => _distribution.CumulativeDistribution(x);

        /// <summary>
        /// Calculates the derivative of the cumulative distribution function at x, which is the PDF.
        /// </summary>
        public double Derivative(double x) => _gaussian.Value(x);
    }


    /// <summary>
    /// Inverse cumulative normal distribution function (quantile function).
    /// </summary>
    /// <remarks>
    /// This implementation uses the highly accurate inverse CDF from the MathNet.Numerics library,
    /// replacing the original Acklam's approximation from the C++ code.
    /// The C++ alias 'InvCumulativeNormalDistribution' is also supported via documentation.
    /// </remarks>
    public class InverseCumulativeNormal
    {
        private readonly Normal _distribution;

        /// <summary>
        /// Initializes a new instance of the InverseCumulativeNormal class.
        /// </summary>
        /// <param name="average">The mean (mu) of the distribution.</param>
        /// <param name="sigma">The standard deviation (sigma) of the distribution.</param>
        public InverseCumulativeNormal(double average = 0.0, double sigma = 1.0)
        {
            if (sigma <= 0.0)
                throw new ArgumentException($"Sigma must be greater than 0.0 ({sigma} not allowed).");
            
            _distribution = new Normal(average, sigma);
        }

        /// <summary>
        /// Calculates the inverse cumulative distribution function (quantile) for a given probability p.
        /// </summary>
        /// <param name="p">The probability, must be in the range (0, 1).</param>
        public double Value(double p)
        {
            if (p <= 0.0 || p >= 1.0)
            {
                // MathNet's InvCDF handles edge cases, but we can add QL's specific error messages.
                if (System.Math.Abs(p - 1.0) < 1e-15) return double.PositiveInfinity;
                if (System.Math.Abs(p) < 1e-15) return double.NegativeInfinity;
                throw new ArgumentOutOfRangeException(nameof(p), $"InverseCumulativeNormal({p}) undefined: must be 0 < p < 1");
            }
            return _distribution.InverseCumulativeDistribution(p);
        }
    }

    /// <summary>
    /// Moro Inverse cumulative normal distribution class.
    /// </summary>
    /// <remarks>
    /// The original C++ implementation uses an approximation from Boris Moro (1995).
    /// This C# port delegates to the more accurate and robust implementation from MathNet.Numerics
    /// while preserving the original API.
    /// </remarks>
    public class MoroInverseCumulativeNormal : InverseCumulativeNormal
    {
        public MoroInverseCumulativeNormal(double average = 0.0, double sigma = 1.0) : base(average, sigma) { }
    }

    /// <summary>
    /// Maddock's Inverse cumulative normal distribution class.
    /// </summary>
    /// <remarks>
    /// The original C++ implementation uses the Boost library's implementation.
    /// This C# port delegates to the high-quality implementation from MathNet.Numerics
    /// while preserving the original API.
    /// </remarks>
    public class MaddockInverseCumulativeNormal : InverseCumulativeNormal
    {
        public MaddockInverseCumulativeNormal(double average = 0.0, double sigma = 1.0) : base(average, sigma) { }
    }

    /// <summary>
    /// Maddock's cumulative normal distribution class.
    /// </summary>
    /// <remarks>
    /// The original C++ implementation uses the Boost library's implementation.
    /// This C# port delegates to the high-quality implementation from MathNet.Numerics
    /// while preserving the original API.
    /// </remarks>
    public class MaddockCumulativeNormal : CumulativeNormalDistribution
    {
        public MaddockCumulativeNormal(double average = 0.0, double sigma = 1.0) : base(average, sigma) { }
    }
}