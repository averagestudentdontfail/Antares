// Blackconstantvolatility.cs

using System;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term.Volatility
{
    /// <summary>
    /// Constant Black volatility, with no time or strike dependence.
    /// </summary>
    /// <remarks>
    /// This class implements the BlackVolatilityTermStructure interface for a
    /// constant Black volatility.
    /// </remarks>
    public class BlackConstantVol : BlackVolatilityTermStructure
    {
        private readonly Handle<IQuote> _volatility;

        #region Constructors
        public BlackConstantVol(Date referenceDate, Calendar calendar, Volatility volatility, DayCounter dayCounter)
            : base(referenceDate, calendar, BusinessDayConvention.Following, dayCounter)
        {
            _volatility = new Handle<IQuote>(new SimpleQuote(volatility));
        }

        public BlackConstantVol(Date referenceDate, Calendar calendar, Handle<IQuote> volatility, DayCounter dayCounter)
            : base(referenceDate, calendar, BusinessDayConvention.Following, dayCounter)
        {
            _volatility = volatility;
            RegisterWith(_volatility);
        }

        public BlackConstantVol(int settlementDays, Calendar calendar, Volatility volatility, DayCounter dayCounter)
            : base(settlementDays, calendar, BusinessDayConvention.Following, dayCounter)
        {
            _volatility = new Handle<IQuote>(new SimpleQuote(volatility));
        }

        public BlackConstantVol(int settlementDays, Calendar calendar, Handle<IQuote> volatility, DayCounter dayCounter)
            : base(settlementDays, calendar, BusinessDayConvention.Following, dayCounter)
        {
            _volatility = volatility;
            RegisterWith(_volatility);
        }
        #endregion

        #region TermStructure and VolatilityTermStructure interface overrides
        public override Date MaxDate => Date.MaxDate;
        public override Rate MinStrike => double.MinValue;
        public override Rate MaxStrike => double.MaxValue;
        #endregion

        #region BlackVolatilityTermStructure implementation
        protected override Volatility BlackVolImpl(Time t, Real strike)
        {
            return _volatility.Value.Value;
        }
        #endregion

        #region Visitability
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<BlackConstantVol> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                base.Accept(v); // Fall back to BlackVolatilityTermStructure visitor
            }
        }
        #endregion
    }
}