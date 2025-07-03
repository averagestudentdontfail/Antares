// Timeunit.cs

using System;
using System.ComponentModel;

namespace Antares.Time
{
    /// <summary>
    /// Units used to describe time periods.
    /// </summary>
    public enum TimeUnit
    {
        Days,
        Weeks,
        Months,
        Years,
        Hours,
        Minutes,
        Seconds,
        Milliseconds,
        Microseconds
    }

    /// <summary>
    /// Provides extension methods for the TimeUnit enum.
    /// </summary>
    public static class TimeUnitExtensions
    {
        /// <summary>
        /// Returns a string representation of the time unit.
        /// This method mimics the C++ operator&lt;&lt; and provides error checking for invalid enum values.
        /// </summary>
        /// <param name="tu">The TimeUnit enum value.</param>
        /// <returns>A string representation of the enum member name.</returns>
        public static string ToEnumString(this TimeUnit tu)
        {
            return tu switch
            {
                TimeUnit.Days => "Days",
                TimeUnit.Weeks => "Weeks",
                TimeUnit.Months => "Months",
                TimeUnit.Years => "Years",
                TimeUnit.Hours => "Hours",
                TimeUnit.Minutes => "Minutes",
                TimeUnit.Seconds => "Seconds",
                TimeUnit.Milliseconds => "Milliseconds",
                TimeUnit.Microseconds => "Microseconds",
                _ => throw new InvalidEnumArgumentException(nameof(tu), (int)tu, typeof(TimeUnit))
            };
        }
    }
}