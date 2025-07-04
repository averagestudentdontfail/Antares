// Option.cs

using System;
using System.ComponentModel;

namespace Antares
{
    /// <summary>
    /// Base option class.
    /// </summary>
    public abstract class Option : Instrument
    {
        /// <summary>
        /// Defines the type of an option (Call or Put).
        /// </summary>
        public enum Type
        {
            Put = -1,
            Call = 1
        }

        protected readonly IPayoff _payoff;
        protected readonly IExercise _exercise;

        protected Option(IPayoff payoff, IExercise exercise)
        {
            _payoff = payoff ?? throw new ArgumentNullException(nameof(payoff));
            _exercise = exercise ?? throw new ArgumentNullException(nameof(exercise));
        }

        /// <summary>
        /// The option's payoff.
        /// </summary>
        public IPayoff payoff() => _payoff;

        /// <summary>
        /// The option's exercise schedule.
        /// </summary>
        public IExercise exercise() => _exercise;

        public override void setupArguments(IPricingEngine.IArguments args)
        {
            if (args is not Arguments optionArgs)
                throw new ArgumentException($"Wrong argument type. Expected {nameof(Arguments)}, got {args?.GetType().Name ?? "null"}.");

            optionArgs.payoff = _payoff;
            optionArgs.exercise = _exercise;
        }

        /// <summary>
        /// Basic option arguments.
        /// </summary>
        public class Arguments : IPricingEngine.IArguments
        {
            public IPayoff? payoff { get; set; }
            public IExercise? exercise { get; set; }

            public virtual void Validate()
            {
                if (payoff == null)
                    throw new InvalidOperationException("No payoff given.");
                if (exercise == null)
                    throw new InvalidOperationException("No exercise given.");
            }
        }
    }

    /// <summary>
    /// Provides extension methods for the Option.Type enum.
    /// </summary>
    public static class OptionTypeExtensions
    {
        /// <summary>
        /// Returns a string representation of the option type.
        /// </summary>
        public static string ToFormattedString(this Option.Type type)
        {
            return type switch
            {
                Option.Type.Call => "Call",
                Option.Type.Put => "Put",
                _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(Option.Type)),
            };
        }
    }

    /// <summary>
    /// Additional option results for first-order Greeks.
    /// </summary>
    public class Greeks : IPricingEngine.IResults
    {
        public Real? delta { get; set; }
        public Real? gamma { get; set; }
        public Real? theta { get; set; }
        public Real? vega { get; set; }
        public Real? rho { get; set; }
        public Real? dividendRho { get; set; }

        public virtual void Reset()
        {
            delta = null;
            gamma = null;
            theta = null;
            vega = null;
            rho = null;
            dividendRho = null;
        }
    }

    /// <summary>
    /// More additional option results.
    /// </summary>
    public class MoreGreeks : IPricingEngine.IResults
    {
        public Real? itmCashProbability { get; set; }
        public Real? deltaForward { get; set; }
        public Real? elasticity { get; set; }
        public Real? thetaPerDay { get; set; }
        public Real? strikeSensitivity { get; set; }

        public virtual void Reset()
        {
            itmCashProbability = null;
            deltaForward = null;
            elasticity = null;
            thetaPerDay = null;
            strikeSensitivity = null;
        }
    }
}