// BusinessDayConvention.cs

using System;
using System.ComponentModel;

namespace Antares.Time
{
    /// <summary>
    /// Business Day conventions.
    /// These conventions specify the algorithm used to adjust a date if it is not a valid business day.
    /// </summary>
    public enum BusinessDayConvention
    {
        /// <summary>
        /// Choose the first business day after the given holiday.
        /// </summary>
        Following,

        /// <summary>
        /// Choose the first business day after the given holiday unless it belongs
        /// to a different month, in which case choose the first business day before the holiday.
        /// </summary>
        ModifiedFollowing,

        /// <summary>
        /// Choose the first business day before the given holiday.
        /// </summary>
        Preceding,

        /// <summary>
        /// Choose the first business day before the given holiday unless it belongs
        /// to a different month, in which case choose the first business day after the holiday.
        /// </summary>
        ModifiedPreceding,

        /// <summary>
        /// Do not adjust the date.
        /// </summary>
        Unadjusted,

        /// <summary>
        /// Choose the first business day after the given holiday unless that day
        /// crosses the mid-month (15th) or the end of month, in which case choose
        /// the first business day before the holiday.
        /// </summary>
        HalfMonthModifiedFollowing,

        /// <summary>
        /// Choose the nearest business day to the given holiday. If both the
        /// preceding and following business days are equally far away, the
        /// following one is chosen.
        /// </summary>
        Nearest
    }

    /// <summary>
    /// Provides extension methods for the BusinessDayConvention enum.
    /// </summary>
    public static class BusinessDayConventionExtensions
    {
        /// <summary>
        /// Returns a string representation of the business day convention.
        /// This method mimics the C++ operator&lt;&lt; and provides error checking for invalid enum values.
        /// </summary>
        /// <param name="convention">The business day convention.</param>
        /// <returns>A string representation of the convention.</returns>
        /// <exception cref="InvalidEnumArgumentException">Thrown if the enum value is not defined.</exception>
        public static string ToFormattedString(this BusinessDayConvention convention)
        {
            return convention switch
            {
                BusinessDayConvention.Following => "Following",
                BusinessDayConvention.ModifiedFollowing => "Modified Following",
                BusinessDayConvention.Preceding => "Preceding",
                BusinessDayConvention.ModifiedPreceding => "Modified Preceding",
                BusinessDayConvention.Unadjusted => "Unadjusted",
                BusinessDayConvention.HalfMonthModifiedFollowing => "Half-Month Modified Following",
                BusinessDayConvention.Nearest => "Nearest",
                _ => throw new InvalidEnumArgumentException(nameof(convention), (int)convention, typeof(BusinessDayConvention))
            };
        }
    }
}