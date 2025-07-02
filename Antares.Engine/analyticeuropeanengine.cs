// C# code for AnalyticEuropeanEngine.cs

using System;
using System.Collections.Generic;
using Antares.Instrument;
using Antares.Process;
using Antares.Term.Yield;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Engine
{
    /// <summary>
    /// Pricing engine for European vanilla options using analytical formulae.
    /// </summary>
    /// <remarks>
    /// The correctness of the returned value is tested by reproducing results 
    /// available in literature. The correctness of the returned Greeks is tested 
    /// by reproducing results available in literature and by reproducing numerical 
    /// derivatives. The correctness of the returned implied volatility is tested 
    /// by using it for reproducing the target value. The implied-volatility 
    /// calculation is tested by checking that it does not modify the option.
    /// The correctness of the returned value in case of cash-or-nothing, 
    /// asset-or-nothing, and gap digital payoffs is tested by reproducing 
    /// results available in literature. The correctness of the returned Greeks 
    /// in case of cash-or-nothing digital payoff is tested by reproducing 
    /// numerical derivatives.
    /// </remarks>
    public class AnalyticEuropeanEngine : OneAssetOption.Engine
    {
        #region Private Fields

        private readonly GeneralizedBlackScholesProcess _process;
        private readonly Handle<YieldTermStructure> _discountCurve;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the AnalyticEuropeanEngine class.
        /// This constructor triggers the usual calculation, in which the risk-free 
        /// rate in the given process is used for both forecasting and discounting.
        /// </summary>
        /// <param name="process">The generalized Black-Scholes process.</param>
        public AnalyticEuropeanEngine(GeneralizedBlackScholesProcess process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _discountCurve = new Handle<YieldTermStructure>();
            
            RegisterWith(_process);
        }

        /// <summary>
        /// Initializes a new instance of the AnalyticEuropeanEngine class.
        /// This constructor allows to use a different term structure for discounting 
        /// the payoff. As usual, the risk-free rate from the given process is used 
        /// for forecasting the forward price.
        /// </summary>
        /// <param name="process">The generalized Black-Scholes process.</param>
        /// <param name="discountCurve">The discount curve to use for discounting.</param>
        public AnalyticEuropeanEngine(GeneralizedBlackScholesProcess process, 
                                     Handle<YieldTermStructure> discountCurve)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _discountCurve = discountCurve ?? throw new ArgumentNullException(nameof(discountCurve));
            
            RegisterWith(_process);
            RegisterWith(_discountCurve);
        }

        #endregion

        #region IPricingEngine Implementation

        /// <summary>
        /// Performs the analytical calculation for European options.
        /// </summary>
        public override void Calculate()
        {
            // If the discount curve is not specified, we default to the
            // risk free rate curve embedded within the GBM process
            YieldTermStructure discountPtr = _discountCurve.IsEmpty
                ? _process.RiskFreeRate.Value
                : _discountCurve.Value;

            QL.Require(_arguments.Exercise.Type == Exercise.ExerciseType.European,
                       "not an European option");

            var payoff = _arguments.Payoff as StrikedTypePayoff;
            QL.Require(payoff != null, "non-striked payoff given");

            double variance = _process.BlackVolatility.Value.BlackVariance(
                _arguments.Exercise.LastDate, payoff.Strike);

            double dividendDiscount = _process.DividendYield.Value.Discount(
                _arguments.Exercise.LastDate);

            double df = discountPtr.Discount(_arguments.Exercise.LastDate);

            double riskFreeDiscountForFwdEstimation = _process.RiskFreeRate.Value.Discount(
                _arguments.Exercise.LastDate);

            double spot = _process.StateVariable.Value.Value;
            QL.Require(spot > 0.0, "negative or null underlying given");

            double forwardPrice = spot * dividendDiscount / riskFreeDiscountForFwdEstimation;

            var black = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance), df);

            // Store the basic results
            _results.Value = black.Value();
            
            // Store Greeks
            _results.Greeks.Delta = black.Delta(spot);
            _results.Greeks.Gamma = black.Gamma(spot);
            _results.Greeks.Rho = black.Rho(GetTimeToMaturity(
                discountPtr.DayCounter, discountPtr.ReferenceDate));
            _results.Greeks.DividendRho = black.DividendRho(GetTimeToMaturity(
                _process.DividendYield.Value.DayCounter, 
                _process.DividendYield.Value.ReferenceDate));
            _results.Greeks.Vega = black.Vega(GetTimeToMaturity(
                _process.BlackVolatility.Value.DayCounter,
                _process.BlackVolatility.Value.ReferenceDate));

            // Store MoreGreeks
            _results.MoreGreeks.DeltaForward = black.DeltaForward();
            _results.MoreGreeks.Elasticity = black.Elasticity(spot);
            _results.MoreGreeks.StrikeSensitivity = black.StrikeSensitivity();
            _results.MoreGreeks.ItmCashProbability = black.ItmCashProbability;

            // Calculate theta and thetaPerDay with error handling
            try
            {
                double timeToMaturity = GetTimeToMaturity(
                    discountPtr.DayCounter, discountPtr.ReferenceDate);
                
                _results.Greeks.Theta = black.Theta(spot, timeToMaturity);
                _results.MoreGreeks.ThetaPerDay = black.ThetaPerDay(spot, timeToMaturity);
            }
            catch (AntaresException)
            {
                _results.Greeks.Theta = null;
                _results.MoreGreeks.ThetaPerDay = null;
            }

            // Store additional results for debugging and analysis
            double tte = _process.BlackVolatility.Value.TimeFromReference(_arguments.Exercise.LastDate);
            _results.AdditionalResults["spot"] = spot;
            _results.AdditionalResults["dividendDiscount"] = dividendDiscount;
            _results.AdditionalResults["riskFreeDiscount"] = riskFreeDiscountForFwdEstimation;
            _results.AdditionalResults["forward"] = forwardPrice;
            _results.AdditionalResults["strike"] = payoff.Strike;
            _results.AdditionalResults["volatility"] = Math.Sqrt(variance / tte);
            _results.AdditionalResults["timeToExpiry"] = tte;
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Calculates the time to maturity using the specified day counter and reference date.
        /// </summary>
        /// <param name="dayCounter">The day counter to use.</param>
        /// <param name="referenceDate">The reference date.</param>
        /// <returns>The time to maturity in years.</returns>
        private double GetTimeToMaturity(DayCounter dayCounter, Date referenceDate)
        {
            return dayCounter.YearFraction(referenceDate, _arguments.Exercise.LastDate);
        }

        #endregion
    }
}