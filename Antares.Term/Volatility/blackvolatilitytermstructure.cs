// Blackvolatilitytermstructure.cs

using System;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term.Volatility
{
    /// <summary>
    /// Black-volatility term structure.
    /// </summary>
    /// <remarks>
    /// This abstract class defines the interface of concrete Black-volatility term structures.
    /// Volatilities are assumed to be expressed on an annual basis.
    /// Derived classes must implement at least one of `BlackVolImpl` or `BlackVarianceImpl`.
    /// </remarks>
    public abstract class BlackVolTermStructure : VolatilityTermStructure
    {
        #region Constructors
        protected BlackVolTermStructure(
            BusinessDayConvention bdc = BusinessDayConvention.Following,
            DayCounter dc = null)
            : base(bdc, dc) { }

        protected BlackVolTermStructure(
            Date referenceDate,
            Calendar cal = null,
            BusinessDayConvention bdc = BusinessDayConvention.Following,
            DayCounter dc = null)
            : base(referenceDate, cal, bdc, dc) { }

        protected BlackVolTermStructure(
            int settlementDays,
            Calendar calendar,
            BusinessDayConvention bdc = BusinessDayConvention.Following,
            DayCounter dc = null)
            : base(settlementDays, calendar, bdc, dc) { }
        #endregion

        #region Black Volatility and Variance
        /// <summary>
        /// Spot volatility.
        /// </summary>
        public Volatility BlackVol(Date maturity, Real strike, bool extrapolate = false)
        {
            CheckRange(maturity, extrapolate);
            CheckStrike(strike, extrapolate);
            Time t = TimeFromReference(maturity);
            return BlackVolImpl(t, strike);
        }

        /// <summary>
        /// Spot volatility.
        /// </summary>
        public Volatility BlackVol(Time maturity, Real strike, bool extrapolate = false)
        {
            CheckRange(maturity, extrapolate);
            CheckStrike(strike, extrapolate);
            return BlackVolImpl(maturity, strike);
        }

        /// <summary>
        /// Spot variance.
        /// </summary>
        public Real BlackVariance(Date maturity, Real strike, bool extrapolate = false)
        {
            CheckRange(maturity, extrapolate);
            CheckStrike(strike, extrapolate);
            Time t = TimeFromReference(maturity);
            return BlackVarianceImpl(t, strike);
        }

        /// <summary>
        /// Spot variance.
        /// </summary>
        public Real BlackVariance(Time maturity, Real strike, bool extrapolate = false)
        {
            CheckRange(maturity, extrapolate);
            CheckStrike(strike, extrapolate);
            return BlackVarianceImpl(maturity, strike);
        }

        /// <summary>
        /// Forward (at-the-money) volatility.
        /// </summary>
        public Volatility BlackForwardVol(Date date1, Date date2, Real strike, bool extrapolate = false)
        {
            QL.Require(date1 <= date2, $"{date1} later than {date2}");
            CheckRange(date2, extrapolate);
            Time time1 = TimeFromReference(date1);
            Time time2 = TimeFromReference(date2);
            return BlackForwardVol(time1, time2, strike, extrapolate);
        }

        /// <summary>
        /// Forward (at-the-money) volatility.
        /// </summary>
        public Volatility BlackForwardVol(Time time1, Time time2, Real strike, bool extrapolate = false)
        {
            QL.Require(time1 <= time2, $"{time1} later than {time2}");
            CheckRange(time2, extrapolate);
            CheckStrike(strike, extrapolate);

            if (Math.Abs(time2 - time1) < 1e-12)
            {
                if (Math.Abs(time1) < 1.0e-12)
                {
                    Time epsilon = 1.0e-5;
                    Real var = BlackVarianceImpl(epsilon, strike);
                    return Math.Sqrt(var / epsilon);
                }
                else
                {
                    Time epsilon = Math.Min(1.0e-5, time1);
                    Real var1 = BlackVarianceImpl(time1 - epsilon, strike);
                    Real var2 = BlackVarianceImpl(time1 + epsilon, strike);
                    QL.Ensure(var2 >= var1, "variances must be non-decreasing");
                    return Math.Sqrt((var2 - var1) / (2 * epsilon));
                }
            }
            else
            {
                Real var1 = BlackVarianceImpl(time1, strike);
                Real var2 = BlackVarianceImpl(time2, strike);
                QL.Ensure(var2 >= var1, "variances must be non-decreasing");
                return Math.Sqrt((var2 - var1) / (time2 - time1));
            }
        }

        /// <summary>
        /// Forward (at-the-money) variance.
        /// </summary>
        public Real BlackForwardVariance(Date date1, Date date2, Real strike, bool extrapolate = false)
        {
            QL.Require(date1 <= date2, $"{date1} later than {date2}");
            CheckRange(date2, extrapolate);
            Time time1 = TimeFromReference(date1);
            Time time2 = TimeFromReference(date2);
            return BlackForwardVariance(time1, time2, strike, extrapolate);
        }

        /// <summary>
        /// Forward (at-the-money) variance.
        /// </summary>
        public Real BlackForwardVariance(Time time1, Time time2, Real strike, bool extrapolate = false)
        {
            QL.Require(time1 <= time2, $"{time1} later than {time2}");
            CheckRange(time2, extrapolate);
            CheckStrike(strike, extrapolate);
            Real v1 = BlackVarianceImpl(time1, strike);
            Real v2 = BlackVarianceImpl(time2, strike);
            QL.Ensure(v2 >= v1, "variances must be non-decreasing");
            return v2 - v1;
        }
        #endregion

        #region Visitability
        public virtual void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<BlackVolTermStructure> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                QL.Fail("not a Black-volatility term structure visitor");
            }
        }
        #endregion

        #region Calculations
        /// <summary>
        /// Must be implemented in derived classes for Black variance calculation.
        /// </summary>
        protected abstract Real BlackVarianceImpl(Time t, Real strike);

        /// <summary>
        /// Must be implemented in derived classes for Black volatility calculation.
        /// </summary>
        protected abstract Volatility BlackVolImpl(Time t, Real strike);
        #endregion
    }

    /// <summary>
    /// Adapter to `BlackVolTermStructure` that allows implementing only `BlackVolImpl`.
    /// </summary>
    public abstract class BlackVolatilityTermStructure : BlackVolTermStructure
    {
        protected BlackVolatilityTermStructure(BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null) : base(bdc, dc) { }
        protected BlackVolatilityTermStructure(Date referenceDate, Calendar cal = null, BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null) : base(referenceDate, cal, bdc, dc) { }
        protected BlackVolatilityTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null) : base(settlementDays, cal, bdc, dc) { }

        protected override sealed Real BlackVarianceImpl(Time maturity, Real strike)
        {
            Volatility vol = BlackVolImpl(maturity, strike);
            return vol * vol * maturity;
        }

        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<BlackVolatilityTermStructure> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                base.Accept(v);
            }
        }
    }

    /// <summary>
    /// Adapter to `BlackVolTermStructure` that allows implementing only `BlackVarianceImpl`.
    /// </summary>
    public abstract class BlackVarianceTermStructure : BlackVolTermStructure
    {
        protected BlackVarianceTermStructure(BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null) : base(bdc, dc) { }
        protected BlackVarianceTermStructure(Date referenceDate, Calendar cal = null, BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null) : base(referenceDate, cal, bdc, dc) { }
        protected BlackVarianceTermStructure(int settlementDays, Calendar calendar, BusinessDayConvention bdc = BusinessDayConvention.Following, DayCounter dc = null) : base(settlementDays, calendar, bdc, dc) { }

        protected override sealed Volatility BlackVolImpl(Time t, Real strike)
        {
            Time nonZeroMaturity = (Math.Abs(t) < 1.0e-12 ? 0.00001 : t);
            Real var = BlackVarianceImpl(nonZeroMaturity, strike);
            return Math.Sqrt(var / nonZeroMaturity);
        }

        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<BlackVarianceTermStructure> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                base.Accept(v);
            }
        }
    }
}