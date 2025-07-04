// C# code for LinearInterpolation.cs

using System;
using System.Collections.Generic;

namespace Antares.Math.Interpolation
{
    /// <summary>
    /// Linear interpolation between discrete points.
    /// </summary>
    /// <remarks>
    /// See the base Interpolation class for information about the
    /// required lifetime of the underlying data.
    /// </remarks>
    public class LinearInterpolation : Interpolation
    {
        /// <summary>
        /// Creates a linear interpolation from the given coordinates.
        /// </summary>
        /// <param name="x">The x-coordinates, which must be sorted.</param>
        /// <param name="y">The y-coordinates.</param>
        public LinearInterpolation(double[] x, double[] y)
        {
            _impl = new LinearInterpolationImpl(x, y);
            _impl.Update();
        }
    }

    /// <summary>
    /// Linear-interpolation factory and traits.
    /// </summary>
    public class Linear
    {
        public const bool Global = false;
        public const int RequiredPoints = 2;

        /// <summary>
        /// Creates a new LinearInterpolation instance.
        /// </summary>
        public Interpolation Interpolate(double[] x, double[] y)
        {
            return new LinearInterpolation(x, y);
        }
    }

    namespace Detail
    {
        /// <summary>
        /// Implementation class for linear interpolation.
        /// </summary>
        internal class LinearInterpolationImpl : Interpolation.TemplateImpl
        {
            private readonly double[] _primitiveConst;
            private readonly double[] _s; // Slopes

            public LinearInterpolationImpl(double[] x, double[] y)
                : base(x, y, Linear.RequiredPoints)
            {
                _primitiveConst = new double[x.Length];
                _s = new double[x.Length - 1];
            }

            public override void Update()
            {
                _primitiveConst[0] = 0.0;
                for (int i = 1; i < _x.Length; ++i)
                {
                    double dx = _x[i] - _x[i - 1];
                    _s[i - 1] = (_y[i] - _y[i - 1]) / dx;
                    _primitiveConst[i] = _primitiveConst[i - 1]
                                       + dx * (_y[i - 1] + 0.5 * dx * _s[i - 1]);
                }
            }

            public override double Value(double x)
            {
                int i = Locate(x);
                return _y[i] + (x - _x[i]) * _s[i];
            }

            public override double Primitive(double x)
            {
                int i = Locate(x);
                double dx = x - _x[i];
                return _primitiveConst[i] + dx * (_y[i] + 0.5 * dx * _s[i]);
            }

            public override double Derivative(double x)
            {
                int i = Locate(x);
                return _s[i];
            }

            public override double SecondDerivative(double x)
            {
                return 0.0;
            }
        }
    }
}