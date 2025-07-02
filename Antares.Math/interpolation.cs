// C# code for Interpolation.cs

using System;
using System.Collections.Generic;

namespace Antares.Math
{
    /// <summary>
    /// Defines behavior for classes that can perform extrapolation.
    /// </summary>
    public interface IExtrapolator
    {
        bool AllowsExtrapolation { get; }
        void EnableExtrapolation(bool value);
    }

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
    public class Interpolation : IExtrapolator
    {
        protected TemplateImpl impl_;

        protected bool allowsExtrapolation_;

        /// <summary>
        /// Basic template implementation.
        /// </summary>
        public abstract class TemplateImpl
        {
            protected readonly double[] _x, _y;
            protected readonly int _requiredPoints;

            protected TemplateImpl(double[] x, double[] y, int requiredPoints = 2)
            {
                _requiredPoints = requiredPoints;

                QL.Require(x?.Length > 0, "empty x vector");
                QL.Require(y?.Length > 0, "empty y vector");
                QL.Require(x.Length == y.Length, $"size mismatch: x size is {x.Length}, y size is {y.Length}");
                QL.Require(x.Length >= requiredPoints, $"not enough points to interpolate: at least {requiredPoints} required, {x.Length} provided");

                _x = x;
                _y = y;

                CheckInputsAreSorted();
            }

            public virtual double XMin => _x[0];
            public virtual double XMax => _x[_x.Length - 1];
            public virtual bool IsInRange(double x) => !(x < XMin || x > XMax);

            protected virtual void CheckInputsAreSorted()
            {
                for (int i = 1; i < _x.Length; ++i)
                    QL.Require(_x[i - 1] < _x[i], $"unsorted x values: x[{i - 1}] = {_x[i - 1]}, x[{i}] = {_x[i]}");
            }

            protected int Locate(double x)
            {
                if (x < _x[0])
                    return 0;
                else if (x > _x[_x.Length - 1])
                    return _x.Length - 2;
                else
                {
                    int result = System.Array.BinarySearch(_x, x);
                    if (result < 0)
                        result = ~result - 1; // Insertion point minus 1
                    
                    // Ensure we don't go beyond bounds for interpolation
                    return System.Math.Min(result, _x.Length - 2);
                }
            }

            public abstract void Update();
            public abstract double Value(double x);
            public abstract double Primitive(double x);
            public abstract double Derivative(double x);
            public abstract double SecondDerivative(double x);
        }

        public Interpolation()
        {
            allowsExtrapolation_ = false;
        }

        public Interpolation(TemplateImpl impl, bool allowsExtrapolation = false)
        {
            impl_ = impl;
            allowsExtrapolation_ = allowsExtrapolation;
        }

        public double XMin => impl_.XMin;
        public double XMax => impl_.XMax;
        public bool Empty => impl_ == null;

        public double Value(double x, bool allowExtrapolation = false)
        {
            CheckRange(x, allowExtrapolation);
            return impl_.Value(x);
        }

        public double Primitive(double x, bool allowExtrapolation = false)
        {
            CheckRange(x, allowExtrapolation);
            return impl_.Primitive(x);
        }

        public double Derivative(double x, bool allowExtrapolation = false)
        {
            CheckRange(x, allowExtrapolation);
            return impl_.Derivative(x);
        }

        public double SecondDerivative(double x, bool allowExtrapolation = false)
        {
            CheckRange(x, allowExtrapolation);
            return impl_.SecondDerivative(x);
        }

        public double this[double x] => Value(x, allowsExtrapolation_);

        public void Update()
        {
            impl_?.Update();
        }

        // IExtrapolator interface
        public bool AllowsExtrapolation => allowsExtrapolation_;

        public void EnableExtrapolation(bool b = true)
        {
            allowsExtrapolation_ = b;
        }

        public void DisallowExtrapolation(bool b = true)
        {
            allowsExtrapolation_ = !b;
        }

        protected void CheckRange(double x, bool extrapolate)
        {
            QL.Require(extrapolate || allowsExtrapolation_ || impl_.IsInRange(x),
                $"interpolation range is [{XMin}, {XMax}]: extrapolation at {x} not allowed");
        }
    }

    /// <summary>
    /// Interface for interpolations that support updating Y values without rebuilding.
    /// </summary>
    public interface IUpdatedYInterpolation
    {
        double Value(Array y, double x);
    }
}