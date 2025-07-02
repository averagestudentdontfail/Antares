// C# code for FdmQuantoHelper.cs

using System;
using Antares.Math;
using Antares.Pattern;
using QLNet;
using QLNet.Termstructures;

namespace Antares.Method.Utility
{
    /// <summary>
    /// Helper class storing market data needed for quanto adjustments in finite difference methods.
    /// A quanto option is an equity option where the payout is converted to a different currency
    /// at a predetermined exchange rate, requiring adjustment for currency correlation effects.
    /// </summary>
    public class FdmQuantoHelper : Observable
    {
        /// <summary>
        /// Domestic yield term structure.
        /// </summary>
        public readonly YieldTermStructure RTS;

        /// <summary>
        /// Foreign yield term structure.
        /// </summary>
        public readonly YieldTermStructure FTS;

        /// <summary>
        /// FX volatility term structure.
        /// </summary>
        public readonly BlackVolTermStructure FxVolTS;

        /// <summary>
        /// Correlation between equity and FX rates.
        /// </summary>
        public readonly Real EquityFxCorrelation;

        /// <summary>
        /// Exchange rate at-the-money level.
        /// </summary>
        public readonly Real ExchRateATMLevel;

        /// <summary>
        /// Initializes a new instance of the FdmQuantoHelper class.
        /// </summary>
        /// <param name="rTS">Domestic yield term structure.</param>
        /// <param name="fTS">Foreign yield term structure.</param>
        /// <param name="fxVolTS">FX volatility term structure.</param>
        /// <param name="equityFxCorrelation">Correlation between equity and FX rates.</param>
        /// <param name="exchRateATMLevel">Exchange rate at-the-money level.</param>
        public FdmQuantoHelper(YieldTermStructure rTS,
                              YieldTermStructure fTS,
                              BlackVolTermStructure fxVolTS,
                              Real equityFxCorrelation,
                              Real exchRateATMLevel)
        {
            RTS = rTS ?? throw new ArgumentNullException(nameof(rTS));
            FTS = fTS ?? throw new ArgumentNullException(nameof(fTS));
            FxVolTS = fxVolTS ?? throw new ArgumentNullException(nameof(fxVolTS));
            EquityFxCorrelation = equityFxCorrelation;
            ExchRateATMLevel = exchRateATMLevel;
        }

        /// <summary>
        /// Calculates the quanto adjustment for a single equity volatility.
        /// </summary>
        /// <param name="equityVol">The equity volatility.</param>
        /// <param name="t1">Start time for the adjustment period.</param>
        /// <param name="t2">End time for the adjustment period.</param>
        /// <returns>The quanto-adjusted rate.</returns>
        /// <remarks>
        /// The quanto adjustment formula is:
        /// rDomestic - rForeign + equityVol * fxVol * correlation
        /// 
        /// This adjustment accounts for the fact that in a quanto option,
        /// the correlation between the equity and FX rates affects the drift
        /// of the equity process when measured in the domestic currency.
        /// </remarks>
        public Rate QuantoAdjustment(Volatility equityVol, Time t1, Time t2)
        {
            Rate rDomestic = RTS.forwardRate(t1, t2, Compounding.Continuous).rate();
            Rate rForeign = FTS.forwardRate(t1, t2, Compounding.Continuous).rate();
            Volatility fxVol = FxVolTS.blackForwardVol(t1, t2, ExchRateATMLevel);

            return rDomestic - rForeign + equityVol * fxVol * EquityFxCorrelation;
        }

        /// <summary>
        /// Calculates the quanto adjustment for an array of equity volatilities.
        /// </summary>
        /// <param name="equityVol">Array of equity volatilities.</param>
        /// <param name="t1">Start time for the adjustment period.</param>
        /// <param name="t2">End time for the adjustment period.</param>
        /// <returns>Array of quanto-adjusted rates corresponding to each input volatility.</returns>
        /// <remarks>
        /// This overload is useful when dealing with time-dependent or space-dependent
        /// volatilities in finite difference grids where different grid points may
        /// have different local volatilities.
        /// </remarks>
        public Array QuantoAdjustment(Array equityVol, Time t1, Time t2)
        {
            if (equityVol == null)
                throw new ArgumentNullException(nameof(equityVol));

            Rate rDomestic = RTS.forwardRate(t1, t2, Compounding.Continuous).rate();
            Rate rForeign = FTS.forwardRate(t1, t2, Compounding.Continuous).rate();
            Volatility fxVol = FxVolTS.blackForwardVol(t1, t2, ExchRateATMLevel);

            var retVal = new Array(equityVol.Count);
            for (int i = 0; i < retVal.Count; ++i)
            {
                retVal[i] = rDomestic - rForeign + equityVol[i] * fxVol * EquityFxCorrelation;
            }

            return retVal;
        }
    }
}