// Qldefines.cs

namespace Antares
{
    /// <summary>
    /// Provides global definitions and numeric limits, analogous to qldefines.hpp.
    /// </summary>
    /// <remarks>
    /// This class provides constants for numeric limits that are frequently used throughout
    /// financial calculations. These values correspond to the QL_XXX macros in the
    /// original C++ library.
    /// <para>
    /// In C#, many platform-specific and compiler-specific directives from qldefines.hpp
    /// are unnecessary due to the nature of the .NET runtime. This file focuses only
    /// on the translatable concepts.
    /// </para>
    /// </remarks>
    public static class QLDefines
    {
        /// <summary>
        /// Defines the value of the largest representable negative integer value.
        /// Corresponds to QL_MIN_INTEGER.
        /// </summary>
        public const int MIN_INTEGER = int.MinValue;

        /// <summary>
        /// Defines the value of the largest representable integer value.
        /// Corresponds to QL_MAX_INTEGER.
        /// </summary>
        public const int MAX_INTEGER = int.MaxValue;

        /// <summary>
        /// Defines the value of the largest representable negative floating-point value.
        /// Corresponds to QL_MIN_REAL.
        /// </summary>
        public const double MIN_REAL = double.MinValue;

        /// <summary>
        /// Defines the value of the largest representable floating-point value.
        /// Corresponds to QL_MAX_REAL.
        /// </summary>
        public const double MAX_REAL = double.MaxValue;

        /// <summary>
        /// Defines the value of the smallest representable positive normalized value.
        /// Corresponds to QL_MIN_POSITIVE_REAL.
        /// Note: This is not the same as .NET's double.Epsilon, which is the smallest subnormal value.
        /// This value matches C++'s std::numeric_limits&lt;double&gt;::min().
        /// </summary>
        public const double MIN_POSITIVE_REAL = 2.2250738585072014e-308;

        /// <summary>
        /// Defines the machine precision for operations over double values.
        /// Corresponds to QL_EPSILON.
        /// This is the smallest value 'e' such that (1.0 + e) != 1.0.
        /// Note: This is not the same as .NET's double.Epsilon.
        /// This value matches C++'s std::numeric_limits&lt;double&gt;::epsilon().
        /// </summary>
        public const double EPSILON = 2.2204460492503131e-016;
    }
}