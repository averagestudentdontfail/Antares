// C# code for Lagrange.cs

using System;

namespace Antares.Math.Interpolation
{
    /// <summary>
    /// Lagrange interpolation factory and option class.
    /// </summary>
    public class Lagrange
    {
        public const bool Global = true;
        public const int RequiredPoints = 2;

        /// <summary>
        /// Creates a new LagrangeInterpolation instance.
        /// </summary>
        public Interpolation Interpolate(double[] x, double[] y)
        {
            return new LagrangeInterpolation(x, y);
        }
    }

    /// <summary>
    /// Lagrange interpolation implementation.
    /// </summary>
    public class LagrangeInterpolation : Interpolation, IUpdatedYInterpolation
    {
        public LagrangeInterpolation(double[] x, double[] y)
            : base(new LagrangeInterpolationImpl(x, y), true)
        {
        }

        public double Value(Array y, double x) => ((LagrangeInterpolationImpl)impl_).Value(y, x);
    }

    namespace Detail
    {
        /// <summary>
        /// Implementation class for Lagrange interpolation using the barycentric formula.
        /// </summary>
        internal class LagrangeInterpolationImpl : Interpolation.TemplateImpl, IUpdatedYInterpolation
        {
            private readonly double[] _lambda;
            private readonly int _n;

            public LagrangeInterpolationImpl(double[] x, double[] y)
                : base(x, y, Lagrange.RequiredPoints)
            {
                _n = x.Length;
                _lambda = new double[_n];

                #if DEBUG
                // Check that x coordinates are distinct
                for (int i = 0; i < _n - 1; ++i)
                {
                    for (int j = i + 1; j < _n; ++j)
                    {
                        QL.Require(_x[i] != _x[j], "x values must be distinct for Lagrange interpolation", nameof(x));
                    }
                }
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
                int idx = System.Array.BinarySearch(_x, x - eps);
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