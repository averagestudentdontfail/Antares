// C# code for ErrorFunction.cs

using MathNet.Numerics;

namespace Antares.Math
{
    /// <summary>
    /// Error function.
    /// Used to calculate the cumulative normal distribution function.
    /// </summary>
    /// <remarks>
    /// This class provides the error function, erf(x). The original C++ implementation
    /// from QuantLib contains a complex, custom numerical approximation routine.
    /// This C# port delegates the calculation to the highly optimized and tested
    /// `Erf` method from the `MathNet.Numerics` library, which is an approved dependency.
    /// </remarks>
    public class ErrorFunction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorFunction"/> class.
        /// </summary>
        public ErrorFunction() { }

        /// <summary>
        /// Calculates the error function of a given value.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The value of erf(x).</returns>
        public double Value(double x)
        {
            return SpecialFunctions.Erf(x);
        }

        /// <summary>
        /// A convenient alias for the Value method to allow for a more functional-style invocation.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The value of erf(x).</returns>
        public double Invoke(double x)
        {
            return Value(x);
        }
    }
}