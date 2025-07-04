// C# code for Functional.cs

namespace Antares.Math
{
    /// <summary>
    /// Provides simple mathematical functions and combinators.
    /// This corresponds to ql/math/functional.hpp.
    /// </summary>
    public static class Functional
    {
        /// <summary>
        /// Calculates the square of a number.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The square of the input value (x*x).</returns>
        public static double Squared(double x)
        {
            return x * x;
        }

        /// <summary>
        /// Calculates the square of a number.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The square of the input value (x*x).</returns>
        public static int Squared(int x)
        {
            return x * x;
        }

        /// <summary>
        /// Calculates the square of a number.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The square of the input value (x*x).</returns>
        public static long Squared(long x)
        {
            return x * x;
        }

        /// <summary>
        /// Calculates the square of a number.
        /// </summary>
        /// <param name="x">The input value.</param>
        /// <returns>The square of the input value (x*x).</returns>
        public static decimal Squared(decimal x)
        {
            return x * x;
        }
    }
}