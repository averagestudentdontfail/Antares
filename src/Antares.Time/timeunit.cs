// TimeUnit.cs

using System;
using System.ComponentModel;

namespace Antares.Time
{
    /// <summary>
    /// Units used to describe time periods.
    /// </summary>
    public enum TimeUnit
    {
        /// <summary>Time unit is days.</summary>
        Days,
        /// <summary>Time unit is weeks.</summary>
        Weeks,
        /// <summary>Time unit is months.</summary>
        Months,
        /// <summary>Time unit is years.</summary>
        Years
    }

    /// <summary>
    /// Provides extension methods for the TimeUnit enum.
    /// </summary>
    public static class TimeUnitExtensions
    {
        /// <summary>
        /// Returns a string representation of the time unit.
        /// </summary>
        /// <param name="unit">The time unit.</param>
        /// <returns>A string representation of the time unit.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the enum value is not defined.</exception>
        public static string ToFormattedString(this TimeUnit unit)
        {
            return unit switch
            {
                TimeUnit.Days => "Days",
                TimeUnit.Weeks => "Weeks", 
                TimeUnit.Months => "Months",
                TimeUnit.Years => "Years",
                _ => throw new InvalidEnumArgumentException(nameof(unit), (int)unit, typeof(TimeUnit))
            };
        }

        /// <summary>
        /// Returns a short string representation of the time unit.
        /// </summary>
        /// <param name="unit">The time unit.</param>
        /// <returns>A short string representation of the time unit.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the enum value is not defined.</exception>
        public static string ToShortString(this TimeUnit unit)
        {
            return unit switch
            {
                TimeUnit.Days => "D",
                TimeUnit.Weeks => "W",
                TimeUnit.Months => "M", 
                TimeUnit.Years => "Y",
                _ => throw new InvalidEnumArgumentException(nameof(unit), (int)unit, typeof(TimeUnit))
            };
        }
    }
}