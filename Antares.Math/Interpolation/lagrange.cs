// LagrangeInterpolation.cs

using System;
using System.Linq;
using Antares.Math; 

namespace Antares.Math.Interpolation
{
    /// <summary>
    /// Defines an interface for interpolations that can be re-evaluated
    /// with a new set of y-values without requiring a full update.
    /// This is specific to algorithms like Lagrange interpolation.
    /// </summary>
    public interface IUpdatedYInterpolation
    {
        double Value(Array yValues, double x);
    }

    /// <summary>
    /// Barycentric Lagrange interpolation.
    /// </summary>
    /// <remarks>
    /// References:
    /// J-P. Berrut and L.N. Trefethen, Barycentric Lagrange interpolation,
    /// SIAM Review, 46(3):501–517, 2004.
    /// https://people.maths.ox.ac.uk/trefethen/barycentric.pdf
    ///
    /// See the base Interpolation class for information about the
    /// required lifetime of the underlying data.
    /// </remarks>
    public class LagrangeInterpolation : Interpolation, IUpdatedYInterpolation
    {
        /// <summary>
        /// Creates a Lagrange interpolation from the given coordinates.
        /// </summary>
        /// <param name="x">The x-coordinates, which must be unique.</param>
        /// <param name="y">The y-coordinates.</param>
        public LagrangeInterpolation(double[] x, double[] y)
        {
            _impl = new Detail.LagrangeInterpolationImpl(x, y);
            _impl.Update();
        }

        /// <summary>
        /// Interpolates with a new set of y-values for a given x-value.
        /// </summary>
        /// <param name="y">The new y-values.</param>
        /// <param name="x">The x-value at which to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        public double Value(Array y, double x)
        {
            // The implementation is directly castable to the required interface.
            return ((IUpdatedYInterpolation)_impl).Value(y, x);
        }
    }

    namespace Detail
    {
        internal class LagrangeInterpolationImpl : Interpolation.TemplateImpl, IUpdatedYInterpolation
        {
            private readonly int _n;
            private readonly Array _lambda;

            public LagrangeInterpolationImpl(double[] x, double[] y)
                : base(x, y)
            {
                _n = x.Length;
                _lambda = new Array(_n);

                #if DEBUG
                // Safety check for duplicate x values, mimicking QL_EXTRA_SAFETY_CHECKS
                if (x.Distinct().Count() != _n)
                    throw new ArgumentException("x values must not contain duplicates.", nameof(x));
                #endif
            }

            public override void Update()
            {
                // This scaling factor 'cM1' is not strictly part of the standard barycentric formula
                // but appears in the QL implementation, likely for numerical stability.
                double cM1 = 4.0 / (_x[_n - 1] - _x[0]);

                for (int i = 0; i < _n; ++i)
                {
                    _lambda[i] = 1.0;
                    double xi = _x[i];
                    for (int j = 0; j < _n; ++j)
                    {
                        if (i != j)
                            _lambda[i] *= cM1 * (xi - _x[j]);
                    }
                    _lambda[i] = 1.0 / _lambda[i];
                }
            }

            public override double Value(double x) => Value(new Array(_y), x);

            public override double Derivative(double x)
            {
                double n = 0.0, d = 0.0, nd = 0.0, dd = 0.0;
                for (int i = 0; i < _n; ++i)
                {
                    double xi = _x[i];
                    if (Comparison.CloseEnough(x, xi))
                    {
                        double p = 0.0;
                        for (int j = 0; j < _n; ++j)
                        {
                            if (i != j)
                            {
                                p += _lambda[j] / (x - _x[j]) * (_y[j] - _y[i]);
                            }
                        }
                        return p / _lambda[i];
                    }

                    double alpha = _lambda[i] / (x - xi);
                    double alphad = -alpha / (x - xi);
                    n += alpha * _y[i];
                    d += alpha;
                    nd += alphad * _y[i];
                    dd += alphad;
                }
                return (nd * d - n * dd) / (d * d);
            }

            public override double Primitive(double x)
            {
                throw new NotImplementedException("LagrangeInterpolation primitive is not implemented.");
            }

            public override double SecondDerivative(double x)
            {
                throw new NotImplementedException("LagrangeInterpolation secondDerivative is not implemented.");
            }

            // Implementation of IUpdatedYInterpolation
            public double Value(Array y, double x)
            {
                double eps = 10 * QLDefines.EPSILON * System.Math.Abs(x);

                // Check if x is very close to one of the grid points.
                // Using Array.BinarySearch for an efficient search.
                int idx = Array.BinarySearch(_x, x - eps);
                if (idx < 0) idx = ~idx; // If not found, get insertion point.
                
                if (idx < _n && _x[idx] - x < eps)
                {
                    return y[idx];
                }

                // Barycentric formula
                double n = 0.0, d = 0.0;
                for (int i = 0; i < _n; ++i)
                {
                    double alpha = _lambda[i] / (x - _x[i]);
                    n += alpha * y[i];
                    d += alpha;
                }
                return n / d;
            }
        }
    }
}