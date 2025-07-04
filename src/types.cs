// Types.cs
global using Integer = System.Int32;
global using BigInteger = System.Int64;
global using Natural = System.UInt32;
global using BigNatural = System.UInt64;
global using Real = System.Double;
global using Decimal = System.Double;
global using Size = System.Int32;
global using Time = System.Double;
global using DiscountFactor = System.Double;
global using Rate = System.Double;
global using Spread = System.Double;
global using Volatility = System.Double;
global using Probability = System.Double;
global using Price = System.Double;
global using Barrier = System.Double;
global using Strike = System.Double;
using System;
using System.Collections.Generic;

namespace Antares
{
    /// <summary>
    /// Additional type definitions and constants for the Antares library.
    /// </summary>
    public static class Types
    {
        /// <summary>
        /// Common mathematical and financial constants.
        /// </summary>
        public static class Constants
        {
            /// <summary>
            /// One basis point (0.01%).
            /// </summary>
            public const Real BasisPoint = 1.0e-4;

            /// <summary>
            /// Machine epsilon for floating-point comparisons.
            /// </summary>
            public const Real Epsilon = 2.2204460492503131e-016;

            /// <summary>
            /// Number of days in a year (for simple calculations).
            /// </summary>
            public const Integer DaysInYear = 365;

            /// <summary>
            /// Number of months in a year.
            /// </summary>
            public const Integer MonthsInYear = 12;

            /// <summary>
            /// Number of weeks in a year.
            /// </summary>
            public const Integer WeeksInYear = 52;
        }

        /// <summary>
        /// Common collection type aliases for financial data.
        /// </summary>
        public static class Collections
        {
            /// <summary>
            /// A sequence of times.
            /// </summary>
            public class TimeGrid : List<Time> { }

            /// <summary>
            /// A sequence of rates.
            /// </summary>
            public class RateCurve : List<Rate> { }

            /// <summary>
            /// A sequence of discount factors.
            /// </summary>
            public class DiscountCurve : List<DiscountFactor> { }

            /// <summary>
            /// A time series of values.
            /// </summary>
            /// <typeparam name="T">The type of values in the series.</typeparam>
            public class TimeSeries<T> : Dictionary<Date, T> { }
        }
    }

    #region Core Enumerations

    /// <summary>
    /// Position type (long or short).
    /// </summary>
    public enum Position
    {
        Long = 1,
        Short = -1
    }

    /// <summary>
    /// Duration calculation methods.
    /// </summary>
    public enum Duration
    {
        /// <summary>
        /// Simple duration calculation.
        /// </summary>
        Simple,
        /// <summary>
        /// Macaulay duration.
        /// </summary>
        Macaulay,
        /// <summary>
        /// Modified duration.
        /// </summary>
        Modified
    }

    /// <summary>
    /// Business day conventions.
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
        /// Do not adjust.
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
        /// preceding and following business days are equally far away, default
        /// to following business day.
        /// </summary>
        Nearest
    }

    /// <summary>
    /// Month enumeration.
    /// </summary>
    public enum Month
    {
        January = 1, February = 2, March = 3, April = 4, May = 5, June = 6,
        July = 7, August = 8, September = 9, October = 10, November = 11, December = 12,
        
        // Short forms
        Jan = 1, Feb = 2, Mar = 3, Apr = 4, Jun = 6, Jul = 7,
        Aug = 8, Sep = 9, Oct = 10, Nov = 11, Dec = 12
    }

    /// <summary>
    /// Weekday enumeration.
    /// </summary>
    public enum Weekday
    {
        Sunday = 1, Monday = 2, Tuesday = 3, Wednesday = 4,
        Thursday = 5, Friday = 6, Saturday = 7,
        
        // Short forms
        Sun = 1, Mon = 2, Tue = 3, Wed = 4, Thu = 5, Fri = 6, Sat = 7
    }

    /// <summary>
    /// Date generation rules.
    /// </summary>
    public enum DateGeneration
    {
        /// <summary>
        /// Backward from termination date to effective date.
        /// </summary>
        Backward,
        /// <summary>
        /// Forward from effective date to termination date.
        /// </summary>
        Forward,
        /// <summary>
        /// No intermediate dates between effective date and termination date.
        /// </summary>
        Zero,
        /// <summary>
        /// All dates but effective date and termination date are taken to be on the third Wednesday of their month.
        /// </summary>
        ThirdWednesday,
        /// <summary>
        /// All dates including effective date and termination date are taken to be on the third Wednesday
        /// of their month (with forward calculation).
        /// </summary>
        ThirdWednesdayInclusive,
        /// <summary>
        /// All dates but the effective date are taken to be the twentieth of their
        /// month (used for CDS schedules in emerging markets). The termination date is also modified.
        /// </summary>
        Twentieth,
        /// <summary>
        /// All dates but the effective date are taken to be the twentieth of an IMM
        /// month (used for CDS schedules). The termination date is also modified.
        /// </summary>
        TwentiethIMM,
        /// <summary>
        /// Same as TwentiethIMM with unrestricted date ends and log/short stub
        /// coupon period (old CDS convention).
        /// </summary>
        OldCDS,
        /// <summary>
        /// Credit derivatives standard rule since 'Big Bang' changes in 2009.
        /// </summary>
        CDS,
        /// <summary>
        /// Credit derivatives standard rule since December 20th, 2015.
        /// </summary>
        CDS2015
    }

    /// <summary>
    /// Cap/Floor types.
    /// </summary>
    public enum CapFloorType
    {
        Cap,
        Floor,
        Collar
    }

    /// <summary>
    /// Interest rate types.
    /// </summary>
    public enum InterestRateType
    {
        Fixed,
        Floating
    }

    #endregion

    #region Utility Structures

    /// <summary>
    /// Represents a null value for a given type.
    /// Mimics QuantLib's Null template.
    /// </summary>
    /// <typeparam name="T">The type for which to represent null.</typeparam>
    public sealed class NullValue<T>
    {
        private static readonly T _value = default(T)!;
        
        /// <summary>
        /// Returns the null value for type T.
        /// </summary>
        public static T Value => _value;
        
        /// <summary>
        /// Implicit conversion to T.
        /// </summary>
        public static implicit operator T(NullValue<T> _) => _value;
        
        private NullValue() { }
        
        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        public static NullValue<T> Instance { get; } = new NullValue<T>();
    }

    /// <summary>
    /// Non-generic version of Null for common types.
    /// </summary>
    public static class NullValues
    {
        public static Real NullReal => Real.NaN;
        public static Integer NullInteger => int.MinValue;
        public static Size NullSize => int.MinValue;
        public static Time NullTime => Real.NaN;
        public static Rate NullRate => Real.NaN;
        public static DiscountFactor NullDiscountFactor => Real.NaN;
    }

    #endregion

    #region Extension Methods

    /// <summary>
    /// Extension methods for common type operations.
    /// </summary>
    public static class AntaresTypeExtensions
    {
        /// <summary>
        /// Checks if a Real value is effectively null.
        /// </summary>
        public static bool IsNullValue(this Real value)
        {
            return double.IsNaN(value);
        }

        /// <summary>
        /// Checks if an Integer/Size value is effectively null.
        /// Note: Since Integer and Size are both aliases for int, this method handles both types.
        /// </summary>
        public static bool IsNullValue(this int value)
        {
            return value == int.MinValue;
        }

        /// <summary>
        /// Converts basis points to a decimal rate.
        /// </summary>
        public static Real FromBasisPoints(this Real basisPoints)
        {
            return basisPoints * Types.Constants.BasisPoint;
        }

        /// <summary>
        /// Converts a decimal rate to basis points.
        /// </summary>
        public static Real ToBasisPoints(this Real rate)
        {
            return rate / Types.Constants.BasisPoint;
        }
    }

    #endregion
}