// Cashflow.cs

// A Leg is a sequence of cash flows.
global using Leg = System.Collections.Generic.IReadOnlyList<Antares.ICashFlow>;

using System;
using System.Collections.Generic;
using System.Linq;

namespace Antares
{
    #region Core Infrastructure Types

    /// <summary>
    /// Represents a date.
    /// </summary>
    public class Date : IComparable<Date>
    {
        private readonly DateTime _dateTime;

        public Date() { _dateTime = DateTime.Today; }
        public Date(DateTime dateTime) { _dateTime = dateTime.Date; }
        public Date(int year, int month, int day) { _dateTime = new DateTime(year, month, day); }

        public static Date Today => new Date(DateTime.Today);
        public static Date MinDate => new Date(DateTime.MinValue);
        public static Date MaxDate => new Date(DateTime.MaxValue);

        public int CompareTo(Date? other) => other == null ? 1 : _dateTime.CompareTo(other._dateTime);
        public static bool operator <(Date? left, Date? right) => left?.CompareTo(right) < 0;
        public static bool operator >(Date? left, Date? right) => left?.CompareTo(right) > 0;
        public static bool operator <=(Date? left, Date? right) => left?.CompareTo(right) <= 0;
        public static bool operator >=(Date? left, Date? right) => left?.CompareTo(right) >= 0;
        public static bool operator ==(Date? left, Date? right) => left?.CompareTo(right) == 0;
        public static bool operator !=(Date? left, Date? right) => !(left == right);

        public override bool Equals(object? obj) => obj is Date other && this == other;
        public override int GetHashCode() => _dateTime.GetHashCode();
        public override string ToString() => _dateTime.ToString("yyyy-MM-dd");

        public static Date minDate() => MinDate;
    }

    /// <summary>
    /// Observer interface for the observer pattern.
    /// </summary>
    public interface IObserver
    {
        void Update();
    }

    /// <summary>
    /// Observable interface for the observer pattern.
    /// </summary>
    public interface IObservable
    {
        void RegisterWith(IObserver observer);
        void UnregisterWith(IObserver observer);
    }

    /// <summary>
    /// Concrete implementation of IObservable to be used via composition.
    /// </summary>
    public class Observable : IObservable
    {
        private readonly List<IObserver> _observers = new List<IObserver>();

        public void RegisterWith(IObserver observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        public void UnregisterWith(IObserver observer)
        {
            _observers.Remove(observer);
        }

        public void NotifyObservers()
        {
            var observersCopy = new List<IObserver>(_observers);
            foreach (var observer in observersCopy)
            {
                observer.Update();
            }
        }
    }

    /// <summary>
    /// A degenerate base interface for the Acyclic Visitor pattern.
    /// </summary>
    public interface IAcyclicVisitor
    {
        // This interface is intentionally empty.
    }

    /// <summary>
    /// A generic visitor interface for a specific class in the Acyclic Visitor pattern.
    /// </summary>
    /// <typeparam name="T">The type of the object to be visited.</typeparam>
    public interface IVisitor<in T> : IAcyclicVisitor
    {
        void Visit(T element);
    }

    /// <summary>
    /// Framework for calculation on demand and result caching.
    /// </summary>
    public abstract class LazyObject : IObserver, IObservable
    {
        private readonly Observable _observable = new Observable();
        protected bool _calculated;
        protected bool _frozen;
        protected bool _alwaysForward;
        private bool _updating;

        protected LazyObject()
        {
            _calculated = false;
            _frozen = false;
            _updating = false;
            _alwaysForward = !Settings.FasterLazyObjects;
        }

        public bool IsCalculated => _calculated;

        public virtual void Update()
        {
            if (_frozen) return;
            if (_updating) return;

            bool wasCalculated = _calculated;
            _calculated = false;

            if (_alwaysForward || wasCalculated)
            {
                _updating = true;
                try
                {
                    NotifyObservers();
                }
                finally
                {
                    _updating = false;
                }
            }
        }

        public virtual void Calculate()
        {
            if (!_calculated && !_frozen)
            {
                _calculated = true;
                try
                {
                    PerformCalculations();
                }
                catch
                {
                    _calculated = false;
                    throw;
                }
            }
        }

        protected abstract void PerformCalculations();

        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
    }

    #endregion

    /// <summary>
    /// Base interface for events associated with a given date.
    /// </summary>
    public interface IEvent : IObservable
    {
        /// <summary>
        /// Returns the date at which the event occurs.
        /// </summary>
        Date Date { get; }

        /// <summary>
        /// Returns true if an event has already occurred before a given reference date.
        /// </summary>
        /// <param name="refDate">The reference date. If null, the global evaluation date is used.</param>
        /// <param name="includeRefDate">
        /// Specifies whether an event occurring on the reference date has occurred.
        /// If null, the global setting is used.
        /// If true, an event on the reference date has NOT occurred.
        /// If false, an event on the reference date HAS occurred.
        /// </param>
        bool HasOccurred(Date? refDate = null, bool? includeRefDate = null);

        /// <summary>
        /// Accepts a visitor.
        /// </summary>
        void Accept(IAcyclicVisitor v);
    }

    /// <summary>
    /// Base interface for cash flows.
    /// </summary>
    public interface ICashFlow : IEvent
    {
        /// <summary>
        /// Returns the amount of the cash flow.
        /// </summary>
        /// <remarks>The amount is not discounted, i.e., it is the actual amount paid at the cash flow date.</remarks>
        Real Amount { get; }

        /// <summary>
        /// Returns the date that the cash flow trades ex-coupon.
        /// </summary>
        Date? ExCouponDate { get; }

        /// <summary>
        /// Returns true if the cashflow is trading ex-coupon on the reference date.
        /// </summary>
        bool TradingExCoupon(Date? refDate = null);
    }

    /// <summary>
    /// Abstract base class for cash flow implementations.
    /// </summary>
    public abstract class CashFlow : LazyObject, ICashFlow
    {
        private readonly Observable _observable = new Observable();

        /// <summary>
        /// Returns the date at which the event occurs.
        /// </summary>
        public abstract Date Date { get; }

        /// <summary>
        /// Returns the amount of the cash flow.
        /// </summary>
        public abstract Real Amount { get; }

        /// <summary>
        /// Returns the date that the cash flow trades ex-coupon.
        /// By default, returns a null date.
        /// </summary>
        public virtual Date? ExCouponDate => null;

        /// <summary>
        /// Overloads Event.HasOccurred in order to take Settings.IncludeTodaysCashFlows into account.
        /// </summary>
        public virtual bool HasOccurred(Date? refDate = null, bool? includeRefDate = null)
        {
            Date cfDate = this.Date;

            // Easy and quick handling of most cases
            if (refDate != null)
            {
                if (refDate < cfDate)
                    return false;
                if (cfDate < refDate)
                    return true;
            }

            // If we're here, either refDate is null or refDate == cfDate.

            // Determine the reference date to use (either passed in or from global settings)
            Date resolvedRefDate = refDate ?? Settings.EvaluationDate;

            // Today's date; we override the bool with the one specified in the settings (if any)
            if (resolvedRefDate == Settings.EvaluationDate)
            {
                bool? includeToday = Settings.IncludeTodaysCashFlows;
                if (includeToday.HasValue)
                {
                    includeRefDate = includeToday;
                }
            }

            // Fall back to Event's logic with the potentially updated includeRefDate flag
            bool include = includeRefDate ?? Settings.IncludeReferenceDateEvents;
            if (include)
                return cfDate < resolvedRefDate;
            else
                return cfDate <= resolvedRefDate;
        }

        /// <summary>
        /// Returns true if the cashflow is trading ex-coupon on the reference date.
        /// </summary>
        public bool TradingExCoupon(Date? refDate = null)
        {
            Date? ecd = this.ExCouponDate;
            if (ecd == null) // A null Date is the equivalent of a default-constructed C++ Date
                return false;

            Date resolvedRefDate = refDate ?? Settings.EvaluationDate;

            return ecd <= resolvedRefDate;
        }

        /// <summary>
        /// Accept a visitor. This implementation tries to dispatch to a <see cref="ICashFlow"/> visitor
        /// first, and then falls back to an Event visitor.
        /// </summary>
        public virtual void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<ICashFlow> cashFlowVisitor)
            {
                cashFlowVisitor.Visit(this);
            }
            else if (v is IVisitor<IEvent> eventVisitor)
            {
                eventVisitor.Visit(this);
            }
            else
            {
                throw new ArgumentException("The provided object is not a valid cashflow visitor.", nameof(v));
            }
        }

        /// <summary>
        /// Empty implementation for the LazyObject interface.
        /// CashFlow itself doesn't perform calculations, but derived classes might.
        /// </summary>
        protected override void PerformCalculations() { }

        // IObservable implementation methods
        public new void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public new void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected new void NotifyObservers() => _observable.NotifyObservers();
    }

    /// <summary>
    /// Provides comparison logic for cash flows.
    /// Corresponds to the C++ struct 'earlier_than&lt;CashFlow&gt;'.
    /// </summary>
    public static class CashFlowComparer
    {
        /// <summary>
        /// Compares two cash flows based on their payment date.
        /// </summary>
        /// <returns>A negative value if c1 is earlier, positive if c2 is earlier, 0 if they are on the same date.</returns>
        public static int Compare(ICashFlow c1, ICashFlow c2)
        {
            return c1.Date.CompareTo(c2.Date);
        }
    }
}