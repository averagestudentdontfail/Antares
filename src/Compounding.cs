// Compounding.cs

using System;
using System.ComponentModel;

namespace Antares
{
    /// <summary>
    /// Interest rate compounding rule.
    /// </summary>
    public enum Compounding
    {
        /// <summary>
        /// 1 + rt
        /// </summary>
        Simple = 0,
        /// <summary>
        /// (1+r)^t
        /// </summary>
        Compounded = 1,
        /// <summary>
        /// e^(rt)
        /// </summary>
        Continuous = 2,
        /// <summary>
        /// Simple up to the first period then Compounded.
        /// </summary>
        SimpleThenCompounded,
        /// <summary>
        /// Compounded up to the first period then Simple.
        /// </summary>
        CompoundedThenSimple
    }

    /// <summary>
    /// Provides extension methods for the Compounding enum.
    /// </summary>
    public static class CompoundingExtensions
    {
        /// <summary>
        /// Returns a string representation of the compounding rule.
        /// This method mimics the C++ operator&lt;&lt; and provides error checking for invalid enum values.
        /// </summary>
        /// <param name="c">The Compounding enum value.</param>
        /// <returns>A string representation of the enum member name.</returns>
        public static string ToEnumString(this Compounding c)
        {
            // The default .ToString() on an enum returns its member name, which matches the C++ implementation.
            // We add a check to ensure the enum value is valid, mimicking the C++ switch's default case.
            if (Enum.IsDefined(typeof(Compounding), c))
            {
                return c.ToString();
            }

            // This line assumes the existence of the QL helper class from the 'errors.cs' port.
            // It faithfully replicates the behavior of QL_FAIL.
            QL.Fail($"Unknown compounding type: {(int)c}");
            return string.Empty; // This will never be reached due to QL.Fail throwing an exception
        }
    }
}