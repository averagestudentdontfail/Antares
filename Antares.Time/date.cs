// Date.cs

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using QLNetDate = QLNet.Time.Date; // Alias to avoid naming conflicts
using QLNetMonth = QLNet.Time.Month;
using QLNetWeekday = QLNet.Time.DayOfWeek;


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
    /// Concrete date class.
    /// </summary>
    /// <remarks>
    /// This class provides methods to inspect dates as well as methods and
    /// operators which implement a limited date algebra. It is a wrapper around
    /// the robust `QLNet.Date` type, which uses NodaTime internally.
    /// This corresponds to the `QL_HIGH_RESOLUTION_DATE` path in the C++ code.
    /// </remarks>
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct Date : IEquatable<Date>, IComparable<Date>
    {
        private readonly QLNetDate _qlDate;

        #region Constructors
        /// <summary>
        /// Constructor taking a serial number as given by Excel.
        /// </summary>
        public Date(int serialNumber)
        {
            CheckSerialNumber(serialNumber);
            _qlDate = new QLNetDate(serialNumber);
        }

        /// <summary>
        /// More traditional constructor.
        /// </summary>
        public Date(int day, Month month, int year)
        {
            _qlDate = new QLNetDate(day, (QLNetMonth)month, year);
        }

        /// <summary>
        /// Private constructor for internal wrapping.
        /// </summary>
        private Date(QLNetDate qlDate)
        {
            _qlDate = qlDate;
        }
        #endregion

        #region Inspectors
        public Weekday Weekday => (Weekday)_qlDate.DayOfWeek;
        public int DayOfMonth => _qlDate.Day;
        public int DayOfYear => _qlDate.DayOfYear;
        public Month Month => (Month)_qlDate.Month;
        public int Year => _qlDate.Year;
        public int SerialNumber => _qlDate.serialNumber();
        #endregion

        #region Operators
        public static Date operator +(Date d, int days) => new Date(d._qlDate + days);
        public static Date operator +(Date d, Period p) => new Date(d._qlDate + p.ToQlNetPeriod());
        public static Date operator -(Date d, int days) => new Date(d._qlDate - days);
        public static Date operator -(Date d, Period p) => new Date(d._qlDate - p.ToQlNetPeriod());
        public static int operator -(Date d1, Date d2) => d1._qlDate - d2._qlDate;

        public static Date operator ++(Date d) => new Date(d._qlDate + 1);
        public static Date operator --(Date d) => new Date(d._qlDate - 1);
        #endregion

        #region Static Methods
        public static Date Today => new Date(QLNetDate.Today);
        public static Date MinDate => new Date(QLNetDate.minDate());
        public static Date MaxDate => new Date(QLNetDate.maxDate());
        public static bool IsLeap(int year) => QLNetDate.IsLeap(year);
        public static Date EndOfMonth(Date d) => new Date(QLNetDate.endOfMonth(d._qlDate));
        public static bool IsEndOfMonth(Date d) => QLNetDate.isEndOfMonth(d._qlDate);

        public static Date NextWeekday(Date d, Weekday w) =>
            new Date(QLNetDate.nextWeekday(d._qlDate, (QLNetWeekday)w));

        public static Date NthWeekday(int n, Weekday w, Month m, int y) =>
            new Date(QLNetDate.nthWeekday(n, (QLNetWeekday)w, (QLNetMonth)m, y));
        #endregion

        #region Formatting and Equality
        public override string ToString() => this.ToLongString();
        public bool Equals(Date other) => this._qlDate == other._qlDate;
        public override bool Equals(object obj) => obj is Date other && this.Equals(other);
        public override int GetHashCode() => _qlDate.GetHashCode();
        public int CompareTo(Date other) => _qlDate.CompareTo(other._qlDate);

        private static void CheckSerialNumber(int serialNumber)
        {
            QL.Require(serialNumber >= QLNetDate.minDate().serialNumber() &&
                       serialNumber <= QLNetDate.maxDate().serialNumber(),
                $"Date's serial number ({serialNumber}) outside allowed range");
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
    }

    /// <summary>
    /// Provides extension methods for formatting Date objects.
    /// </summary>
    public static class DateExtensions
    {
        internal static QLNet.Period ToQlNetPeriod(this Period p)
        {
            return new QLNet.Period(p.Length, (QLNet.TimeUnit)p.Units);
        }

        /// <summary>
        /// Outputs date in long format (e.g., "January 1st, 2023").
        /// </summary>
        public static string ToLongString(this Date d)
        {
            if (d == default) return "null date";
            return $"{d.Month.ToLongString()} {Utility.DataFormatters.ToOrdinal(d.DayOfMonth)}, {d.Year}";
        }

        /// <summary>
        /// Outputs date in short format (e.g., "01/15/2023").
        /// </summary>
        public static string ToShortString(this Date d)
        {
            if (d == default) return "null date";
            return d.Month.ToString("00") + "/" + d.DayOfMonth.ToString("00") + "/" + d.Year;
        }

        /// <summary>
        /// Outputs date in ISO format (e.g., "2023-01-15").
        /// </summary>
        public static string ToIsoString(this Date d)
        {
            if (d == default) return "null date";
            return $"{d.Year}-{((int)d.Month):00}-{d.DayOfMonth:00}";
        }
    }
}