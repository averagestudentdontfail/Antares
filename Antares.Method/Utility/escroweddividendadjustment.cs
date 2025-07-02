// C# code for EscrowedDividendAdjustment.cs

using System;
using System.Collections.Generic;
using QLNet.Time;

namespace Antares.Method.Utility
{
    /// <summary>
    /// Interface for dividend objects used in dividend schedules
    /// </summary>
    public interface IDividend
    {
        /// <summary>
        /// Gets the dividend amount
        /// </summary>
        Real Amount { get; }

        /// <summary>
        /// Gets the dividend date
        /// </summary>
        Date Date { get; }
    }

    /// <summary>
    /// Placeholder for YieldTermStructure - this would be replaced by the actual implementation
    /// </summary>
    public interface IYieldTermStructure
    {
        /// <summary>
        /// Returns the discount factor for the given time
        /// </summary>
        /// <param name="t">The time</param>
        /// <returns>The discount factor</returns>
        Real Discount(Time t);
    }

    /// <summary>
    /// Represents a schedule of dividends
    /// </summary>
    public class DividendSchedule : List<IDividend>
    {
        public DividendSchedule() : base() { }
        public DividendSchedule(IEnumerable<IDividend> dividends) : base(dividends) { }
    }

    /// <summary>
    /// Handles escrowed dividend adjustments for options pricing
    /// </summary>
    public class EscrowedDividendAdjustment
    {
        private readonly DividendSchedule _dividendSchedule;
        private readonly Handle<IYieldTermStructure> _rTs;
        private readonly Handle<IYieldTermStructure> _qTs;
        private readonly Func<Date, Real> _toTime;
        private readonly Time _maturity;

        /// <summary>
        /// Constructs an escrowed dividend adjustment calculator
        /// </summary>
        /// <param name="dividendSchedule">The schedule of dividends</param>
        /// <param name="rTs">Risk-free rate term structure</param>
        /// <param name="qTs">Dividend yield term structure</param>
        /// <param name="toTime">Function to convert dates to times</param>
        /// <param name="maturity">The maturity time</param>
        public EscrowedDividendAdjustment(
            DividendSchedule dividendSchedule,
            Handle<IYieldTermStructure> rTs,
            Handle<IYieldTermStructure> qTs,
            Func<Date, Real> toTime,
            Time maturity)
        {
            _dividendSchedule = dividendSchedule ?? throw new ArgumentNullException(nameof(dividendSchedule));
            _rTs = rTs ?? throw new ArgumentNullException(nameof(rTs));
            _qTs = qTs ?? throw new ArgumentNullException(nameof(qTs));
            _toTime = toTime ?? throw new ArgumentNullException(nameof(toTime));
            _maturity = maturity;
        }

        /// <summary>
        /// Calculates the dividend adjustment for a given time
        /// </summary>
        /// <param name="t">The time at which to calculate the adjustment</param>
        /// <returns>The dividend adjustment amount</returns>
        public Real DividendAdjustment(Time t)
        {
            Real divAdj = 0.0;
            
            foreach (var dividend in _dividendSchedule)
            {
                Time divTime = _toTime(dividend.Date);

                if (divTime >= t && t <= _maturity)
                {
                    divAdj -= dividend.Amount
                        * _rTs.Value.Discount(divTime) / _rTs.Value.Discount(t)
                        * _qTs.Value.Discount(t) / _qTs.Value.Discount(divTime);
                }
            }

            return divAdj;
        }

        /// <summary>
        /// Gets the risk-free rate term structure
        /// </summary>
        public Handle<IYieldTermStructure> RiskFreeRate => _rTs;

        /// <summary>
        /// Gets the dividend yield term structure
        /// </summary>
        public Handle<IYieldTermStructure> DividendYield => _qTs;
    }
}