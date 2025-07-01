// C# code for ImpliedVolatility.cs

using System;
using Antares.Pattern;
using Antares.Process;
using Antares.Quote;
using Antares.Term.Volatility;
using Antares.Time;
using MathNet.Numerics.RootFinding;

namespace Antares.Instrument
{
    /// <summary>
    /// Helper class for one-asset implied-volatility calculation.
    /// This corresponds to the `detail::ImpliedVolatilityHelper` in QuantLib.
    /// </summary>
    internal static class ImpliedVolatilityHelper
    {
        /// <summary>
        /// Performs the root-finding calculation to find the implied volatility.
        /// </summary>
        /// <remarks>
        /// The passed engine must be linked to the passed quote.
        /// </remarks>
        public static Volatility Calculate(
            Instrument instrument,
            IPricingEngine engine,
            SimpleQuote volQuote,
            Real targetValue,
            Real accuracy,
            int maxEvaluations,
            Volatility minVol,
            Volatility maxVol)
        {
            // Set up the arguments on the pricing engine.
            instrument.SetupArguments(engine.GetArguments());
            engine.GetArguments().Validate();

            // The objective function for the root-finder.
            // It calculates the difference between the model price and the target price.
            Func<double, double> f = vol =>
            {
                // Set the new volatility value in the quote, which is observed by the engine's process.
                volQuote.SetValue(vol);
                // The engine recalculates with the new volatility.
                engine.Calculate();
                // We need to fetch the results from the engine.
                var results = engine.GetResults() as Instrument.Results;
                QL.Require(results != null, "Pricing engine does not supply needed results");
                QL.Require(results.Value.HasValue, "Pricing engine returned a null value");

                return results.Value.Value - targetValue;
            };

            // Use a robust solver to find the volatility.
            try
            {
                // Brent's method is a good choice for this type of problem.
                double guess = (minVol + maxVol) / 2.0;
                if (!Brent.TryFindRoot(f, minVol, maxVol, accuracy, maxEvaluations, out double result))
                {
                    // If the solver fails, it often means the root is not bracketed or does not exist.
                    QL.Fail($"failed to converge: root not bracketed by ({minVol}, {maxVol}) or other solver error.");
                }
                return result;
            }
            catch (Exception e)
            {
                QL.Fail($"failed to converge: {e.Message}");
                return 0; // Unreachable code
            }
        }

        /// <summary>
        /// Clones a process, replacing its volatility structure with one linked to a given quote.
        /// </summary>
        /// <remarks>
        /// The returned process is equal to the passed one, except for the volatility,
        /// which is flat and whose value is driven by the passed quote.
        /// </remarks>
        public static GeneralizedBlackScholesProcess Clone(
            GeneralizedBlackScholesProcess process,
            Handle<IQuote> volQuote)
        {
            Handle<IQuote> stateVariable = process.StateVariable;
            Handle<YieldTermStructure> dividendYield = process.DividendYield;
            Handle<YieldTermStructure> riskFreeRate = process.RiskFreeRate;
            Handle<BlackVolTermStructure> blackVol = process.BlackVolatility;

            // Create a new, flat volatility term structure linked to the quote
            var constantVol = new BlackConstantVol(
                blackVol.Value.ReferenceDate,
                blackVol.Value.Calendar,
                volQuote,
                blackVol.Value.DayCounter);

            var volatilityHandle = new Handle<BlackVolTermStructure>(constantVol);

            // Re-create the process with the new volatility handle.
            // Using BlackScholesMertonProcess as the concrete type for the clone.
            return new BlackScholesMertonProcess(
                stateVariable,
                dividendYield,
                riskFreeRate,
                volatilityHandle);
        }
    }
}