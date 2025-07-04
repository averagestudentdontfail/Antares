// Dataformatters.cs

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Antares.Utility
{
    /// <summary>
    /// Provides static helper methods for creating formatted output strings.
    /// This class replaces the C++ stream manipulator pattern with idiomatic
    /// C# static formatting methods.
    /// </summary>
    public static class DataFormatters
    {
        /// <summary>
        /// Formats a number with an ordinal suffix (e.g., 1st, 2nd, 3rd, 4th).
        /// Corresponds to the C++ `io::ordinal` manipulator.
        /// </summary>
        /// <param name="n">The number to format.</param>
        /// <returns>The formatted string.</returns>
        public static string ToOrdinal(int n)
        {
            if (n < 0) return n.ToString(); // Or throw? QL doesn't seem to handle this.

            // Suffixes for 11th, 12th, 13th are exceptions.
            if ((n % 100) >= 11 && (n % 100) <= 13)
            {
                return $"{n}th";
            }

            return (n % 10) switch
            {
                1 => $"{n}st",
                2 => $"{n}nd",
                3 => $"{n}rd",
                _ => $"{n}th",
            };
        }

        /// <summary>
        /// Formats a real number as a percentage string.
        /// Corresponds to the C++ `io::percent`, `io::rate`, and `io::volatility` manipulators.
        /// </summary>
        /// <param name="value">The value to format. If null, "null" is returned.</param>
        /// <param name="precision">The number of decimal places to show. Defaults to 4.</param>
        /// <returns>The formatted percentage string (e.g., "12.3456 %").</returns>
        public static string ToPercent(double? value, int precision = 4)
        {
            if (!value.HasValue)
                return "null";

            // We manually format to match the C++ output "value %" instead of using
            // the standard C# "P" format specifier which includes the culture-specific symbol.
            string format = $"F{precision}";
            return $"{(value.Value * 100.0).ToString(format, CultureInfo.InvariantCulture)} %";
        }

        /// <summary>
        /// Formats an integer as a power of two (e.g., 12 becomes "3*2^2").
        /// Corresponds to the C++ `io::power_of_two` manipulator.
        /// </summary>
        /// <param name="value">The integer value to format. If null, "null" is returned.</param>
        /// <returns>The formatted string.</returns>
        public static string ToPowerOfTwo(long? value)
        {
            if (!value.HasValue)
                return "null";

            long n = value.Value;
            if (n == 0)
                return "0*2^0";

            int power = 0;
            while ((n & 1L) == 0)
            {
                power++;
                n >>= 1;
            }

            return $"{n}*2^{power}";
        }

        /// <summary>
        /// Formats a sequence of items into a space-separated string enclosed in parentheses.
        /// Corresponds to the C++ `io::sequence` manipulator.
        /// </summary>
        /// <typeparam name="T">The type of items in the sequence.</typeparam>
        /// <param name="sequence">The sequence of items to format.</param>
        /// <returns>The formatted string, e.g., "( item1 item2 item3 )".</returns>
        public static string ToSequenceString<T>(IEnumerable<T> sequence)
        {
            if (sequence == null)
                return "( )";

            return $"( {string.Join(" ", sequence)} )";
        }
    }
}