// Term.cs

using System;
using System.Collections.Generic;

namespace Antares
{
    /// <summary>
    /// TARGET calendar implementation.
    /// </summary>
    public class TARGET : Antares.Time.Calendar
    {
        public override string Name => "TARGET";
        
        protected override bool IsBusinessDayImpl(Antares.Time.Date d)
        {
            return true; // Simplified implementation - TARGET has minimal holidays
        }

        public override bool IsWeekend(Antares.Time.Weekday w)
        {
            return w == Antares.Time.Weekday.Saturday || w == Antares.Time.Weekday.Sunday;
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

    /// <summary>
    /// Basic term-structure functionality.
    /// Base class for interest-rate and volatility term structures.
    /// </summary>
    public abstract class TermStructure : Extrapolator, IObserver, IObservable
    {
        private readonly Observable _observable = new Observable();

        private readonly bool _moving;
        private bool _updated;

        private readonly Antares.Time.Calendar _calendar;
        private Antares.Time.Date? _referenceDate;
        private readonly Natural? _settlementDays;
        private readonly DayCounter _dayCounter;

        #region Constructors
        protected TermStructure(DayCounter? dayCounter = null)
        {
            _dayCounter = dayCounter ?? new Actual365Fixed();
            _settlementDays = null;
            _updated = true;
            _moving = false;
            _calendar = new TARGET();
        }

        protected TermStructure(Antares.Time.Date referenceDate, Antares.Time.Calendar? calendar = null, DayCounter? dayCounter = null)
        {
            _calendar = calendar ?? new TARGET();
            _dayCounter = dayCounter ?? new Actual365Fixed();
            _referenceDate = referenceDate;
            _settlementDays = null;
            _updated = true;
            _moving = false;
        }

        protected TermStructure(Natural settlementDays, Antares.Time.Calendar calendar, DayCounter? dayCounter = null)
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
        public DayCounter dayCounter() => _dayCounter;
        public double timeFromReference(Antares.Time.Date date) => dayCounter().YearFraction(referenceDate(), date);
        public abstract Antares.Time.Date maxDate();
        public virtual double maxTime() => timeFromReference(maxDate());

        public virtual Antares.Time.Date referenceDate()
        {
            if (!_updated)
            {
                Antares.Time.Date today = Settings.EvaluationDate;
                _referenceDate = calendar().Advance(today, (int)settlementDays(), Antares.Time.TimeUnit.Days);
                _updated = true;
            }
            return _referenceDate ?? new Antares.Time.Date();
        }

        public Antares.Time.Calendar calendar() => _calendar;

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

        public void Update() => update();
        #endregion

        #region IObservable interface
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void notifyObservers() => _observable.NotifyObservers();
        #endregion

        #region Range checks
        protected void checkRange(Antares.Time.Date d, bool extrapolate)
        {
            if (d < referenceDate())
                throw new ArgumentException($"date ({d}) before reference date ({referenceDate()})");
            
            if (!extrapolate && !allowsExtrapolation() && d > maxDate())
                throw new ArgumentException($"date ({d}) is past max curve date ({maxDate()})");
        }

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