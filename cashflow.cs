// Cashflow.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Time;
using Antares.Pattern;

// A Leg is a sequence of cash flows.
global using Leg = System.Collections.Generic.IReadOnlyList<Antares.ICashFlow>;

namespace Antares
{
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