// Volatilitytermstructure.cs

using System;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term
{
    /// <summary>
    /// Volatility term structure.
    /// This abstract class defines the interface of concrete volatility structures.
    /// </summary>
    public abstract class VolatilityTermStructure : TermStructure
    {
        private readonly BusinessDayConvention _businessDayConvention;

        #region Constructors
        /// <summary>
        /// Term structures initialized by means of this constructor must manage their own
        /// reference date by overriding the ReferenceDate property.
        /// </summary>
        protected VolatilityTermStructure(BusinessDayConvention bdc, DayCounter dc = null)
            : base(dc)
        {
            _businessDayConvention = bdc;
        }

        /// <summary>
        /// Initialize with a fixed reference date.
        /// </summary>
        protected VolatilityTermStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null)
            : base(referenceDate, cal, dc)
        {
            _businessDayConvention = bdc;
        }

        /// <summary>
        /// Calculate the reference date based on the global evaluation date.
        /// </summary>
        protected VolatilityTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null)
            : base((uint)settlementDays, cal, dc)
        {
            _businessDayConvention = bdc;
        }
        #endregion

        /// <summary>
        /// The business day convention used in tenor to date conversion.
        /// </summary>
        public virtual BusinessDayConvention BusinessDayConvention => _businessDayConvention;

        /// <summary>
        /// The minimum strike for which the term structure can return vols.
        /// </summary>
        public abstract Rate MinStrike { get; }

        /// <summary>
        /// The maximum strike for which the term structure can return vols.
        /// </summary>
        public abstract Rate MaxStrike { get; }

        /// <summary>
        /// Converts a period to a date based on the calendar and business day convention.
        /// </summary>
        public Date OptionDateFromTenor(Period p)
        {
            // swaption style
            return Calendar.Advance(ReferenceDate, p, BusinessDayConvention);
        }

        /// <summary>
        /// Performs a check on the strike value to ensure it is within the valid domain.
        /// </summary>
        /// <param name="strike">The strike to check.</param>
        /// <param name="extrapolate">Whether extrapolation is allowed.</param>
        protected void CheckStrike(Rate strike, bool extrapolate)
        {
            QL.Require(extrapolate || AllowsExtrapolation || (strike >= MinStrike && strike <= MaxStrike),
                       $"strike ({strike}) is outside the curve domain [{MinStrike},{MaxStrike}]");
        }
    }
}