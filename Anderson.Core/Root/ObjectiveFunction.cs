using System;

namespace Anderson.Root
{
    /// <summary>
    /// Represents a single-variable function for which a root is sought.
    /// This is the base interface for all objective functions.
    /// </summary>
    public interface IObjectiveFunction
    {
        /// <summary>
        /// Calculates the value of the function at a given point.
        /// </summary>
        /// <param name="x">The point at which to evaluate the function.</param>
        /// <returns>The value of f(x).</returns>
        double Value(double x);
    }

    /// <summary>
    /// Represents an objective function that also provides its first derivative.
    /// Solvers like Newton's method will require this interface.
    /// </summary>
    public interface IObjectiveFunctionWithDerivative : IObjectiveFunction
    {
        /// <summary>
        /// Calculates the first derivative of the function at a given point.
        /// </summary>
        /// <param name="x">The point at which to evaluate the derivative.</param>
        /// <returns>The value of f'(x).</returns>
        double Derivative(double x);
    }

    /// <summary>
    /// Represents an objective function that also provides its second derivative.
    /// Solvers like Halley's method will require this interface.
    /// </summary>
    public interface IObjectiveFunctionWithSecondDerivative : IObjectiveFunctionWithDerivative
    {
        /// <summary>
        /// Calculates the second derivative of the function at a given point.
        /// </summary>
        /// <param name="x">The point at which to evaluate the second derivative.</param>
        /// <returns>The value of f''(x).</returns>
        double SecondDerivative(double x);
    }
}