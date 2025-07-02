// C# code for Interpolation.cs

using System;
using System.Collections.Generic;

namespace Antares.Math
{
    #region Supporting Infrastructure (Normally in separate files)

    /// <summary>
    /// Defines behavior for classes that can perform extrapolation.
    /// This is a placeholder and should be defined in its own file.
    /// </summary>
    public interface IExtrapolator
    {
        bool AllowsExtrapolation { get; }
        void EnableExtrapolation(bool value);
    }

    /// <summary>
    /// This is a minimal stand-in for ql/math/comparison.hpp
    /// It will be replaced by a full port of the corresponding file later.
    /// </summary>
    internal static class Comparison
    {
        private const double Epsilon = 1.0e-15;

        public static bool Close(double x, double y, int n = 42)
        {
            double diff = System.Math.Abs(x - y);
            double tolerance = n * Epsilon;
            if (y != 0.0)
                return diff <= System.Math.Abs(y) * tolerance;
            return diff <= tolerance;
        }

        public static bool CloseEnough(double x, double y, int n = 42)
        {
            double diff = System.Math.Abs(x - y);
            double tolerance = n * Epsilon * System.Math.Max(1.0, System.Math.Max(System.Math.Abs(x), System.Math.Abs(y)));
            return diff <= tolerance;
        }
    }

    #endregion

    /// <summary>
    /// Base class for 1-D interpolations.
    /// </summary>
    /// <remarks>
    /// Classes derived from this class will provide interpolated
    /// values from two sequences of equal length, representing
    /// discretized values of a variable and a function of the former,
    /// respectively.
    /// <para>
    /// Interpolations don't copy their underlying data;
    /// instead, they store references to them. This allows them to see
    /// changes in the underlying data without having to propagate them
    /// manually, but adds the requirement that the lifetime
    /// of the underlying data exceeds or equals the lifetime
    /// of the interpolation. It is up to the user to ensure
    /// this.
    /// </para>
    /// </remarks>
    public abstract class Interpolation : IExtrapolator
    {
        protected IImpl _impl;

        #region IExtrapolator implementation
        private bool _allowExtrapolation;
        public bool AllowsExtrapolation => _allowExtrapolation;
        public void EnableExtrapolation(bool value) => _allowExtrapolation = value;
        #endregion

        protected Interpolation() { }

        #region Public Interface
        public bool IsEmpty => _impl == null;

        public double Value(double x, bool allowExtrapolation = false)
        {
            CheckRange(x, allowExtrapolation);
            return _impl.Value(x);
        }

        public double Primitive(double x, bool allowExtrapolation = false)
        {
            CheckRange(x, allowExtrapolation);
            return _impl.Primitive(x);
        }

        public double Derivative(double x, bool allowExtrapolation = false)
        {
            CheckRange(x, allowExtrapolation);
            return _impl.Derivative(x);
        }

        public double SecondDerivative(double x, bool allowExtrapolation = false)
        {
            CheckRange(x, allowExtrapolation);
            return _impl.SecondDerivative(x);
        }

        public double XMin => _impl.XMin;
        public double XMax => _impl.XMax;
        public bool IsInRange(double x) => _impl.IsInRange(x);
        public void Update() => _impl.Update();
        #endregion

        protected void CheckRange(double x, bool extrapolate)
        {
            if (extrapolate || AllowsExtrapolation || _impl.IsInRange(x))
                return;

            throw new ArgumentOutOfRangeException(nameof(x),
                $"interpolation range is [{_impl.XMin}, {_impl.XMax}]: extrapolation at {x} not allowed");
        }

        /// <summary>
        /// Abstract base class for interpolation implementations
        /// </summary>
        protected interface IImpl
        {
            void Update();
            double XMin { get; }
            double XMax { get; }
            IReadOnlyList<double> XValues { get; }
            IReadOnlyList<double> YValues { get; }
            bool IsInRange(double x);
            double Value(double x);
            double Primitive(double x);
            double Derivative(double x);
            double SecondDerivative(double x);
        }

        /// <summary>
        /// Basic abstract implementation for interpolations.
        /// </summary>
        protected abstract class TemplateImpl : IImpl
        {
            protected readonly double[] _x;
            protected readonly double[] _y;

            protected TemplateImpl(double[] x, double[] y, int requiredPoints = 2)
            {
                if (x.Length < requiredPoints)
                    throw new ArgumentException($"Not enough points to interpolate: at least {requiredPoints} required, {x.Length} provided.");
                if (x.Length != y.Length)
                    throw new ArgumentException($"X and Y arrays must be of the same size, but are {x.Length} and {y.Length}.");

                _x = x;
                _y = y;
            }

            public double XMin => _x[0];
            public double XMax => _x[_x.Length - 1];
            public IReadOnlyList<double> XValues => _x;
            public IReadOnlyList<double> YValues => _y;

            public bool IsInRange(double x)
            {
                #if DEBUG
                for (int i = 0; i < _x.Length - 1; ++i)
                {
                    if (_x[i+1] <= _x[i])
                        throw new InvalidOperationException("Unsorted x values provided to interpolation.");
                }
                #endif

                double x1 = XMin, x2 = XMax;
                return (x >= x1 && x <= x2) || Comparison.Close(x, x1) || Comparison.Close(x, x2);
            }

            protected int Locate(double x)
            {
                if (x < _x[0])
                    return 0;
                if (x >= _x[_x.Length - 1])
                    return _x.Length - 2;

                int result = Array.BinarySearch(_x, x);
                if (result >= 0)
                {
                    // Exact match. If it's the last point, it should belong to the last interval.
                    return System.Math.Min(result, _x.Length - 2);
                }
                else
                {
                    // Not found. Bitwise complement of result is the index of the first element larger than x.
                    int insertionPoint = ~result;
                    return insertionPoint - 1;
                }
            }

            // Abstract members to be implemented by concrete interpolation types
            public abstract void Update();
            public abstract double Value(double x);
            public abstract double Primitive(double x);
            public abstract double Derivative(double x);
            public abstract double SecondDerivative(double x);
        }
    }
}