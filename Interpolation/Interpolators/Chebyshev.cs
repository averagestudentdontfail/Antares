using System;
using System.Linq;

namespace Anderson.Interpolation.Interpolators
{
    /// <summary>
    /// Performs Chebyshev interpolation on a given set of function values at Chebyshev nodes.
    /// This method is renowned for its stability and near-optimal approximation properties,
    /// making it ideal for representing functions like an option's exercise boundary.
    /// Reference: S.A. Sarra, "Chebyshev Interpolation: An Interactive Tour."
    /// </summary>
    public class ChebyshevInterpolation : Interpolation
    {
        public enum PointsType { FirstKind, SecondKind }

        private readonly double[] _nodes;
        private readonly double[] _coefficients;

        /// <summary>
        /// Initializes a new instance of the ChebyshevInterpolation class from pre-computed function values.
        /// </summary>
        /// <param name="y">The function values at the Chebyshev nodes. The length of this array determines the order of interpolation.</param>
        /// <param name="pointsType">The type of Chebyshev nodes. The default (SecondKind) corresponds to the extrema and is generally preferred.</param>
        public ChebyshevInterpolation(double[] y, PointsType pointsType = PointsType.SecondKind)
            : base(y)
        {
            _nodes = GetNodes(Y.Length, pointsType);
            _coefficients = new double[Y.Length];
            CalculateCoefficients();
        }

        /// <summary>
        /// Initializes a new instance of the ChebyshevInterpolation class by evaluating a function at the nodes.
        /// </summary>
        /// <param name="n">The number of interpolation nodes (which is the polynomial degree + 1).</param>
        /// <param name="f">The function to evaluate at the Chebyshev nodes. The function should map from [-1, 1] to a real value.</param>
        /// <param name="pointsType">The type of Chebyshev nodes.</param>
        public ChebyshevInterpolation(int n, Func<double, double> f, PointsType pointsType = PointsType.SecondKind)
            : this(GetNodes(n, pointsType).Select(f).ToArray(), pointsType)
        {
        }

        /// <summary>
        /// Gets the Chebyshev nodes used for this interpolation, which lie in the range [-1, 1].
        /// </summary>
        public double[] Nodes() => _nodes;

        /// <summary>
        /// Generates the canonical Chebyshev nodes for a given order and type.
        /// </summary>
        /// <param name="n">The number of nodes.</param>
        /// <param name="pointsType">The type of nodes (roots or extrema).</param>
        /// <returns>An array of nodes in the range [-1, 1].</returns>
        public static double[] GetNodes(int n, PointsType pointsType)
        {
            if (n <= 1)
            {
                throw new ArgumentException("Number of nodes for Chebyshev interpolation must be greater than 1.");
            }

            var nodes = new double[n];
            if (pointsType == PointsType.SecondKind)
            {
                // Extrema of T_{n-1}(x), also known as Chebyshev-Lobatto points.
                // These include the endpoints -1 and 1.
                // z_k = -cos(k*pi / (n-1)) for k = 0, ..., n-1
                int n_minus_1 = n - 1;
                for (int k = 0; k < n; k++)
                {
                    nodes[k] = -Math.Cos(k * Math.PI / n_minus_1);
                }
            }
            else // FirstKind
            {
                // Roots of T_n(x). These are interior to [-1, 1].
                // z_k = -cos((2k+1)*pi / (2n)) for k = 0, ..., n-1
                double two_n = 2.0 * n;
                for (int k = 0; k < n; k++)
                {
                    nodes[k] = -Math.Cos((2.0 * k + 1.0) * Math.PI / two_n);
                }
            }
            return nodes;
        }

        /// <summary>
        /// Updates the function values and recalculates the interpolation coefficients.
        /// </summary>
        public override void UpdateY(double[] newY)
        {
            base.UpdateY(newY);
            CalculateCoefficients();
        }

        /// <summary>
        /// Evaluates the interpolation at point x using the stable Clenshaw's algorithm.
        /// </summary>
        /// <param name="x">The point at which to interpolate. Must be in the canonical range [-1, 1].</param>
        /// <returns>The interpolated value.</returns>
        public override double Value(double x)
        {
            if (x < -1.0 - 1e-12 || x > 1.0 + 1e-12) // Allow for tiny floating point inaccuracies
            {
                throw new ArgumentOutOfRangeException(nameof(x), $"Input x ({x}) must be in the range [-1, 1].");
            }
            x = Math.Max(-1.0, Math.Min(1.0, x));

            // Clenshaw's algorithm for evaluating a Chebyshev series sum C(x) = sum_{k=0}^{n-1} a_k T_k(x)
            double b_k_plus_2 = 0.0;
            double b_k_plus_1 = 0.0;
            double two_x = 2.0 * x;

            for (int k = Y.Length - 1; k >= 1; --k)
            {
                double b_k = _coefficients[k] + two_x * b_k_plus_1 - b_k_plus_2;
                b_k_plus_2 = b_k_plus_1;
                b_k_plus_1 = b_k;
            }

            // The final result is a_0 + x*b_1 - b_2
            return _coefficients[0] + x * b_k_plus_1 - b_k_plus_2;
        }

        /// <summary>
        /// Calculates the Chebyshev coefficients from the function values at the nodes
        /// using a Discrete Cosine Transform (DCT-I).
        /// </summary>
        private void CalculateCoefficients()
        {
            int n = Y.Length;
            int n_minus_1 = n - 1;

            for (int k = 0; k < n; k++)
            {
                double sum = 0.0;
                for (int i = 0; i < n; i++)
                {
                    sum += Y[i] * Math.Cos(k * Math.PI * i / n_minus_1);
                }

                // The first and last coefficients have different scaling
                if (k == 0 || k == n_minus_1)
                {
                    _coefficients[k] = sum / n_minus_1;
                }
                else
                {
                    _coefficients[k] = 2.0 * sum / n_minus_1;
                }
            }
        }
    }
}