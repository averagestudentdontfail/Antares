// Localvolatilitytermstructure.cs

using System;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term.Volatility
{
    /// <summary>
    /// Local-volatility term structure.
    /// This abstract class defines the interface of concrete local-volatility term structures.
    /// Volatilities are assumed to be expressed on an annual basis.
    /// </summary>
    public abstract class LocalVolTermStructure : VolatilityTermStructure
    {
        #region Constructors
        /// <summary>
        /// Term structures initialized by means of this constructor must manage their own
        /// reference date by overriding the ReferenceDate property.
        /// </summary>
        protected LocalVolTermStructure(
            BusinessDayConvention bdc = BusinessDayConvention.Following,
            DayCounter dc = null)
            : base(bdc, dc) { }

        /// <summary>
        /// Initialize with a fixed reference date.
        /// </summary>
        protected LocalVolTermStructure(
            Date referenceDate,
            Calendar cal = null,
            BusinessDayConvention bdc = BusinessDayConvention.Following,
            DayCounter dc = null)
            : base(referenceDate, cal, bdc, dc) { }

        /// <summary>
        /// Calculate the reference date based on the global evaluation date.
        /// </summary>
        protected LocalVolTermStructure(
            int settlementDays,
            Calendar calendar,
            BusinessDayConvention bdc = BusinessDayConvention.Following,
            DayCounter dc = null)
            : base(settlementDays, calendar, bdc, dc) { }
        #endregion

        #region Local Volatility
        /// <summary>
        /// Calculates the local volatility for a given date and underlying level.
        /// </summary>
        public Volatility LocalVol(Date d, Real underlyingLevel, bool extrapolate = false)
        {
            CheckRange(d, extrapolate);
            CheckStrike(underlyingLevel, extrapolate);
            Time t = TimeFromReference(d);
            return LocalVolImpl(t, underlyingLevel);
        }

        /// <summary>
        /// Calculates the local volatility for a given time and underlying level.
        /// </summary>
        public Volatility LocalVol(Time t, Real underlyingLevel, bool extrapolate = false)
        {
            CheckRange(t, extrapolate);
            CheckStrike(underlyingLevel, extrapolate);
            return LocalVolImpl(t, underlyingLevel);
        }
        #endregion

        #region Visitability
        /// <summary>
        /// Accepts a visitor.
        /// </summary>
        public virtual void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<LocalVolTermStructure> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                QL.Fail("not a local-volatility term structure visitor");
            }
        }
        #endregion

        #region Calculations
        /// <summary>
        /// This method must be implemented in derived classes to perform the actual volatility calculations.
        /// When it is called, range check has already been performed; therefore, it must
        /// assume that extrapolation is required.
        /// </summary>
        protected abstract Volatility LocalVolImpl(Time t, Real strike);
        #endregion
    }
}