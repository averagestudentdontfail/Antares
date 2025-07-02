// C# code for AnalyticDividendEuropeanEngine.cs

using System;
using System.Collections.Generic;
using Antares.Cashflow;
using Antares.Instrument;
using Antares.Process;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Engine
{
    /// <summary>
    /// Analytic pricing engine for European options with discrete dividends.
    /// </summary>
    /// <remarks>
    /// This engine handles European vanilla options where discrete dividends are paid 
    /// during the life of the option. The dividends are incorporated by adjusting the 
    /// spot price down by the present value of the dividends that occur between the 
    /// settlement date and the option expiry.
    /// 
    /// The correctness of the returned Greeks is tested by reproducing numerical derivatives.
    /// </remarks>
    public class AnalyticDividendEuropeanEngine : OneAssetOption.Engine
    {
        #region Private Fields

        private readonly GeneralizedBlackScholesProcess _process;
        private readonly IReadOnlyList<IDividend> _dividends;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the AnalyticDividendEuropeanEngine class.
        /// </summary>
        /// <param name="process">The generalized Black-Scholes process.</param>
        /// <param name="dividends">The schedule of discrete dividends.</param>
        public AnalyticDividendEuropeanEngine(GeneralizedBlackScholesProcess process,
                                             IReadOnlyList<IDividend> dividends)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _dividends = dividends ?? throw new ArgumentNullException(nameof(dividends));
            
            RegisterWith(_process);
        }

        #endregion

        #region IPricingEngine Implementation

        /// <summary>
        /// Performs the analytical calculation for European options with discrete dividends.
        /// </summary>
        public override void Calculate()
        {
            QL.Require(_arguments.Exercise.Type == Exercise.ExerciseType.European,
                       "not an European option");

            var payoff = _arguments.Payoff as StrikedTypePayoff;
            QL.Require(payoff != null, "non-striked payoff given");

            Date settlementDate = _process.RiskFreeRate.Value.ReferenceDate;
            double riskless = 0.0;

            // Calculate the present value of dividends that fall between settlement and exercise
            foreach (var dividend in _dividends)
            {
                Date cashFlowDate = dividend.Date;

                if (cashFlowDate >= settlementDate && cashFlowDate <= _arguments.Exercise.LastDate)
                {
                    riskless += dividend.Amount *
                        _process.RiskFreeRate.Value.Discount(cashFlowDate) /
                        _process.DividendYield.Value.Discount(cashFlowDate);
                }
            }

            // Adjust spot price by subtracting the present value of dividends
            double spot = _process.StateVariable.Value.Value - riskless;
            QL.Require(spot > 0.0,
                       "negative or null underlying after subtracting dividends");

            double dividendDiscount = _process.DividendYield.Value.Discount(
                _arguments.Exercise.LastDate);
            double riskFreeDiscount = _process.RiskFreeRate.Value.Discount(
                _arguments.Exercise.LastDate);
            double forwardPrice = spot * dividendDiscount / riskFreeDiscount;

            double variance = _process.BlackVolatility.Value.BlackVariance(
                _arguments.Exercise.LastDate, payoff.Strike);

            var black = new BlackCalculator(payoff, forwardPrice, Math.Sqrt(variance), riskFreeDiscount);

            // Store basic results
            _results.Value = black.Value();
            _results.Greeks.Delta = black.Delta(spot);
            _results.Greeks.Gamma = black.Gamma(spot);

            DayCounter rfdc = _process.RiskFreeRate.Value.DayCounter;
            DayCounter dydc = _process.DividendYield.Value.DayCounter;
            DayCounter voldc = _process.BlackVolatility.Value.DayCounter;
            
            double t = voldc.YearFraction(_process.BlackVolatility.Value.ReferenceDate,
                                         _arguments.Exercise.LastDate);
            _results.Greeks.Vega = black.Vega(t);

            // Calculate dividend adjustments for theta and rho
            double deltaTheta = 0.0;
            double deltaRho = 0.0;

            foreach (var dividend in _dividends)
            {
                Date d = dividend.Date;

                if (d >= settlementDate && d <= _arguments.Exercise.LastDate)
                {
                    var rfRate = _process.RiskFreeRate.Value.ZeroRate(d, rfdc, Compounding.Continuous, Frequency.Annual);
                    var dyRate = _process.DividendYield.Value.ZeroRate(d, dydc, Compounding.Continuous, Frequency.Annual);

                    deltaTheta -= dividend.Amount *
                        (rfRate.Rate - dyRate.Rate) *
                        _process.RiskFreeRate.Value.Discount(d) /
                        _process.DividendYield.Value.Discount(d);

                    double timeToDiv = _process.Time(d);
                    deltaRho += dividend.Amount * timeToDiv *
                        _process.RiskFreeRate.Value.Discount(timeToDiv) /
                        _process.DividendYield.Value.Discount(timeToDiv);
                }
            }

            double timeToExpiry = _process.Time(_arguments.Exercise.LastDate);

            // Calculate theta with dividend adjustment and error handling
            try
            {
                _results.Greeks.Theta = black.Theta(spot, timeToExpiry) +
                                       deltaTheta * black.Delta(spot);
            }
            catch (AntaresException)
            {
                _results.Greeks.Theta = null;
            }

            // Calculate rho with dividend adjustment
            _results.Greeks.Rho = black.Rho(timeToExpiry) +
                                 deltaRho * black.Delta(spot);

            // Store MoreGreeks (additional Greeks not affected by dividends)
            _results.MoreGreeks.DeltaForward = black.DeltaForward();
            _results.MoreGreeks.Elasticity = black.Elasticity(spot);
            _results.MoreGreeks.StrikeSensitivity = black.StrikeSensitivity();
            _results.MoreGreeks.ItmCashProbability = black.ItmCashProbability;

            // Calculate dividend rho (usually not affected by discrete dividends for this engine)
            _results.Greeks.DividendRho = black.DividendRho(timeToExpiry);

            try
            {
                _results.MoreGreeks.ThetaPerDay = black.ThetaPerDay(spot, timeToExpiry);
            }
            catch (AntaresException)
            {
                _results.MoreGreeks.ThetaPerDay = null;
            }

            // Store additional results for debugging and analysis
            _results.AdditionalResults["spot"] = spot;
            _results.AdditionalResults["unadjustedSpot"] = _process.StateVariable.Value.Value;
            _results.AdditionalResults["dividendDiscount"] = dividendDiscount;
            _results.AdditionalResults["riskFreeDiscount"] = riskFreeDiscount;
            _results.AdditionalResults["forward"] = forwardPrice;
            _results.AdditionalResults["strike"] = payoff.Strike;
            _results.AdditionalResults["variance"] = variance;
            _results.AdditionalResults["volatility"] = Math.Sqrt(variance / timeToExpiry);
            _results.AdditionalResults["timeToExpiry"] = timeToExpiry;
            _results.AdditionalResults["dividendPresentValue"] = riskless;
            _results.AdditionalResults["dividendCount"] = CountDividendsInPeriod(settlementDate, _arguments.Exercise.LastDate);
        }

        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Counts the number of dividends that fall between two dates (inclusive).
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>The number of dividends in the period.</returns>
        private int CountDividendsInPeriod(Date startDate, Date endDate)
        {
            int count = 0;
            foreach (var dividend in _dividends)
            {
                if (dividend.Date >= startDate && dividend.Date <= endDate)
                {
                    count++;
                }
            }
            return count;
        }

        #endregion
    }
}