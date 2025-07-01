// Weekday.cs

using System;
using System.ComponentModel;

namespace Antares.Time
{
    /// <summary>
    /// Day's serial number MOD 7.
    /// This enum is equivalent to the WEEKDAY Excel function, except for Sunday = 7 in Excel.
    /// Note: This enum is 1-indexed (Sunday = 1) to match QuantLib, unlike .NET's
    /// System.DayOfWeek which is 0-indexed.
    /// </summary>
    public enum Weekday
    {
        Sunday = 1,
        Monday = 2,
        Tuesday = 3,
        Wednesday = 4,
        Thursday = 5,
        Friday = 6,
        Saturday = 7
    }

    /// <summary>
    /// Provides extension methods for the Weekday enum, offering various string formatting options.
    /// This class replaces the C++ stream manipulator pattern.
    /// </summary>
    public static class WeekdayExtensions
    {
        /// <summary>
        /// Returns the full name of the weekday (e.g., "Sunday").
        /// This corresponds to the C++ `io::long_weekday` manipulator.
        /// </summary>
        /// <param name="w">The weekday.</param>
        /// <returns>The full name of the weekday.</returns>
        public static string ToLongString(this Weekday w)
        {
            return w switch
            {
                Weekday.Sunday => "Sunday",
                Weekday.Monday => "Monday",
                Weekday.Tuesday => "Tuesday",
                Weekday.Wednesday => "Wednesday",
                Weekday.Thursday => "Thursday",
                Weekday.Friday => "Friday",
                Weekday.Saturday => "Saturday",
                _ => throw new InvalidEnumArgumentException(nameof(w), (int)w, typeof(Weekday))
            };
        }

        /// <summary>
        /// Returns the three-letter abbreviation of the weekday (e.g., "Sun").
        /// This corresponds to the C++ `io::short_weekday` manipulator.
        /// </summary>
        /// <param name="w">The weekday.</param>
        /// <returns>The three-letter abbreviation of the weekday.</returns>
        public static string ToShortString(this Weekday w)
        {
            return w switch
            {
                Weekday.Sunday => "Sun",
                Weekday.Monday => "Mon",
                Weekday.Tuesday => "Tue",
                Weekday.Wednesday => "Wed",
                Weekday.Thursday => "Thu",
                Weekday.Friday => "Fri",
                Weekday.Saturday => "Sat",
                _ => throw new InvalidEnumArgumentException(nameof(w), (int)w, typeof(Weekday))
            };
        }

        /// <summary>
        /// Returns the two-letter abbreviation of the weekday (e.g., "Su").
        /// This corresponds to the C++ `io::shortest_weekday` manipulator.
        /// </summary>
        /// <param name="w">The weekday.</param>
        /// <returns>The two-letter abbreviation of the weekday.</returns>
        public static string ToShortestString(this Weekday w)
        {
            return w switch
            {
                Weekday.Sunday => "Su",
                Weekday.Monday => "Mo",
                Weekday.Tuesday => "Tu",
                Weekday.Wednesday => "We",
                Weekday.Thursday => "Th",
                Weekday.Friday => "Fr",
                Weekday.Saturday => "Sa",
                _ => throw new InvalidEnumArgumentException(nameof(w), (int)w, typeof(Weekday))
            };
        }

        /// <summary>
        /// Returns a string representation of the weekday. Defaults to the long format.
        /// This corresponds to the C++ `operator<<` overload for Weekday.
        /// </summary>
        /// <param name="w">The weekday.</param>
        /// <returns>The full name of the weekday.</returns>
        public static string ToFormattedString(this Weekday w)
        {
            return w.ToLongString();
        }
    }
}