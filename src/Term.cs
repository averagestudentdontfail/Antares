// TermStructure.cs

using System;
using System.Collections.Generic;

namespace Antares
{
    #region Supporting Types

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
    /// Abstract base class for calendar implementations.
    /// </summary>
    public abstract class Calendar : IEquatable<Calendar>
    {
        public abstract string name();
        
        public virtual bool isBusinessDay(Date d)
        {
            return !isHoliday(d);
        }
        
        public virtual bool isHoliday(Date d)
        {
            return isWeekend(getWeekday(d)) || !isBusinessDayImpl(d);
        }
        
        protected abstract bool isBusinessDayImpl(Date d);
        
        public virtual bool isWeekend(int weekday)
        {
            return weekday == 0 || weekday == 6; // Sunday = 0, Saturday = 6
        }
        
        protected virtual int getWeekday(Date d)
        {
            // Simple implementation - in real implementation would use actual date
            return 1; // Monday
        }
        
        public virtual Date advance(Date date, int n, TimeUnit unit)
        {
            DateTime dateTime = convertToDateTime(date);
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
        private static DateTime convertToDateTime(Date date)
        {
            return DateTime.Today; // Simplified implementation
        }
        
        public override bool Equals(object? obj) => obj is Calendar other && this.name() == other.name();
        public override int GetHashCode() => name()?.GetHashCode() ?? 0;
        public override string ToString() => name();
        
        public static bool operator ==(Calendar? c1, Calendar? c2)
        {
            if (c1 is null) return c2 is null;
            return c1.Equals(c2);
        }
        public static bool operator !=(Calendar? c1, Calendar? c2) => !(c1 == c2);
        
        public bool Equals(Calendar? other) => other != null && name() == other.name();
    }

    /// <summary>
    /// TARGET calendar implementation.
    /// </summary>
    public class TARGET : Calendar
    {
        public override string name() => "TARGET";
        
        protected override bool isBusinessDayImpl(Date d)
        {
            return true; // Simplified implementation
        }
    }

    /// <summary>
    /// Defines behavior for classes that can perform extrapolation.
    /// </summary>
    public abstract class Extrapolator
    {
        private bool _extrapolationAllowed = false;

        public bool allowsExtrapolation() => _extrapolationAllowed;
        public void enableExtrapolation(bool b = true) => _extrapolationAllowed = b;
        public void disableExtrapolation(bool b = true) => _extrapolationAllowed = !b;
    }

    #endregion

    /// <summary>
    /// Basic term-structure functionality.
    /// Base class for interest-rate and volatility term structures.
    /// </summary>
    public abstract class TermStructure : Extrapolator, IObserver, IObservable
    {
        private readonly Observable _observable = new Observable();

        private readonly bool _moving;
        private bool _updated;

        private readonly Calendar _calendar;
        private Date? _referenceDate;
        private readonly Natural? _settlementDays;
        private readonly DayCounter _dayCounter;

        #region Constructors
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <remarks>
        /// Term structures initialized by means of this constructor must manage
        /// their own reference date by overriding the referenceDate() property.
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
            Settings.evaluationDate().RegisterWith(this);
        }
        #endregion

        #region Dates and Time
        /// <summary>
        /// The day counter used for date/time conversion.
        /// </summary>
        public DayCounter dayCounter() => _dayCounter;

        /// <summary>
        /// Date/time conversion.
        /// </summary>
        public double timeFromReference(Date date) => dayCounter().yearFraction(referenceDate(), date);

        /// <summary>
        /// The latest date for which the curve can return values.
        /// </summary>
        public abstract Date maxDate();

        /// <summary>
        /// The latest time for which the curve can return values.
        /// </summary>
        public virtual double maxTime() => timeFromReference(maxDate());

        /// <summary>
        /// The date at which discount = 1.0 and/or variance = 0.0.
        /// </summary>
        public virtual Date referenceDate()
        {
            if (!_updated)
            {
                Date today = Settings.EvaluationDate;
                _referenceDate = calendar().advance(today, (int)settlementDays(), TimeUnit.Days);
                _updated = true;
            }
            return _referenceDate ?? Date.Today; // Provide fallback if null
        }

        /// <summary>
        /// The calendar used for reference and/or option date calculation.
        /// </summary>
        public Calendar calendar() => _calendar;

        /// <summary>
        /// The settlement days used for reference date calculation.
        /// </summary>
        public Natural settlementDays()
        {
            if (!_settlementDays.HasValue)
                throw new InvalidOperationException("settlement days not provided for this instance");
            return _settlementDays.Value;
        }
        #endregion

        #region IObserver interface
        public virtual void update()
        {
            if (_moving)
                _updated = false;
            notifyObservers();
        }

        // For C++ compatibility
        public void Update() => update();
        #endregion

        #region IObservable interface
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void notifyObservers() => _observable.NotifyObservers();
        #endregion

        #region Range checks
        /// <summary>
        /// Date-range check.
        /// </summary>
        protected void checkRange(Date d, bool extrapolate)
        {
            if (d < referenceDate())
                throw new ArgumentException($"date ({d}) before reference date ({referenceDate()})");
            
            if (!extrapolate && !allowsExtrapolation() && d > maxDate())
                throw new ArgumentException($"date ({d}) is past max curve date ({maxDate()})");
        }

        /// <summary>
        /// Time-range check.
        /// </summary>
        protected void checkRange(double t, bool extrapolate)
        {
            if (t < 0.0)
                throw new ArgumentException($"negative time ({t}) given");
                
            if (!extrapolate && !allowsExtrapolation() && 
                t > maxTime() && !Comparison.CloseEnough(t, maxTime()))
                throw new ArgumentException($"time ({t}) is past max curve time ({maxTime()})");
        }
        #endregion
    }
}