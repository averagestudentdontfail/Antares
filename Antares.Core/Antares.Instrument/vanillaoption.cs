// VanillaOption.cs

using System;
using System.Collections.Generic;
using Antares.Cashflow;
using Antares.Pattern;
using Antares.Process;
using Antares.Quote;
using Antares.Term;
using Antares.Term.Volatility;
using Antares.Term.Yield;
using Antares.Time;
using Antares.Time.Day;
using MathNet.Numerics.RootFinding;

// Alias for a dividend schedule to match C++ code clarity.
using DividendSchedule = System.Collections.Generic.IReadOnlyList<Antares.Cashflow.IDividend>;

namespace Antares.Instrument
{
    /// <summary>
    /// Vanilla option on a single asset.
    /// </summary>
    public class VanillaOption : OneAssetOption
    {
        public VanillaOption(IStrikedTypePayoff payoff, IExercise exercise)
            : base(payoff, exercise) { }

        /// <summary>
        /// Calculates the implied volatility of the option.
        /// </summary>
        /// <param name="targetValue">The target option price to match.</param>
        /// <param name="process">The Black-Scholes process with zero volatility.</param>
        /// <param name="accuracy">Desired accuracy of the implied volatility.</param>
        /// <param name="maxEvaluations">Maximum number of function evaluations.</param>
        /// <param name="minVol">Minimum volatility boundary.</param>
        /// <param name="maxVol">Maximum volatility boundary.</param>
        /// <returns>The implied volatility.</returns>
        public Volatility ImpliedVolatility(Real targetValue,
                                          GeneralizedBlackScholesProcess process,
                                          Real accuracy = 1.0e-4,
                                          int maxEvaluations = 100,
                                          Volatility minVol = 1.0e-7,
                                          Volatility maxVol = 4.0)
        {
            QL.Require(!IsExpired, "option expired");

            var originalVol = process.BlackVolatility.Value.BlackVol(Exercise.LastDate, Payoff.Strike);
            
            Func<double, double> f = (vol) =>
            {
                var volQuote = new SimpleQuote(vol);
                var volHandle = new Handle<IQuote>(volQuote);
                var volTS = new BlackConstantVol(process.RiskFreeRate.Value.ReferenceDate,
                                               new NullCalendar(), volHandle,
                                               new Actual365Fixed());
                var volTSHandle = new Handle<BlackVolTermStructure>(volTS);

                var newProcess = new GeneralizedBlackScholesProcess(
                    process.StateVariable,
                    process.DividendYield,
                    process.RiskFreeRate,
                    volTSHandle);

                var engine = new AnalyticEuropeanEngine(newProcess);
                SetPricingEngine(engine);
                
                return NPV - targetValue;
            };

            try
            {
                return Brent.FindRoot(f, minVol, maxVol, accuracy, maxEvaluations);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to calculate implied volatility: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Calculates the Black-Scholes delta of the option.
        /// </summary>
        public Real Delta
        {
            get
            {
                Calculate();
                return Result<Real>("delta");
            }
        }

        /// <summary>
        /// Calculates the Black-Scholes gamma of the option.
        /// </summary>
        public Real Gamma
        {
            get
            {
                Calculate();
                return Result<Real>("gamma");
            }
        }

        /// <summary>
        /// Calculates the Black-Scholes theta of the option.
        /// </summary>
        public Real Theta
        {
            get
            {
                Calculate();
                return Result<Real>("theta");
            }
        }

        /// <summary>
        /// Calculates the Black-Scholes vega of the option.
        /// </summary>
        public Real Vega
        {
            get
            {
                Calculate();
                return Result<Real>("vega");
            }
        }

        /// <summary>
        /// Calculates the Black-Scholes rho of the option.
        /// </summary>
        public Real Rho
        {
            get
            {
                Calculate();
                return Result<Real>("rho");
            }
        }

        /// <summary>
        /// Calculates the Black-Scholes dividend rho of the option.
        /// </summary>
        public Real DividendRho
        {
            get
            {
                Calculate();
                return Result<Real>("dividendRho");
            }
        }

        /// <summary>
        /// Returns the strike price of the option.
        /// </summary>
        public Real Strike => ((IStrikedTypePayoff)Payoff).Strike;

        /// <summary>
        /// Returns the option type (Call or Put).
        /// </summary>
        public Option.Type OptionType => ((IStrikedTypePayoff)Payoff).OptionType;

        #region Engine Registration Helpers
        /// <summary>
        /// Registers an analytic European engine for the option.
        /// </summary>
        public void UseAnalyticEuropeanEngine(GeneralizedBlackScholesProcess process)
        {
            SetPricingEngine(new AnalyticEuropeanEngine(process));
        }

        /// <summary>
        /// Registers an analytic dividend European engine for the option.
        /// </summary>
        public void UseAnalyticDividendEuropeanEngine(GeneralizedBlackScholesProcess process, 
                                                     DividendSchedule dividends)
        {
            SetPricingEngine(new AnalyticDividendEuropeanEngine(process, dividends));
        }

        /// <summary>
        /// Registers a finite difference Black-Scholes engine for the option.
        /// </summary>
        public void UseFdBlackScholesEngine(GeneralizedBlackScholesProcess process, 
                                           DividendSchedule dividends = null)
        {
            SetPricingEngine(new FdBlackScholesVanillaEngine(process, dividends));
        }
        #endregion
    }
}