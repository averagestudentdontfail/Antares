// C# code for FdmBlackScholesMesher.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math.Distribution;
using Antares.Method.Mesh;
using Antares.Method.Utilities;
using Antares.Process;
using Antares.Term;
using Antares.Term.Yield;
using Antares.Term.Volatility;
using Antares.Instrument;
using Antares.Time;
using Antares.Quote;

namespace Antares.Method.Mesh
{
    /// <summary>
    /// 1-D mesher for the Black-Scholes process (in ln(S)).
    /// </summary>
    public class FdmBlackScholesMesher : Fdm1dMesher
    {
        /// <summary>
        /// Creates a new mesher for a Black-Scholes process.
        /// </summary>
        /// <param name="size">The number of points in the grid.</param>
        /// <param name="process">The Black-Scholes process.</param>
        /// <param name="maturity">The maturity of the option/problem.</param>
        /// <param name="strike">The strike price, used for volatility lookup.</param>
        /// <param name="xMinConstraint">Optional minimum value for the log-space grid.</param>
        /// <param name="xMaxConstraint">Optional maximum value for the log-space grid.</param>
        /// <param name="eps">The cutoff for the normal distribution.</param>
        /// <param name="scaleFactor">A scaling factor for the grid width.</param>
        /// <param name="cPointInfo">Optional concentration point information (point, density).</param>
        /// <param name="dividendSchedule">A list of dividends.</param>
        /// <param name="fdmQuantoHelper">Optional helper for quanto adjustments.</param>
        /// <param name="spotAdjustment">An optional adjustment to the initial spot price.</param>
        public FdmBlackScholesMesher(int size,
            GeneralizedBlackScholesProcess process,
            double maturity, double strike,
            double? xMinConstraint = null,
            double? xMaxConstraint = null,
            double eps = 0.0001,
            double scaleFactor = 1.5,
            (double? point, double? density)? cPointInfo = null,
            IReadOnlyList<Dividend> dividendSchedule = null,
            FdmQuantoHelper fdmQuantoHelper = null,
            double spotAdjustment = 0.0)
            : base(size)
        {
            double s0 = process.X0.Value;
            QL.Require(s0 > 0.0, "Negative or null underlying given.");
            dividendSchedule ??= new List<Dividend>();
            var cPoint = cPointInfo ?? (null, null);

            var intermediateSteps = new List<(double time, double amount)>();
            foreach (var div in dividendSchedule)
            {
                double t = process.Time(div.Date);
                if (t <= maturity && t >= 0.0)
                    intermediateSteps.Add((t, div.Amount));
            }

            int intermediateTimeSteps = System.Math.Max(2, (int)(24.0 * maturity));
            for (int i = 0; i < intermediateTimeSteps; ++i)
                intermediateSteps.Add(((i + 1) * (maturity / intermediateTimeSteps), 0.0));

            intermediateSteps.Sort((a, b) => a.time.CompareTo(b.time));
            
            YieldTermStructure rTS = process.RiskFreeRate;
            YieldTermStructure qTS;

            if (fdmQuantoHelper != null)
            {
                qTS = new QuantoTermStructure(
                    process.DividendYield,
                    process.RiskFreeRate,
                    fdmQuantoHelper.ForeignRiskFreeRate,
                    process.BlackVolatility,
                    strike,
                    fdmQuantoHelper.FxVolatility,
                    fdmQuantoHelper.ExchangeRateAtmLevel,
                    fdmQuantoHelper.EquityFxCorrelation);
            }
            else
            {
                qTS = process.DividendYield;
            }

            double lastDivTime = 0.0;
            double fwd = s0 + spotAdjustment;
            double mi = fwd, ma = fwd;

            foreach (var step in intermediateSteps)
            {
                double divTime = step.time;
                double divAmount = step.amount;

                fwd = fwd / rTS.Discount(divTime) * rTS.Discount(lastDivTime)
                          * qTS.Discount(divTime) / qTS.Discount(lastDivTime);

                mi = System.Math.Min(mi, fwd);
                ma = System.Math.Max(ma, fwd);

                fwd -= divAmount;

                mi = System.Math.Min(mi, fwd);
                ma = System.Math.Max(ma, fwd);

                lastDivTime = divTime;
            }

            // Set the grid boundaries in log-space
            double normInvEps = new InverseCumulativeNormal().Value(1.0 - eps);
            double sigmaSqrtT = process.BlackVolatility.BlackVol(maturity, strike) * System.Math.Sqrt(maturity);

            double xMin = System.Math.Log(mi) - sigmaSqrtT * normInvEps * scaleFactor;
            double xMax = System.Math.Log(ma) + sigmaSqrtT * normInvEps * scaleFactor;

            if (xMinConstraint.HasValue)
            {
                xMin = xMinConstraint.Value;
            }
            if (xMaxConstraint.HasValue)
            {
                xMax = xMaxConstraint.Value;
            }

            Fdm1dMesher helper;
            if (cPoint.point.HasValue
                && System.Math.Log(cPoint.point.Value) >= xMin
                && System.Math.Log(cPoint.point.Value) <= xMax)
            {
                var concentratedPointInfo = (point: (double?)System.Math.Log(cPoint.point.Value), density: cPoint.density);
                helper = new Concentrating1dMesher(xMin, xMax, size, concentratedPointInfo);
            }
            else
            {
                helper = new Uniform1dMesher(xMin, xMax, size);
            }

            // Copy locations and deltas from the helper mesher
            Array.Copy(helper.Locations.ToArray(), _locations, helper.Size);
            for (int i = 0; i < helper.Size; ++i)
            {
                _dplus[i] = helper.Dplus(i);
                _dminus[i] = helper.Dminus(i);
            }
        }

        /// <summary>
        /// A factory method to create a simple GeneralizedBlackScholesProcess.
        /// </summary>
        /// <param name="s0">The initial spot price.</param>
        /// <param name="rTS">The risk-free rate term structure.</param>
        /// <param name="qTS">The dividend yield term structure.</param>
        /// <param name="vol">The constant volatility.</param>
        /// <returns>A new GeneralizedBlackScholesProcess instance.</returns>
        public static GeneralizedBlackScholesProcess ProcessHelper(
            Quote s0,
            YieldTermStructure rTS,
            YieldTermStructure qTS,
            double vol)
        {
            var volTS = new BlackConstantVol(rTS.ReferenceDate,
                                             new Calendar(),
                                             vol,
                                             rTS.DayCounter);

            return new GeneralizedBlackScholesProcess(s0, qTS, rTS, volTS);
        }
    }
}