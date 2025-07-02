// C# code for IFunctionWithDerivative.cs

namespace Antares.Math.Solver
{
    /// <summary>
    /// Defines a function that provides both its value and its first derivative.
    /// This interface is required by solvers that use derivative information, such as Newton's method.
    /// </summary>
    public interface IFunctionWithDerivative
    {
        /// <summary>
        /// Calculates the function's value at a given point.
        /// </summary>
        /// <param name="x">The point at which to evaluate the function.</param>
        /// <returns>The value of the function.</returns>
        double Value(double x);

        /// <summary>
        /// Calculates the function's first derivative at a given point.
        /// </summary>
        /// <param name="x">The point at which to evaluate the derivative.</param>
        /// <returns>The value of the first derivative.</returns>
        double Derivative(double x);
    }
}