// C# code for Integral.cs

using System;

namespace Antares.Math.Integral
{
    /// <summary>
    /// Delegate for integrand functions.
    /// </summary>
    /// <param name="x">The variable of integration.</param>
    /// <returns>The value of the function at x.</returns>
    public delegate double Integrand(double x);

    /// <summary>
    /// Abstract base class for numerical integration methods.
    /// </summary>
    /// <remarks>
    /// This class defines the interface for numerical integration algorithms.
    /// Derived classes implement specific integration methods such as
    /// Simpson's rule, trapezoidal rule, Gaussian quadrature, etc.
    /// </remarks>
    public abstract class Integrator
    {
        protected double _absoluteAccuracy;
        protected double _relativeAccuracy;
        protected int _maxEvaluations;
        protected int _evaluationNumber;
        protected bool _absoluteError;
        protected bool _increaseNumberOfEvaluations;

        /// <summary>
        /// Initializes a new instance of the Integrator class.
        /// </summary>
        /// <param name="absoluteAccuracy">Required absolute accuracy.</param>
        /// <param name="maxEvaluations">Maximum number of function evaluations.</param>
        protected Integrator(double absoluteAccuracy, int maxEvaluations)
        {
            _absoluteAccuracy = absoluteAccuracy;
            _maxEvaluations = maxEvaluations;
            _relativeAccuracy = double.NaN;
            _evaluationNumber = 0;
            _absoluteError = true;
            _increaseNumberOfEvaluations = false;
        }

        #region Properties
        /// <summary>
        /// Gets or sets the absolute accuracy for the integration.
        /// </summary>
        public double AbsoluteAccuracy
        {
            get => _absoluteAccuracy;
            set => _absoluteAccuracy = value;
        }

        /// <summary>
        /// Gets or sets the relative accuracy for the integration.
        /// Setting this value enables relative error checking.
        /// </summary>
        public double RelativeAccuracy
        {
            get => _relativeAccuracy;
            set
            {
                _relativeAccuracy = value;
                _absoluteError = false;
            }
        }

        /// <summary>
        /// Gets the absolute error from the last integration.
        /// Only valid if absolute error checking is enabled.
        /// </summary>
        public virtual double AbsoluteError
        {
            get
            {
                QL.Require(_absoluteError, "Relative accuracy is being used");
                return _absoluteAccuracy;
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of function evaluations.
        /// </summary>
        public int MaxEvaluations
        {
            get => _maxEvaluations;
            set => _maxEvaluations = value;
        }

        /// <summary>
        /// Gets the number of function evaluations used in the last integration.
        /// </summary>
        public int NumberOfEvaluations => _evaluationNumber;

        /// <summary>
        /// Gets or sets whether to increase the number of evaluations when accuracy is not met.
        /// </summary>
        public bool IncreaseNumberOfEvaluations
        {
            get => _increaseNumberOfEvaluations;
            set => _increaseNumberOfEvaluations = value;
        }

        /// <summary>
        /// Gets whether the integrator uses absolute error checking.
        /// </summary>
        public bool IsAbsoluteError => _absoluteError;
        #endregion

        #region Integration Interface
        /// <summary>
        /// Integrates the function f from a to b.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        /// <returns>The approximate value of the integral.</returns>
        public double Integrate(Integrand f, double a, double b)
        {
            _evaluationNumber = 0;
            if (a == b)
                return 0.0;
            if (b > a)
                return Integrate(f, a, b);
            else
                return -Integrate(f, b, a);
        }

        /// <summary>
        /// Abstract method that derived classes must implement to perform the actual integration.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        /// <returns>The approximate value of the integral.</returns>
        protected abstract double Integrate(Integrand f, double a, double b);
        #endregion

        #region Utility Methods
        /// <summary>
        /// Sets the absolute accuracy and enables absolute error checking.
        /// </summary>
        /// <param name="accuracy">The absolute accuracy.</param>
        public void SetAbsoluteAccuracy(double accuracy)
        {
            _absoluteAccuracy = accuracy;
            _absoluteError = true;
        }

        /// <summary>
        /// Sets the relative accuracy and enables relative error checking.
        /// </summary>
        /// <param name="accuracy">The relative accuracy.</param>
        public void SetRelativeAccuracy(double accuracy)
        {
            _relativeAccuracy = accuracy;
            _absoluteError = false;
        }
        #endregion
    }

    /// <summary>
    /// Base class for integration methods that can be constructed with default parameters.
    /// </summary>
    public abstract class Integral
    {
        protected readonly double _absoluteAccuracy;
        protected readonly int _maxEvaluations;

        /// <summary>
        /// Initializes a new instance of the Integral class.
        /// </summary>
        /// <param name="absoluteAccuracy">Required absolute accuracy.</param>
        /// <param name="maxEvaluations">Maximum number of function evaluations.</param>
        protected Integral(double absoluteAccuracy, int maxEvaluations)
        {
            _absoluteAccuracy = absoluteAccuracy;
            _maxEvaluations = maxEvaluations;
        }

        /// <summary>
        /// Creates an integrator instance for this integration method.
        /// </summary>
        /// <returns>A new integrator instance.</returns>
        public abstract Integrator GetIntegrator();

        /// <summary>
        /// Convenience method to integrate a function using this integration method.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        /// <returns>The approximate value of the integral.</returns>
        public double Integrate(Integrand f, double a, double b)
        {
            var integrator = GetIntegrator();
            return integrator.Integrate(f, a, b);
        }
    }

    /// <summary>
    /// Integration methods enumeration.
    /// This can be used to select different integration algorithms.
    /// </summary>
    public enum IntegrationMethod
    {
        /// <summary>
        /// Simpson's rule.
        /// </summary>
        Simpson,
        /// <summary>
        /// Trapezoidal rule.
        /// </summary>
        Trapezoid,
        /// <summary>
        /// Gauss-Lobatto quadrature.
        /// </summary>
        GaussLobatto,
        /// <summary>
        /// Tanh-sinh quadrature.
        /// </summary>
        TanhSinh
    }

    /// <summary>
    /// Utility class for common integration operations.
    /// </summary>
    public static class Integration
    {
        /// <summary>
        /// Default absolute accuracy for integration.
        /// </summary>
        public const double DefaultAbsoluteAccuracy = 1.0e-6;

        /// <summary>
        /// Default maximum number of evaluations.
        /// </summary>
        public const int DefaultMaxEvaluations = 10000;

        /// <summary>
        /// Checks if two values are close enough based on the given tolerance.
        /// </summary>
        /// <param name="x">First value.</param>
        /// <param name="y">Second value.</param>
        /// <param name="tolerance">Tolerance for comparison.</param>
        /// <returns>True if the values are close enough, false otherwise.</returns>
        public static bool IsClose(double x, double y, double tolerance)
        {
            return System.Math.Abs(x - y) <= tolerance;
        }

        /// <summary>
        /// Checks if a value is close to zero based on the given tolerance.
        /// </summary>
        /// <param name="x">The value to check.</param>
        /// <param name="tolerance">Tolerance for comparison.</param>
        /// <returns>True if the value is close to zero, false otherwise.</returns>
        public static bool IsCloseToZero(double x, double tolerance)
        {
            return System.Math.Abs(x) <= tolerance;
        }
    }
}