// C# code for Comparison.cs

using System;

namespace Antares.Math
{
    /// <summary>
    /// Provides methods for floating-point comparisons.
    /// </summary>
    public static class Comparison
    {
        private const int DefaultN = 42;

        /// <summary>
        /// Strict floating-point comparison.
        /// </summary>
        /// <remarks>
        /// Follows the advice of Knuth on checking for floating-point equality.
        /// The closeness relationship is:
        /// <c>|x-y| &lt;= tolerance * |x| AND |x-y| &lt;= tolerance * |y|</c>
        /// where tolerance is <c>n</c> times the machine epsilon.
        /// </remarks>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="n">The number of machine-epsilon units for the tolerance.</param>
        /// <returns>True if the values are close, false otherwise.</returns>
        public static bool Close(double x, double y, int n = DefaultN)
        {
            // Handles +infinity and -infinity representations, etc.
            if (x == y)
                return true;

            double diff = System.Math.Abs(x - y);
            double tolerance = n * QLDefines.EPSILON;

            // Special case for x or y being zero to avoid division by zero
            // and handle cases where one is very small.
            if (x == 0.0 || y == 0.0)
                return diff < (tolerance * tolerance);

            return diff <= tolerance * System.Math.Abs(x) &&
                   diff <= tolerance * System.Math.Abs(y);
        }

        /// <summary>
        /// Loose floating-point comparison.
        /// </summary>
        /// <remarks>
        /// Follows the advice of Knuth on checking for floating-point equality.
        /// The closeness relationship is:
        /// <c>|x-y| &lt;= tolerance * |x| OR |x-y| &lt;= tolerance * |y|</c>
        /// where tolerance is <c>n</c> times the machine epsilon.
        /// </remarks>
        /// <param name="x">The first value.</param>
        /// <param name="y">The second value.</param>
        /// <param name="n">The number of machine-epsilon units for the tolerance.</param>
        /// <returns>True if the values are close enough, false otherwise.</returns>
        public static bool CloseEnough(double x, double y, int n = DefaultN)
        {
            // Deals with +infinity and -infinity representations etc.
            if (x == y)
                return true;

            double diff = System.Math.Abs(x - y);
            double tolerance = n * QLDefines.EPSILON;

            // Special case for x or y being zero.
            if (x == 0.0 || y == 0.0)
                return diff < (tolerance * tolerance);

            return diff <= tolerance * System.Math.Abs(x) ||
                   diff <= tolerance * System.Math.Abs(y);
        }
    }
}