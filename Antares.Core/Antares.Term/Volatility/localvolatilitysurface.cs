// Localvolatilitysurface.cs

using System;
using Antares.Pattern;
using Antares.Term.Yield;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term.Volatility
{
    /// <summary>
    /// Placeholder for BlackVolTermStructure's full interface.
    /// This will be replaced by a full port later.
    /// </summary>
    public abstract class BlackVolTermStructure : VolatilityTermStructure
    {
        protected BlackVolTermStructure(BusinessDayConvention bdc, DayCounter dc = null) : base(bdc, dc) { }
        protected BlackVolTermStructure(Date referenceDate, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null) : base(referenceDate, cal, bdc, dc) { }
        protected BlackVolTermStructure(int settlementDays, Calendar cal, BusinessDayConvention bdc, DayCounter dc = null) : base(settlementDays, cal, bdc, dc) { }

        public Volatility BlackVariance(Time t, Rate k, bool extrapolate = false)
        {
            Volatility vol = BlackVol(t, k, extrapolate);
            return vol * vol * t;
        }

        public abstract Volatility BlackVol(Time t, Rate k, bool extrapolate = false);
    }

    /// <summary>
    /// Local volatility surface derived from a Black vol surface.
    /// </summary>
    /// <remarks>
    /// For details about this implementation refer to
    /// "Stochastic Volatility and Local Volatility," in
    /// "Case Studies and Financial Modelling Course Notes," by
    /// Jim Gatheral, Fall Term, 2003.
    /// </remarks>
    public class LocalVolSurface : LocalVolTermStructure
    {
        private readonly Handle<BlackVolTermStructure> _blackTS;
        private readonly Handle<YieldTermStructure> _riskFreeTS;
        private readonly Handle<YieldTermStructure> _dividendTS;
        private readonly Handle<IQuote> _underlying;

        #region Constructors
        public LocalVolSurface(
            Handle<BlackVolTermStructure> blackTS,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<YieldTermStructure> dividendTS,
            Handle<IQuote> underlying)
            : base(blackTS.Value.BusinessDayConvention, blackTS.Value.DayCounter)
        {
            _blackTS = blackTS;
            _riskFreeTS = riskFreeTS;
            _dividendTS = dividendTS;
            _underlying = underlying;

            RegisterWith(_blackTS);
            RegisterWith(_riskFreeTS);
            RegisterWith(_dividendTS);
            RegisterWith(_underlying);
        }

        public LocalVolSurface(
            Handle<BlackVolTermStructure> blackTS,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<YieldTermStructure> dividendTS,
            Real underlying)
            : base(blackTS.Value.BusinessDayConvention, blackTS.Value.DayCounter)
        {
            _blackTS = blackTS;
            _riskFreeTS = riskFreeTS;
            _dividendTS = dividendTS;
            _underlying = new Handle<IQuote>(new SimpleQuote(underlying));

            RegisterWith(_blackTS);
            RegisterWith(_riskFreeTS);
            RegisterWith(_dividendTS);
        }
        #endregion

        #region TermStructure and VolatilityTermStructure interface overrides
        public override Date ReferenceDate => _blackTS.Value.ReferenceDate;
        public override DayCounter DayCounter => _blackTS.Value.DayCounter;
        public override Date MaxDate => _blackTS.Value.MaxDate;
        public override Rate MinStrike => _blackTS.Value.MinStrike;
        public override Rate MaxStrike => _blackTS.Value.MaxStrike;
        #endregion

        #region LocalVolTermStructure implementation
        protected override Volatility LocalVolImpl(Time t, Real underlyingLevel)
        {
            DiscountFactor dr = _riskFreeTS.Value.Discount(t, true);
            DiscountFactor dq = _dividendTS.Value.Discount(t, true);
            Real forwardValue = _underlying.Value.Value * dq / dr;

            // Strike derivatives
            Real strike = underlyingLevel;
            Real y = Math.Log(strike / forwardValue);
            Real dy = (Math.Abs(y) > 0.001) ? y * 0.0001 : 0.000001;
            Real strikep = strike * Math.Exp(dy);
            Real strikem = strike / Math.Exp(dy);
            Real w = _blackTS.Value.BlackVariance(t, strike, true);
            Real wp = _blackTS.Value.BlackVariance(t, strikep, true);
            Real wm = _blackTS.Value.BlackVariance(t, strikem, true);
            Real dwdy = (wp - wm) / (2.0 * dy);
            Real d2wdy2 = (wp - 2.0 * w + wm) / (dy * dy);

            // Time derivative
            Real dt, dwdt;
            if (Math.Abs(t) < 1.0e-12)
            {
                dt = 0.0001;
                DiscountFactor drpt = _riskFreeTS.Value.Discount(t + dt, true);
                DiscountFactor dqpt = _dividendTS.Value.Discount(t + dt, true);
                Real strikept = strike * dr * dqpt / (drpt * dq);

                Real wpt = _blackTS.Value.BlackVariance(t + dt, strikept, true);
                QL.Ensure(wpt >= w, $"decreasing variance at strike {strike} between time {t} and time {t + dt}");
                dwdt = (wpt - w) / dt;
            }
            else
            {
                dt = Math.Min(0.0001, t / 2.0);
                DiscountFactor drpt = _riskFreeTS.Value.Discount(t + dt, true);
                DiscountFactor drmt = _riskFreeTS.Value.Discount(t - dt, true);
                DiscountFactor dqpt = _dividendTS.Value.Discount(t + dt, true);
                DiscountFactor dqmt = _dividendTS.Value.Discount(t - dt, true);

                Real strikept = strike * dr * dqpt / (drpt * dq);
                Real strikemt = strike * dr * dqmt / (drmt * dq);

                Real wpt = _blackTS.Value.BlackVariance(t + dt, strikept, true);
                Real wmt = _blackTS.Value.BlackVariance(t - dt, strikemt, true);

                QL.Ensure(wpt >= w, $"decreasing variance at strike {strike} between time {t} and time {t + dt}");
                QL.Ensure(w >= wmt, $"decreasing variance at strike {strike} between time {t - dt} and time {t}");
                dwdt = (wpt - wmt) / (2.0 * dt);
            }

            if (Math.Abs(dwdy) < 1.0e-12 && Math.Abs(d2wdy2) < 1.0e-12)
            {
                // Avoid division by w where w might be 0.0
                return Math.Sqrt(dwdt);
            }
            else
            {
                Real den1 = 1.0 - y / w * dwdy;
                Real den2 = 0.25 * (-0.25 - 1.0 / w + y * y / w / w) * dwdy * dwdy;
                Real den3 = 0.5 * d2wdy2;
                Real den = den1 + den2 + den3;
                Real result = dwdt / den;

                QL.Ensure(result >= 0.0, $"negative local vol^2 at strike {strike} and time {t}; the black vol surface is not smooth enough");
                return Math.Sqrt(result);
            }
        }
        #endregion

        #region Visitability
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<LocalVolSurface> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                base.Accept(v); // Fall back to LocalVolTermStructure visitor
            }
        }
        #endregion
    }
}