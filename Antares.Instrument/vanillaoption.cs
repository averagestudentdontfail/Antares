// C# code for VanillaOption.cs

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
    #region Supporting Infrastructure (Placeholders for other files)
    // This infrastructure is included to make the file self-contained and compilable.
    // In a real project, these would be referenced via `using` statements.

    public interface IStrikedTypePayoff : IPayoff { }

    public abstract class OneAssetOption : Option
    {
        protected OneAssetOption(IPayoff payoff, IExercise exercise) : base(payoff, exercise) { }
    }

    // --- Pricing Engine Placeholders ---
    public class AnalyticEuropeanEngine : IPricingEngine
    {
        public AnalyticEuropeanEngine(GeneralizedBlackScholesProcess process) { }
        public IPricingEngine.IArguments GetArguments() => null;
        public IPricingEngine.IResults GetResults() => null;
        public void Reset() { }
        public void Calculate() { /* Dummy implementation */ }
        public void RegisterWith(IObserver observer) { }
        public void UnregisterWith(IObserver observer) { }
    }

    public class AnalyticDividendEuropeanEngine : IPricingEngine
    {
        public AnalyticDividendEuropeanEngine(GeneralizedBlackScholesProcess process, DividendSchedule dividends) { }
        public IPricingEngine.IArguments GetArguments() => null;
        public IPricingEngine.IResults GetResults() => null;
        public void Reset() { }
        public void Calculate() { /* Dummy implementation */ }
        public void RegisterWith(IObserver observer) { }
        public void UnregisterWith(IObserver observer) { }
    }

    public class FdBlackScholesVanillaEngine : IPricingEngine
    {
        public FdBlackScholesVanillaEngine(GeneralizedBlackScholesProcess process, DividendSchedule dividends = null) { }
        public IPricingEngine.IArguments GetArguments() => null;
        public IPricingEngine.IResults GetResults() => null;
        public void Reset() { }
        public void Calculate() { /* Dummy implementation */ }
        public void RegisterWith(IObserver observer) { }
        public void UnregisterWith(IObserver observer) { }
    }
    #endregion

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
        /// <param name="targetValue">The market price of the option.</param>
        /// <param name="process">The underlying stochastic process.</param>
        /// <param name="accuracy">The desired accuracy of the result.</param>
        /// <param name="maxEvaluations">The maximum number of iterations for the solver.</param>
        /// <param name="minVol">The lower bound for the volatility search.</param>
        /// <param name="maxVol">The upper bound for the volatility search.</param>
        /// <returns>The implied volatility.</returns>
        public Volatility ImpliedVolatility(
            Real targetValue,
            GeneralizedBlackScholesProcess process,
            Real accuracy = 1.0e-4,
            int maxEvaluations = 100,
            Volatility minVol = 1.0e-7,
            Volatility maxVol = 4.0)
        {
            return ImpliedVolatility(targetValue, process, new List<IDividend>(),
                                     accuracy, maxEvaluations, minVol, maxVol);
        }

        /// <summary>
        /// Calculates the implied volatility of the option.
        /// </summary>
        /// <remarks>
        /// Currently, this method uses analytic formulas for European options and
        /// a finite-difference method for American and Bermudan options. It will
        /// give inconsistent results if the pricing was performed with any other methods.
        /// </remarks>
        public Volatility ImpliedVolatility(
            Real targetValue,
            GeneralizedBlackScholesProcess process,
            DividendSchedule dividends,
            Real accuracy = 1.0e-4,
            int maxEvaluations = 100,
            Volatility minVol = 1.0e-7,
            Volatility maxVol = 4.0)
        {
            QL.Require(!IsExpired, "option expired");

            var volQuote = new SimpleQuote();

            // Clone the process and link its volatility to the quote
            var newProcess = ImpliedVolatilityHelper.Clone(process, new Handle<IQuote>(volQuote));

            // Engines are built-in for the time being
            IPricingEngine engine;
            switch (Exercise.Type)
            {
                case Exercise.ExerciseType.European:
                    if (dividends == null || dividends.Count == 0)
                        engine = new AnalyticEuropeanEngine(newProcess);
                    else
                        engine = new AnalyticDividendEuropeanEngine(newProcess, dividends);
                    break;
                case Exercise.ExerciseType.American:
                case Exercise.ExerciseType.Bermudan:
                    engine = new FdBlackScholesVanillaEngine(newProcess, dividends);
                    break;
                default:
                    QL.Fail("unknown exercise type");
                    return 0; // Unreachable
            }

            return ImpliedVolatilityHelper.Calculate(this, engine, volQuote, targetValue,
                                                     accuracy, maxEvaluations, minVol, maxVol);
        }
    }

    /// <summary>
    /// Helper class for implied volatility calculation.
    /// This corresponds to the `detail::ImpliedVolatilityHelper` pattern in QuantLib.
    /// </summary>
    internal static class ImpliedVolatilityHelper
    {
        /// <summary>
        /// Clones a process, replacing its volatility structure with one linked to a given quote.
        /// </summary>
        public static GeneralizedBlackScholesProcess Clone(GeneralizedBlackScholesProcess process, Handle<IQuote> volQuote)
        {
            // We create a new BlackConstantVol linked to the volQuote.
            // This new vol term structure will be used in the cloned process.
            var tempVol = new BlackConstantVol(
                process.RiskFreeRate.Value.ReferenceDate,
                process.RiskFreeRate.Value.Calendar,
                volQuote,
                process.RiskFreeRate.Value.DayCounter);

            // Create the new process, which is identical to the original except for the volatility structure.
            var newProcess = new BlackScholesMertonProcess(
                process.StateVariable,
                process.DividendYield,
                process.RiskFreeRate,
                new Handle<BlackVolTermStructure>(tempVol));
            
            return newProcess;
        }

        /// <summary>
        /// Performs the root-finding calculation to find the implied volatility.
        /// </summary>
        public static Volatility Calculate(
            VanillaOption option,
            IPricingEngine engine,
            SimpleQuote volQuote,
            Real targetValue,
            Real accuracy,
            int maxEvaluations,
            Volatility minVol,
            Volatility maxVol)
        {
            // Set the pricing engine on a temporary (cloned) option
            // to avoid altering the original instrument's state.
            var tempOption = new VanillaOption((IStrikedTypePayoff)option.Payoff, option.Exercise);
            tempOption.SetPricingEngine(engine);

            // The function whose root we want to find
            Func<double, double> f = vol =>
            {
                volQuote.SetValue(vol);
                return tempOption.NPV - targetValue;
            };

            // Use a robust solver to find the root
            try
            {
                double result = Brent.FindRoot(f, minVol, maxVol, accuracy, maxEvaluations);
                if (double.IsNaN(result))
                {
                    QL.Fail($"failed to converge: root not bracketed by ({minVol}, {maxVol})");
                }
                return result;
            }
            catch (Exception e)
            {
                QL.Fail($"failed to converge: {e.Message}");
                return 0; // Unreachable
            }
        }
    }
}