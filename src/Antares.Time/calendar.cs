// Calendar.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace Antares.Time
{
    /// <summary>
    /// Calendar class for business day logic.
    /// </summary>
    /// <remarks>
    /// This class provides methods for determining whether a date is a business day or a holiday,
    /// and for incrementing/decrementing a date by a given number of business days.
    /// </remarks>
    public abstract class Calendar
    {
        private readonly HashSet<Date> _addedHolidays = new HashSet<Date>();
        private readonly HashSet<Date> _removedHolidays = new HashSet<Date>();

        #region Abstract Interface
        /// <summary>
        /// The name of the calendar.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// The implementation of business day logic for a specific calendar.
        /// </summary>
        protected abstract bool IsBusinessDayImpl(Date d);

        /// <summary>
        /// Returns true if the weekday is part of the weekend for this calendar.
        /// </summary>
        public abstract bool IsWeekend(Weekday w);
        #endregion

        #region Public Interface
        /// <summary>
        /// Returns whether the calendar has a valid implementation.
        /// </summary>
        public bool IsEmpty() => string.IsNullOrEmpty(Name);

        /// <summary>
        /// Returns true if the date is a business day for the given market.
        /// </summary>
        public bool IsBusinessDay(Date d)
        {
            if (_addedHolidays.Contains(d))
                return false;
            if (_removedHolidays.Contains(d))
                return true;
            return IsBusinessDayImpl(d);
        }

        /// <summary>
        /// Returns true if the date is a holiday for the given market.
        /// </summary>
        public bool IsHoliday(Date d) => !IsBusinessDay(d);

        /// <summary>
        /// Returns true if the weekday is part of the weekend for this calendar.
        /// </summary>
        public bool IsWeekend(Date d) => IsWeekend(d.Weekday);

        /// <summary>
        /// Returns true if the date is the last business day for the month in given market.
        /// </summary>
        public bool IsEndOfMonth(Date d) => d.Month != Adjust(d + 1).Month;

        /// <summary>
        /// Last business day of the month to which the given date belongs.
        /// </summary>
        public Date EndOfMonth(Date d) => Adjust(Date.EndOfMonth(d), BusinessDayConvention.Preceding);

        /// <summary>
        /// Adds a date to the set of holidays for the given calendar.
        /// </summary>
        public void AddHoliday(Date d)
        {
            _removedHolidays.Remove(d);
            if (IsBusinessDayImpl(d))
                _addedHolidays.Add(d);
        }

        /// <summary>
        /// Removes a date from the set of holidays for the given calendar.
        /// </summary>
        public void RemoveHoliday(Date d)
        {
            _addedHolidays.Remove(d);
            if (!IsBusinessDayImpl(d))
                _removedHolidays.Add(d);
        }

        /// <summary>
        /// Adjusts a non-business day to the appropriate near business day with respect to the given convention.
        /// </summary>
        public Date Adjust(Date d, BusinessDayConvention convention = BusinessDayConvention.Following)
        {
            if (d == default)
                throw new ArgumentException("null date");

            if (convention == BusinessDayConvention.Unadjusted)
                return d;

            Date d1 = d;
            if (convention == BusinessDayConvention.Following ||
                convention == BusinessDayConvention.ModifiedFollowing ||
                convention == BusinessDayConvention.HalfMonthModifiedFollowing)
            {
                while (IsHoliday(d1))
                    d1++;
                if (convention == BusinessDayConvention.ModifiedFollowing ||
                    convention == BusinessDayConvention.HalfMonthModifiedFollowing)
                {
                    if (d1.Month != d.Month)
                        return Adjust(d, BusinessDayConvention.Preceding);

                    if (convention == BusinessDayConvention.HalfMonthModifiedFollowing &&
                        d.DayOfMonth <= 15 && d1.DayOfMonth > 15)
                    {
                        return Adjust(d, BusinessDayConvention.Preceding);
                    }
                }
            }
            else if (convention == BusinessDayConvention.Preceding ||
                     convention == BusinessDayConvention.ModifiedPreceding)
            {
                while (IsHoliday(d1))
                    d1--;
                if (convention == BusinessDayConvention.ModifiedPreceding && d1.Month != d.Month)
                {
                    return Adjust(d, BusinessDayConvention.Following);
                }
            }
            else if (convention == BusinessDayConvention.Nearest)
            {
                Date d2 = d;
                while (IsHoliday(d1) && IsHoliday(d2))
                {
                    d1++;
                    d2--;
                }
                return IsHoliday(d1) ? d2 : d1;
            }
            else
            {
                throw new ArgumentException("unknown business-day convention");
            }
            return d1;
        }

        /// <summary>
        /// Advances the given date by the given period and returns the result.
        /// </summary>
        public Date Advance(Date d, Period period, BusinessDayConvention convention = BusinessDayConvention.Following, bool endOfMonth = false)
        {
            return Advance(d, period.Length, period.Units, convention, endOfMonth);
        }

        /// <summary>
        /// Advances the given date by the given number of business days and returns the result.
        /// </summary>
        public Date Advance(Date d, int n, TimeUnit unit, BusinessDayConvention convention = BusinessDayConvention.Following, bool endOfMonth = false)
        {
            if (d == default)
                throw new ArgumentException("null date");

            if (n == 0)
            {
                return Adjust(d, convention);
            }
            else if (unit == TimeUnit.Days)
            {
                Date d1 = d;
                if (n > 0)
                {
                    while (n > 0)
                    {
                        d1++;
                        while (IsHoliday(d1))
                            d1++;
                        n--;
                    }
                }
                else
                {
                    while (n < 0)
                    {
                        d1--;
                        while (IsHoliday(d1))
                            d1--;
                        n++;
                    }
                }
                return d1;
            }
            else
            {
                Date d1 = d + new Period(n, unit);
                return Adjust(d1, convention);
            }
        }

        /// <summary>
        /// Calculates the number of business days between two given dates and returns the result.
        /// </summary>
        public int BusinessDaysBetween(Date from, Date to, bool includeFirst = true, bool includeLast = false)
        {
            return BusinessDaysBetweenImpl(this, from, to, includeFirst, includeLast);
        }

        private static int BusinessDaysBetweenImpl(Calendar cal, Date from, Date to, bool includeFirst, bool includeLast)
        {
            int result = includeLast && cal.IsBusinessDay(to) ? 1 : 0;
            for (Date d = includeFirst ? from : from + 1; d < to; d++)
            {
                if (cal.IsBusinessDay(d))
                    result++;
            }
            return result;
        }

        public List<Date> HolidayList(Date from, Date to, bool includeWeekends = false)
        {
            if (to < from)
                throw new ArgumentException($"'from' date ({from}) must be equal to or earlier than 'to' date ({to})");

            var result = new List<Date>();
            for (Date d = from; d <= to; d++)
            {
                if (IsHoliday(d) && (includeWeekends || !IsWeekend(d.Weekday)))
                    result.Add(d);
            }
            return result;
        }
        #endregion

        #region Equality and Formatting
        public override bool Equals(object? obj) => obj is Calendar other && this.Name == other.Name;
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
        public override string ToString() => Name;

        // Remove the problematic binary operators - these will be handled by Equals() method
        #endregion
    }

    /// <summary>
    /// Partial calendar implementation for Western calendars.
    /// </summary>
    public abstract class WesternCalendar : Calendar
    {
        public override bool IsWeekend(Weekday w) => w == Weekday.Saturday || w == Weekday.Sunday;

        protected static int EasterMonday(int year)
        {
            byte[] easterMondayData = {
                98,90,103,95,114,106,91,111,102,9,96,87,107,92,112,103,95,108,100,91,111,96,88,107,92,
                112,104,88,108,100,85,104,96,116,101,92,112,97,89,108,100,85,105,96,109,101,93,112,97,
                89,109,93,113,105,90,109,101,86,106,97,89,102,94,113,105,90,110,101,86,106,98,110,102,
                94,114,98,90,110,95,86,106,91,111,102,94,107,99,90,103,95,115,106,91,111,103,87,107,99,
                84,103,95,115,100,91,111,96,88,107,92,112,104,95,108,100,92,111,96,88,108,92,112,104,89,
                108,100,85,105,96,116,101,93,112,97,89,109,100,85,105,97,109,101,93,113,97,89,109,94,
                113,105,90,110,101,86,106,98,89,102,94,114,105,90,110,102,86,106,98,111,102,94,114,99,
                90,110,95,87,106,91,111,103,94,107,99,91,103,95,115,107,91,111,103,88,108,100,85,105,
                96,109,101,93,112,97,89,109,93,113,105,90,109,101,86,106,97,89,102,94,113,105,90,110,
                101,86,106,98,110,102,94,114,98,90,110,95,86,106,91,111,102,94,107,99,90,103,95,115,
                106,91,111,103,87,107,99,84,103,95,115,100,91,111,96,88,107,92,112,104,95,108,100,92,
                111,96,88,108,92,112,104,89,108,100,85,105,96,116,101,93,112,97,89,109,100,85,105
            };
            return easterMondayData[year - 1901];
        }
    }
}