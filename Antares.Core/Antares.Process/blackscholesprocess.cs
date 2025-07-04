// Blackscholesprocess.cs

using System;
using System.Collections.Generic;
using Antares.Pattern;
using Antares.Quote;
using Antares.Term;
using Antares.Term.Volatility;
using Antares.Term.Yield;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Process
{
    /// <summary>
    /// Generalized Black-Scholes stochastic process.
    /// </summary>
    /// <remarks>
    /// This class describes the stochastic process S governed by
    /// d(ln S(t)) = (r(t) - q(t) - sigma(t, S)^2 / 2) dt + sigma * dW_t.
    /// While the interface is expressed in terms of S, the internal calculations work on ln(S).
    /// </remarks>
    public class GeneralizedBlackScholesProcess : StochasticProcess1D
    {
        private readonly Handle<IQuote> _x0;
        private readonly Handle<YieldTermStructure> _riskFreeRate;
        private readonly Handle<YieldTermStructure> _dividendYield;
        private readonly Handle<BlackVolTermStructure> _blackVolatility;
        private readonly Handle<LocalVolTermStructure> _externalLocalVolTS;

        private readonly bool _forceDiscretization;
        private readonly bool _hasExternalLocalVol;

        private readonly RelinkableHandle<LocalVolTermStructure> _localVolatility = new RelinkableHandle<LocalVolTermStructure>();
        private bool _updated;
        private bool _isStrikeIndependent;

        #region Constructors
        public GeneralizedBlackScholesProcess(
            Handle<IQuote> x0,
            Handle<YieldTermStructure> dividendTS,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            IStochasticProcess1DDiscretization d = null,
            bool forceDiscretization = false)
            : base(d ?? new EulerDiscretization())
        {
            _x0 = x0;
            _riskFreeRate = riskFreeTS;
            _dividendYield = dividendTS;
            _blackVolatility = blackVolTS;
            _forceDiscretization = forceDiscretization;
            _hasExternalLocalVol = false;
            _updated = false;
            _isStrikeIndependent = false;

            RegisterWith(_x0);
            RegisterWith(_riskFreeRate);
            RegisterWith(_dividendYield);
            RegisterWith(_blackVolatility);
        }

        public GeneralizedBlackScholesProcess(
            Handle<IQuote> x0,
            Handle<YieldTermStructure> dividendTS,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            Handle<LocalVolTermStructure> localVolTS)
            : base(new EulerDiscretization())
        {
            _x0 = x0;
            _riskFreeRate = riskFreeTS;
            _dividendYield = dividendTS;
            _blackVolatility = blackVolTS;
            _externalLocalVolTS = localVolTS;
            _forceDiscretization = false;
            _hasExternalLocalVol = true;
            _updated = false;
            _isStrikeIndependent = false;

            RegisterWith(_x0);
            RegisterWith(_riskFreeRate);
            RegisterWith(_dividendYield);
            RegisterWith(_blackVolatility);
            RegisterWith(_externalLocalVolTS);
        }
        #endregion

        #region StochasticProcess1D interface
        public override Real X0 => _x0.Value.Value;

        public override Real Drift(Time t, Real x)
        {
            Real sigma = Diffusion(t, x);
            Time t1 = t + 0.0001;
            return _riskFreeRate.Value.ForwardRate(t, t1, Compounding.Continuous, Frequency.NoFrequency, true)
                 - _dividendYield.Value.ForwardRate(t, t1, Compounding.Continuous, Frequency.NoFrequency, true)
                 - 0.5 * sigma * sigma;
        }

        public override Real Diffusion(Time t, Real x)
        {
            return LocalVolatility.Value.LocalVol(t, x, true);
        }

        public override Real Apply(Real x0, Real dx) => x0 * Math.Exp(dx);

        public override Real Expectation(Time t0, Real x0, Time dt)
        {
            // Trigger update of local volatility term structure
            var _ = LocalVolatility;
            if (_isStrikeIndependent && !_forceDiscretization)
            {
                return x0 * Math.Exp(dt * (_riskFreeRate.Value.ForwardRate(t0, t0 + dt, Compounding.Continuous, Frequency.NoFrequency, true)
                                          - _dividendYield.Value.ForwardRate(t0, t0 + dt, Compounding.Continuous, Frequency.NoFrequency, true)));
            }
            
            // Fallback to base class for discretization
            return base.Expectation(t0, x0, dt);
        }

        public override Real StdDeviation(Time t0, Real x0, Time dt)
        {
            var _ = LocalVolatility;
            if (_isStrikeIndependent && !_forceDiscretization)
            {
                return Math.Sqrt(Variance(t0, x0, dt));
            }
            return base.StdDeviation(t0, x0, dt);
        }

        public override Real Variance(Time t0, Real x0, Time dt)
        {
            var _ = LocalVolatility;
            if (_isStrikeIndependent && !_forceDiscretization)
            {
                return _blackVolatility.Value.BlackVariance(t0 + dt, 0.01, true) -
                       _blackVolatility.Value.BlackVariance(t0, 0.01, true);
            }
            return base.Variance(t0, x0, dt);
        }

        public override Real Evolve(Time t0, Real x0, Time dt, Real dw)
        {
            var _ = LocalVolatility;
            if (_isStrikeIndependent && !_forceDiscretization)
            {
                Real var = Variance(t0, x0, dt);
                Real drift = (_riskFreeRate.Value.ForwardRate(t0, t0 + dt, Compounding.Continuous, Frequency.NoFrequency, true) -
                              _dividendYield.Value.ForwardRate(t0, t0 + dt, Compounding.Continuous, Frequency.NoFrequency, true)) * dt - 0.5 * var;
                return Apply(x0, Math.Sqrt(var) * dw + drift);
            }
            return base.Evolve(t0, x0, dt, dw);
        }

        public override Time Time(Date d)
        {
            return _riskFreeRate.Value.DayCounter.yearFraction(_riskFreeRate.Value.ReferenceDate, d);
        }
        #endregion

        #region Observer interface
        public override void Update()
        {
            _updated = false;
            base.Update();
        }
        #endregion

        #region Inspectors
        public Handle<IQuote> StateVariable => _x0;
        public Handle<YieldTermStructure> DividendYield => _dividendYield;
        public Handle<YieldTermStructure> RiskFreeRate => _riskFreeRate;
        public Handle<BlackVolTermStructure> BlackVolatility => _blackVolatility;
        
        public Handle<LocalVolTermStructure> LocalVolatility
        {
            get
            {
                if (_hasExternalLocalVol)
                    return _externalLocalVolTS;

                if (!_updated)
                {
                    _isStrikeIndependent = true;

                    var blackVol = _blackVolatility.Value;
                    
                    if (blackVol is BlackConstantVol constVol)
                    {
                        _localVolatility.LinkTo(new LocalConstantVol(
                            constVol.ReferenceDate,
                            constVol.BlackVol(0.0, _x0.Value.Value),
                            constVol.DayCounter));
                    }
                    else if (blackVol is BlackVarianceCurve volCurve)
                    {
                        // BlackVarianceCurve is already strike-independent by definition
                        _localVolatility.LinkTo(new LocalVolCurve(new Handle<BlackVarianceCurve>(volCurve)));
                    }
                    else
                    {
                        // Fallback to a full surface
                        _localVolatility.LinkTo(new LocalVolSurface(
                            _blackVolatility, _riskFreeRate, _dividendYield, _x0));
                        _isStrikeIndependent = false;
                    }
                    _updated = true;
                }
                return _localVolatility;
            }
        }
        #endregion
    }

    /// <summary>
    /// Black-Scholes (1973) stochastic process (no dividend yield).
    /// </summary>
    public class BlackScholesProcess : GeneralizedBlackScholesProcess
    {
        public BlackScholesProcess(
            Handle<IQuote> x0,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            IStochasticProcess1DDiscretization d = null,
            bool forceDiscretization = false)
            : base(x0,
                   new Handle<YieldTermStructure>(new FlatForward(0, new NullCalendar(), 0.0, new Actual365Fixed())),
                   riskFreeTS,
                   blackVolTS,
                   d, forceDiscretization) { }
    }

    /// <summary>
    /// Merton (1973) extension to the Black-Scholes process with continuous dividend yield.
    /// </summary>
    public class BlackScholesMertonProcess : GeneralizedBlackScholesProcess
    {
        public BlackScholesMertonProcess(
            Handle<IQuote> x0,
            Handle<YieldTermStructure> dividendTS,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            IStochasticProcess1DDiscretization d = null,
            bool forceDiscretization = false)
            : base(x0, dividendTS, riskFreeTS, blackVolTS, d, forceDiscretization) { }
    }

    /// <summary>
    /// Black (1976) stochastic process for a forward or futures contract.
    /// </summary>
    public class BlackProcess : GeneralizedBlackScholesProcess
    {
        public BlackProcess(
            Handle<IQuote> x0,
            Handle<YieldTermStructure> riskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            IStochasticProcess1DDiscretization d = null,
            bool forceDiscretization = false)
            : base(x0, riskFreeTS, riskFreeTS, blackVolTS, d, forceDiscretization) { }
    }

    /// <summary>
    /// Garman-Kohlhagen (1983) stochastic process for an exchange rate.
    /// </summary>
    public class GarmanKohlagenProcess : GeneralizedBlackScholesProcess
    {
        public GarmanKohlagenProcess(
            Handle<IQuote> x0,
            Handle<YieldTermStructure> foreignRiskFreeTS,
            Handle<YieldTermStructure> domesticRiskFreeTS,
            Handle<BlackVolTermStructure> blackVolTS,
            IStochasticProcess1DDiscretization d = null,
            bool forceDiscretization = false)
            : base(x0, foreignRiskFreeTS, domesticRiskFreeTS, blackVolTS, d, forceDiscretization) { }
    }
}