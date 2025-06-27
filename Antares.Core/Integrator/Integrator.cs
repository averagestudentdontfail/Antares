using System;

namespace Antares.Integrator
{
    /// <summary>
    /// Defines the contract for a 1-dimensional numerical integration algorithm.
    /// </summary>
    public interface IIntegrator
    {
        /// <summary>
        /// Gets the absolute accuracy target for the integration.
        /// </summary>
        double AbsoluteAccuracy { get; }

        /// <summary>
        /// Gets the maximum number of function evaluations allowed.
        /// </summary>
        int MaxEvaluations { get; }

        /// <summary>
        /// Gets the absolute error of the last integration performed.
        /// </summary>
        double AbsoluteError { get; }

        /// <summary>
        /// Gets the number of function evaluations performed in the last integration.
        /// </summary>
        int NumberOfEvaluations { get; }

        /// <summary>
        /// Integrates the given function over the interval [a, b].
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower bound of the integration interval.</param>
        /// <param name="b">The upper bound of the integration interval.</param>
        /// <returns>The result of the integration.</returns>
        double Integrate(Func<double, double> f, double a, double b);
    }

    /// <summary>
    /// Abstract base class for numerical integrators, providing common state management.
    /// </summary>
    public abstract class Integrator : IIntegrator
    {
        public double AbsoluteAccuracy { get; }
        public int MaxEvaluations { get; }

        public double AbsoluteError { get; protected set; }
        public int NumberOfEvaluations { get; protected set; }

        protected Integrator(double absoluteAccuracy, int maxEvaluations)
        {
            AbsoluteAccuracy = absoluteAccuracy;
            MaxEvaluations = maxEvaluations;
        }

        public double Integrate(Func<double, double> f, double a, double b)
        {
            NumberOfEvaluations = 0;
            AbsoluteError = -1.0; // Reset error
            return IntegrateImpl(f, a, b);
        }

        protected abstract double IntegrateImpl(Func<double, double> f, double a, double b);
    }
}