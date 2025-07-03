// Date.cs

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace Antares.Time
{
    /// <summary>
    /// Month names.
    /// </summary>
    public enum Month
    {
        January = 1,
        February = 2,
        March = 3,
        April = 4,
        May = 5,
        June = 6,
        July = 7,
        August = 8,
        September = 9,
        October = 10,
        November = 11,
        December = 12
    }

    /// <summary>
    /// Weekday names.
    /// </summary>
    public enum Weekday
    {
        Sunday = 0,
        Monday = 1,
        Tuesday = 2,
        Wednesday = 3,
        Thursday = 4,
        Friday = 5,
        Saturday = 6
    }

    public static class MonthExtensions
    {
        /// <summary>
        /// Returns the full name of the month.
        /// </summary>
        public static string ToLongString(this Month m)
        {
            return m switch
            {
                Month.January => "January",
                Month.February => "February",
                Month.March => "March",
                Month.April => "April",
                Month.May => "May",
                Month.June => "June",
                Month.July => "July",
                Month.August => "August",
                Month.September => "September",
                Month.October => "October",
                Month.November => "November",
                Month.December => "December",
                _ => throw new InvalidEnumArgumentException(nameof(m), (int)m, typeof(Month))
            };
        }
    }

    /// <summary>
    /// Concrete date class using native .NET DateOnly.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct Date : IEquatable<Date>, IComparable<Date>
    {
        private readonly DateOnly _date;

        #region Constructors
        /// <summary>
        /// Constructor taking a serial number as given by Excel.
        /// </summary>
        public Date(int serialNumber)
        {
            CheckSerialNumber(serialNumber);
            _date = DateOnly.FromDayNumber(serialNumber + ExcelEpochOffset);
        }

        /// <summary>
        /// More traditional constructor.
        /// </summary>
        public Date(int day, Month month, int year)
        {
            _date = new DateOnly(year, (int)month, day);
        }

        /// <summary>
        /// Constructor from DateOnly.
        /// </summary>
        public Date(DateOnly date)
        {
            _date = date;
        }

        /// <summary>
        /// Constructor from DateTime.
        /// </summary>
        public Date(DateTime dateTime)
        {
            _date = DateOnly.FromDateTime(dateTime);
        }
        #endregion

        #region Constants
        private const int ExcelEpochOffset = -693594; // Offset to convert .NET day numbers to Excel serial numbers
        private const int MinSerialNumber = 367; // January 1, 1901 in Excel
        private const int MaxSerialNumber = 109574; // December 31, 2199 in Excel
        #endregion

        #region Inspectors
        public Weekday Weekday => (Weekday)_date.DayOfWeek;
        public int DayOfMonth => _date.Day;
        public int DayOfYear => _date.DayOfYear;
        public Month Month => (Month)_date.Month;
        public int Year => _date.Year;
        public int SerialNumber => _date.DayNumber - ExcelEpochOffset;
        #endregion

        #region Operators
        public static Date operator +(Date d, int days) => new Date(d._date.AddDays(days));
        public static Date operator +(Date d, Period p) => d.AddPeriod(p);
        public static Date operator -(Date d, int days) => new Date(d._date.AddDays(-days));
        public static Date operator -(Date d, Period p) => d.SubtractPeriod(p);
        public static int operator -(Date d1, Date d2) => d1._date.DayNumber - d2._date.DayNumber;

        public static Date operator ++(Date d) => new Date(d._date.AddDays(1));
        public static Date operator --(Date d) => new Date(d._date.AddDays(-1));
        #endregion

        #region Static Methods
        public static Date Today => new Date(DateOnly.FromDateTime(DateTime.Today));
        public static Date MinDate => new Date(MinSerialNumber);
        public static Date MaxDate => new Date(MaxSerialNumber);
        public static bool IsLeap(int year) => DateTime.IsLeapYear(year);
        
        public static Date EndOfMonth(Date d)
        {
            var lastDay = DateTime.DaysInMonth(d.Year, (int)d.Month);
            return new Date(lastDay, d.Month, d.Year);
        }
        
        public static bool IsEndOfMonth(Date d)
        {
            var lastDay = DateTime.DaysInMonth(d.Year, (int)d.Month);
            return d.DayOfMonth == lastDay;
        }

        public static Date NextWeekday(Date d, Weekday w)
        {
            var days = ((int)w - (int)d.Weekday + 7) % 7;
            if (days == 0) days = 7; // Next occurrence, not today
            return d + days;
        }

        public static Date NthWeekday(int n, Weekday w, Month m, int y)
        {
            var firstOfMonth = new Date(1, m, y);
            var firstWeekday = (int)firstOfMonth.Weekday;
            var targetWeekday = (int)w;
            
            var daysToFirst = (targetWeekday - firstWeekday + 7) % 7;
            var targetDate = firstOfMonth + daysToFirst + (n - 1) * 7;
            
            if (targetDate.Month != m)
                throw new ArgumentException($"The {n}th {w} of {m} {y} does not exist.");
                
            return targetDate;
        }
        #endregion

        #region Helper Methods
        private Date AddPeriod(Period p)
        {
            return p.Units switch
            {
                TimeUnit.Days => new Date(_date.AddDays(p.Length)),
                TimeUnit.Weeks => new Date(_date.AddDays(p.Length * 7)),
                TimeUnit.Months => new Date(_date.AddMonths(p.Length)),
                TimeUnit.Years => new Date(_date.AddYears(p.Length)),
                _ => throw new ArgumentException($"Unknown time unit: {p.Units}")
            };
        }

        private Date SubtractPeriod(Period p)
        {
            return p.Units switch
            {
                TimeUnit.Days => new Date(_date.AddDays(-p.Length)),
                TimeUnit.Weeks => new Date(_date.AddDays(-p.Length * 7)),
                TimeUnit.Months => new Date(_date.AddMonths(-p.Length)),
                TimeUnit.Years => new Date(_date.AddYears(-p.Length)),
                _ => throw new ArgumentException($"Unknown time unit: {p.Units}")
            };
        }

        private static void CheckSerialNumber(int serialNumber)
        {
            if (serialNumber < MinSerialNumber || serialNumber > MaxSerialNumber)
                throw new ArgumentOutOfRangeException(nameof(serialNumber), 
                    $"Date's serial number ({serialNumber}) outside allowed range [{MinSerialNumber}, {MaxSerialNumber}]");
        }
        #endregion

        #region Formatting and Equality
        public override string ToString() => this.ToLongString();
        public bool Equals(Date other) => this._date == other._date;
        public override bool Equals(object? obj) => obj is Date other && this.Equals(other);
        public override int GetHashCode() => _date.GetHashCode();
        public int CompareTo(Date other) => _date.CompareTo(other._date);

        private static void CheckSerialNumber(int serialNumber)
        {
            if (serialNumber < MinSerialNumber || serialNumber > MaxSerialNumber)
                throw new ArgumentOutOfRangeException(nameof(serialNumber), 
                    $"Date's serial number ({serialNumber}) outside allowed range [{MinSerialNumber}, {MaxSerialNumber}]");
        }
        #endregion
        
        #region Comparison Operators
        public static bool operator ==(Date left, Date right) => left.Equals(right);
        public static bool operator !=(Date left, Date right) => !left.Equals(right);
        public static bool operator <(Date left, Date right) => left.CompareTo(right) < 0;
        public static bool operator >(Date left, Date right) => left.CompareTo(right) > 0;
        public static bool operator <=(Date left, Date right) => left.CompareTo(right) <= 0;
        public static bool operator >=(Date left, Date right) => left.CompareTo(right) >= 0;
        #endregion

        #region Implicit Conversions
        public static implicit operator DateOnly(Date date) => date._date;
        public static implicit operator Date(DateOnly dateOnly) => new Date(dateOnly);
        #endregion
    }

    /// <summary>
    /// Time unit enumeration.
    /// </summary>
    public enum TimeUnit
    {
        Days,
        Weeks,
        Months,
        Years
    }

    /// <summary>
    /// Period class representing a time span.
    /// </summary>
    public readonly struct Period : IEquatable<Period>
    {
        public int Length { get; }
        public TimeUnit Units { get; }

        public Period(int length, TimeUnit units)
        {
            Length = length;
            Units = units;
        }

        public bool Equals(Period other) => Length == other.Length && Units == other.Units;
        public override bool Equals(object? obj) => obj is Period other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Length, Units);
        public override string ToString() => $"{Length}{Units switch { TimeUnit.Days => "D", TimeUnit.Weeks => "W", TimeUnit.Months => "M", TimeUnit.Years => "Y", _ => "?" }}";

        public static bool operator ==(Period left, Period right) => left.Equals(right);
        public static bool operator !=(Period left, Period right) => !left.Equals(right);
    }

    /// <summary>
    /// Provides extension methods for formatting Date objects.
    /// </summary>
    public static class DateExtensions
    {
        /// <summary>
        /// Outputs date in long format (e.g., "January 1st, 2023").
        /// </summary>
        public static string ToLongString(this Date d)
        {
            if (d == default) return "null date";
            return $"{d.Month.ToLongString()} {ToOrdinal(d.DayOfMonth)}, {d.Year}";
        }

        /// <summary>
        /// Outputs date in short format (e.g., "01/15/2023").
        /// </summary>
        public static string ToShortString(this Date d)
        {
            if (d == default) return "null date";
            return $"{(int)d.Month:00}/{d.DayOfMonth:00}/{d.Year}";
        }

        /// <summary>
        /// Outputs date in ISO format (e.g., "2023-01-15").
        /// </summary>
        public static string ToIsoString(this Date d)
        {
            if (d == default) return "null date";
            return $"{d.Year}-{(int)d.Month:00}-{d.DayOfMonth:00}";
        }

        private static string ToOrdinal(int number)
        {
            return number switch
            {
                1 or 21 or 31 => $"{number}st",
                2 or 22 => $"{number}nd",
                3 or 23 => $"{number}rd",
                _ => $"{number}th"
            };
        }
    }
}