// TermStructure.cs

using System;
using System.Collections.Generic;

namespace Antares
{
    #region Missing Type Definitions

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
    /// Abstract base class for day counter implementations.
    /// </summary>
    public abstract class DayCounter : IEquatable<DayCounter>
    {
        public abstract string Name { get; }
        public virtual int DayCount(Date d1, Date d2)
        {
            return d2.CompareTo(d1);
        }
        public abstract double YearFraction(Date d1, Date d2, Date? refPeriodStart = null, Date? refPeriodEnd = null);
        
        public bool IsEmpty() => string.IsNullOrEmpty(Name);
        
        public bool Equals(DayCounter? other)
        {
            if (other is null) return false;
            if (this.IsEmpty() && other.IsEmpty()) return true;
            return this.Name == other.Name;
        }

        public override bool Equals(object? obj) => obj is DayCounter other && this.Equals(other);
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
        public override string ToString() => Name ?? "No day counter implementation provided";
        
        public static bool operator ==(DayCounter d1, DayCounter d2)
        {
            if (d1 is null) return d2 is null;
            return d1.Equals(d2);
        }
        public static bool operator !=(DayCounter d1, DayCounter d2) => !(d1 == d2);
    }

    /// <summary>
    /// Actual/365 Fixed day counter.
    /// </summary>
    public class Actual365Fixed : DayCounter
    {
        public override string Name => "Actual/365 (Fixed)";
        
        public override double YearFraction(Date d1, Date d2, Date? refPeriodStart = null, Date? refPeriodEnd = null)
        {
            return DayCount(d1, d2) / 365.0;
        }
    }

    /// <summary>
    /// Abstract base class for calendar implementations.
    /// </summary>
    public abstract class Calendar : IEquatable<Calendar>
    {
        public abstract string Name { get; }
        
        public virtual bool IsBusinessDay(Date d)
        {
            return !IsHoliday(d);
        }
        
        public virtual bool IsHoliday(Date d)
        {
            return IsWeekend(GetWeekday(d)) || IsBusinessDayImpl(d);
        }
        
        protected abstract bool IsBusinessDayImpl(Date d);
        
        public virtual bool IsWeekend(int weekday)
        {
            return weekday == 0 || weekday == 6; // Sunday = 0, Saturday = 6
        }
        
        protected virtual int GetWeekday(Date d)
        {
            // Simple implementation - in real implementation would use actual date
            return 1; // Monday
        }
        
        public virtual Date Advance(Date date, int n, TimeUnit unit)
        {
            DateTime dateTime = ConvertToDateTime(date);
            switch (unit)
            {
                case TimeUnit.Days:
                    return new Date(dateTime.AddDays(n));
                case TimeUnit.Weeks:
                    return new Date(dateTime.AddDays(n * 7));
                case TimeUnit.Months:
                    return new Date(dateTime.AddMonths(n));
                case TimeUnit.Years:
                    return new Date(dateTime.AddYears(n));
                default:
                    throw new ArgumentException($"Unknown time unit: {unit}");
            }
        }
        
        // Helper method for date conversion
        private static DateTime ConvertToDateTime(Date date)
        {
            return DateTime.Today; // Simplified implementation
        }
        
        public override bool Equals(object? obj) => obj is Calendar other && this.Name == other.Name;
        public override int GetHashCode() => Name?.GetHashCode() ?? 0;
        public override string ToString() => Name;
        
        public static bool operator ==(Calendar c1, Calendar c2)
        {
            if (c1 is null) return c2 is null;
            return c1.Equals(c2);
        }
        public static bool operator !=(Calendar c1, Calendar c2) => !(c1 == c2);
        
        public bool Equals(Calendar? other) => other != null && Name == other.Name;
    }

    /// <summary>
    /// TARGET calendar implementation.
    /// </summary>
    public class TARGET : Calendar
    {
        public override string Name => "TARGET";
        
        protected override bool IsBusinessDayImpl(Date d)
        {
            return true; // Simplified implementation
        }
    }

    /// <summary>
    /// Defines behavior for classes that can perform extrapolation.
    /// </summary>
    public interface IExtrapolator
    {
        bool AllowsExtrapolation { get; }
        void EnableExtrapolation(bool value);
    }

    /// <summary>
    /// Provides the global evaluation date and notifies observers on change.
    /// This is a simplified C# equivalent of QuantLib's Settings singleton.
    /// </summary>
    public static class EvaluationSettings
    {
        private static readonly Observable _observable = new Observable();
        private static Date _evaluationDate = Date.Today;

        public static Date EvaluationDate
        {
            get => _evaluationDate;
            set
            {
                if (_evaluationDate != value)
                {
                    _evaluationDate = value;
                    _observable.NotifyObservers();
                }
            }
        }

        public static void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public static void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
    }

    #endregion

    /// <summary>
    /// Basic term-structure functionality.
    /// Base class for interest-rate and volatility term structures.
    /// </summary>
    public abstract class TermStructure : IObserver, IObservable, IExtrapolator
    {
        private readonly Observable _observable = new Observable();
        private bool _extrapolationAllowed = false;

        private readonly bool _moving;
        private bool _updated;

        private readonly Calendar _calendar;
        private Date _referenceDate;
        private readonly Natural? _settlementDays;
        private readonly DayCounter _dayCounter;

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <remarks>
        /// Term structures initialized by means of this constructor must manage
        /// their own reference date by overriding the ReferenceDate property.
        /// </remarks>
        protected TermStructure(DayCounter? dayCounter = null)
        {
            _dayCounter = dayCounter ?? new Actual365Fixed();
            _settlementDays = null; // Equivalent to Null<Natural>()
            _updated = true;
            _moving = false;
            _calendar = new TARGET(); // Default calendar
        }

        /// <summary>
        /// Initialize with a fixed reference date.
        /// </summary>
        protected TermStructure(Date referenceDate, Calendar? calendar = null, DayCounter? dayCounter = null)
        {
            _calendar = calendar ?? new TARGET();
            _dayCounter = dayCounter ?? new Actual365Fixed();
            _referenceDate = referenceDate;
            _settlementDays = null;
            _updated = true;
            _moving = false;
        }

        /// <summary>
        /// Calculate the reference date based on the global evaluation date.
        /// </summary>
        protected TermStructure(Natural settlementDays, Calendar calendar, DayCounter? dayCounter = null)
        {
            _calendar = calendar ?? throw new ArgumentNullException(nameof(calendar));
            _dayCounter = dayCounter ?? new Actual365Fixed();
            _settlementDays = settlementDays;
            _moving = true;
            _updated = false;
            EvaluationSettings.RegisterWith(this);
        }
        #endregion

        #region Dates and Time
        /// <summary>
        /// The day counter used for date/time conversion.
        /// </summary>
        public DayCounter DayCounter => _dayCounter;

        /// <summary>
        /// Date/time conversion.
        /// </summary>
        public Time TimeFromReference(Date date) => DayCounter.YearFraction(ReferenceDate, date);

        /// <summary>
        /// The latest date for which the curve can return values.
        /// </summary>
        public abstract Date MaxDate { get; }

        /// <summary>
        /// The latest time for which the curve can return values.
        /// </summary>
        public virtual Time MaxTime => TimeFromReference(MaxDate);

        /// <summary>
        /// The date at which discount = 1.0 and/or variance = 0.0.
        /// </summary>
        public virtual Date ReferenceDate
        {
            get
            {
                if (!_updated)
                {
                    Date today = EvaluationSettings.EvaluationDate;
                    _referenceDate = Calendar.Advance(today, (int)SettlementDays, TimeUnit.Days);
                    _updated = true;
                }
                return _referenceDate;
            }
        }

        /// <summary>
        /// The calendar used for reference and/or option date calculation.
        /// </summary>
        public Calendar Calendar => _calendar;

        /// <summary>
        /// The settlement days used for reference date calculation.
        /// </summary>
        public Natural SettlementDays
        {
            get
            {
                if (!_settlementDays.HasValue)
                    throw new InvalidOperationException("settlement days not provided for this instance");
                return _settlementDays.Value;
            }
        }
        #endregion

        #region IObserver interface
        public virtual void Update()
        {
            if (_moving)
                _updated = false;
            NotifyObservers();
        }
        #endregion

        #region IObservable interface
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
        #endregion

        #region IExtrapolator interface
        public bool AllowsExtrapolation => _extrapolationAllowed;
        public void EnableExtrapolation(bool value) => _extrapolationAllowed = value;
        #endregion

        #region Range checks
        /// <summary>
        /// Date-range check.
        /// </summary>
        protected void CheckRange(Date d, bool extrapolate)
        {
            if (d < ReferenceDate || d > MaxDate)
            {
                if (!extrapolate || !AllowsExtrapolation)
                    throw new ArgumentException($"Date {d} is outside the range [{ReferenceDate}, {MaxDate}]");
            }
        }

        /// <summary>
        /// Time-range check.
        /// </summary>
        protected void CheckRange(Time t, bool extrapolate)
        {
            if (t < 0.0 || t > MaxTime)
            {
                if (!extrapolate || !AllowsExtrapolation)
                    throw new ArgumentException($"Time {t} is outside the range [0, {MaxTime}]");
            }
        }
        #endregion
    }
}