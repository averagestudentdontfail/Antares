// Zeroyieldstructure.cs

using System;
using System.Collections.Generic;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term.Yield
{
    /// <summary>
    /// Zero-yield term structure.
    /// </summary>
    /// <remarks>
    /// This abstract class acts as an adapter to YieldTermStructure,
    /// allowing the programmer to implement only the `ZeroYieldImpl(Time)`
    /// method in derived classes.
    ///
    /// Discount and forward rates are calculated from zero yields.
    /// Zero rates are assumed to be annual continuous compounding.
    /// </remarks>
    public abstract class ZeroYieldStructure : YieldTermStructure
    {
        #region Constructors
        /// <summary>
        /// See the TermStructure documentation for issues regarding constructors.
        /// </summary>
        protected ZeroYieldStructure(DayCounter dc = null)
            : base(dc) { }

        protected ZeroYieldStructure(
            Date referenceDate,
            Calendar calendar = null,
            DayCounter dc = null,
            List<Handle<IQuote>> jumps = null,
            List<Date> jumpDates = null)
            : base(referenceDate, calendar, dc, jumps, jumpDates) { }

        protected ZeroYieldStructure(
            int settlementDays,
            Calendar calendar,
            DayCounter dc = null,
            List<Handle<IQuote>> jumps = null,
            List<Date> jumpDates = null)
            : base(settlementDays, calendar, dc, jumps, jumpDates) { }
        #endregion

        #region Calculations
        /// <summary>
        /// This method must be implemented in derived classes to perform the actual calculations.
        /// When it is called, range check has already been performed; therefore, it
        /// must assume that extrapolation is required.
        /// </summary>
        /// <param name="t">The time for which the zero yield is to be calculated.</param>
        /// <returns>The zero-yield rate.</returns>
        protected abstract Rate ZeroYieldImpl(Time t);
        #endregion

        #region YieldTermStructure implementation
        /// <summary>
        /// Returns the discount factor for the given date, calculating it from the zero yield.
        /// </summary>
        /// <param name="t">The time for which the discount factor is to be calculated.</param>
        /// <returns>The discount factor.</returns>
        protected override DiscountFactor DiscountImpl(Time t)
        {
            // This acts as a safeguard in cases where ZeroYieldImpl(0.0) would throw.
            if (Math.Abs(t) < 1.0e-12)
                return 1.0;

            Rate r = ZeroYieldImpl(t);
            return Math.Exp(-r * t);
        }
        #endregion
    }
}