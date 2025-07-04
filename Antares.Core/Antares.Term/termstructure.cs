// TermStructure.cs

using System;
using System.Collections.Generic;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term
{
    #region Missing Type Definitions
    
    using Antares.Time;

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
        private Date? _referenceDate;
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
                return _referenceDate ?? Date.Today; // Provide fallback if null
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