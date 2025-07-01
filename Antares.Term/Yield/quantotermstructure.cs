// Quantotermstructure.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term.Yield
{
    /// <summary>
    /// Quanto term structure.
    /// </summary>
    /// <remarks>
    /// Quanto term structure for modelling quanto effect in option pricing.
    /// This term structure will remain linked to the original structures, i.e.,
    /// any changes in the latter will be reflected in this structure as well.
    /// </remarks>
    public class QuantoTermStructure : ZeroYieldStructure
    {
        private readonly Handle<YieldTermStructure> _underlyingDividendTS;
        private readonly Handle<YieldTermStructure> _riskFreeTS;
        private readonly Handle<YieldTermStructure> _foreignRiskFreeTS;
        private readonly Handle<BlackVolTermStructure> _underlyingBlackVolTS;
        private readonly Handle<BlackVolTermStructure> _exchRateBlackVolTS;
        private readonly Real _underlyingExchRateCorrelation;
        private readonly Real _strike;
        private readonly Real _exchRateATMlevel;

        public QuantoTermStructure(
            Handle<YieldTermStructure> underlyingDividendTS,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<YieldTermStructure> foreignRiskFreeTS,
            Handle<BlackVolTermStructure> underlyingBlackVolTS,
            Real strike,
            Handle<BlackVolTermStructure> exchRateBlackVolTS,
            Real exchRateATMlevel,
            Real underlyingExchRateCorrelation)
            : base(underlyingDividendTS.Value.DayCounter)
        {
            _underlyingDividendTS = underlyingDividendTS;
            _riskFreeTS = riskFreeTS;
            _foreignRiskFreeTS = foreignRiskFreeTS;
            _underlyingBlackVolTS = underlyingBlackVolTS;
            _exchRateBlackVolTS = exchRateBlackVolTS;
            _underlyingExchRateCorrelation = underlyingExchRateCorrelation;
            _strike = strike;
            _exchRateATMlevel = exchRateATMlevel;

            RegisterWith(_underlyingDividendTS);
            RegisterWith(_riskFreeTS);
            RegisterWith(_foreignRiskFreeTS);
            RegisterWith(_underlyingBlackVolTS);
            RegisterWith(_exchRateBlackVolTS);
        }

        #region TermStructure interface
        public override DayCounter DayCounter => _underlyingDividendTS.Value.DayCounter;
        public override Calendar Calendar => _underlyingDividendTS.Value.Calendar;
        public override uint SettlementDays => _underlyingDividendTS.Value.SettlementDays;
        public override Date ReferenceDate => _underlyingDividendTS.Value.ReferenceDate;

        public override Date MaxDate
        {
            get
            {
                var dates = new List<Date>
                {
                    _underlyingDividendTS.Value.MaxDate,
                    _riskFreeTS.Value.MaxDate,
                    _foreignRiskFreeTS.Value.MaxDate,
                    _underlyingBlackVolTS.Value.MaxDate,
                    _exchRateBlackVolTS.Value.MaxDate
                };
                return dates.Min();
            }
        }
        #endregion

        #region ZeroYieldStructure implementation
        protected override Rate ZeroYieldImpl(Time t)
        {
            // Warning: here it is assumed that all TS have the same daycount.
            //          It should be QL.Require'd
            return _underlyingDividendTS.Value.ZeroRate(t, Compounding.Continuous, Frequency.NoFrequency, true)
                 + _riskFreeTS.Value.ZeroRate(t, Compounding.Continuous, Frequency.NoFrequency, true)
                 - _foreignRiskFreeTS.Value.ZeroRate(t, Compounding.Continuous, Frequency.NoFrequency, true)
                 + _underlyingExchRateCorrelation
                 * _underlyingBlackVolTS.Value.BlackVol(t, _strike, true)
                 * _exchRateBlackVolTS.Value.BlackVol(t, _exchRateATMlevel, true);
        }
        #endregion
    }
}