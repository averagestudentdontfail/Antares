// Cashflow.cs

using System;
using System.Collections.Generic;
using System.Linq;
using QLNet.Time;

// A Leg is a sequence of cash flows.
global using Leg = System.Collections.Generic.IReadOnlyList<Antares.ICashFlow>;

namespace Antares
{
    #region Supporting Infrastructure (Normally in separate files)
    // This infrastructure is included to make the file self-contained and compilable.
    // In a real project, these would be in their own files.

    public interface IObserver { void Update(); }
    public interface IObservable { void RegisterWith(IObserver observer); void UnregisterWith(IObserver observer); }
    public interface IAcyclicVisitor { }
    public interface IVisitor<in T> : IAcyclicVisitor { void Visit(T element); }

    public class Observable : IObservable
    {
        private readonly List<IObserver> _observers = new List<IObserver>();
        public void RegisterWith(IObserver observer) { if (!_observers.Contains(observer)) _observers.Add(observer); }
        public void UnregisterWith(IObserver observer) => _observers.Remove(observer);
        public void NotifyObservers()
        {
            var observersCopy = new List<IObserver>(_observers);
            foreach (var observer in observersCopy) observer.Update();
        }
    }

    public abstract class LazyObject : IObserver, IObservable
    {
        private readonly Observable _observable = new Observable();
        protected bool _calculated;
        public virtual void Update() { _calculated = false; NotifyObservers(); }
        public virtual void Calculate() { if (!_calculated) { _calculated = true; PerformCalculations(); } }
        protected abstract void PerformCalculations();
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
    }

    public interface IEvent : IObservable
    {
        Date Date { get; }
        bool HasOccurred(Date refDate = null, bool? includeRefDate = null);
        void Accept(IAcyclicVisitor v);
    }

    // Placeholder for Settings class
    public static partial class Settings
    {
        public static bool? IncludeTodaysCashFlows { get; set; }
    }
    #endregion

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
        Date ExCouponDate { get; }

        /// <summary>
        /// Returns true if the cashflow is trading ex-coupon on the reference date.
        /// </summary>
        bool TradingExCoupon(Date refDate = null);
    }

    /// <summary>
    /// Abstract base class for cash flow implementations.
    /// </summary>
    public abstract class CashFlow : LazyObject, ICashFlow
    {
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
        public virtual Date ExCouponDate => null;

        /// <summary>
        /// Overloads Event.HasOccurred in order to take Settings.IncludeTodaysCashFlows into account.
        /// </summary>
        public virtual bool HasOccurred(Date refDate = null, bool? includeRefDate = null)
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
        public bool TradingExCoupon(Date refDate = null)
        {
            Date ecd = this.ExCouponDate;
            if (ecd == null) // A null Date is the equivalent of a default-constructed C++ Date
                return false;

            Date resolvedRefDate = refDate ?? Settings.EvaluationDate;

            return ecd <= resolvedRefDate;
        }

        /// <summary>
        /// Accept a visitor. This implementation tries to dispatch to a CashFlow visitor
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
    }

    /// <summary>
    /// Provides comparison logic for cash flows.
    /// Corresponds to the C++ struct 'earlier_than<CashFlow>'.
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