// C# code for Option.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using QLNet;

namespace Antares
{
    #region Supporting Infrastructure (Placeholders for other files)

    /// <summary>
    /// Base class for all financial instruments.
    /// This is a simplified placeholder for the actual Instrument class.
    /// </summary>
    public abstract class Instrument : IObservable
    {
        private readonly Observable _observable = new Observable();
        protected IPricingEngine _engine;

        public abstract bool IsExpired { get; }

        public abstract void SetupArguments(IPricingEngine.IArguments args);

        public void SetPricingEngine(IPricingEngine engine)
        {
            _engine = engine;
            if (_engine != null)
                _engine.RegisterWith(new InstrumentObserver(this));
            NotifyObservers();
        }

        protected void NotifyObservers() => _observable.NotifyObservers();
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);

        // Inner class to proxy notifications
        private class InstrumentObserver : IObserver
        {
            private readonly Instrument _instrument;
            public InstrumentObserver(Instrument instrument) => _instrument = instrument;
            public void Update() => _instrument.NotifyObservers();
        }
    }

    /// <summary>
    /// Base interface for instrument exercise schedules.
    /// </summary>
    public interface IExercise { }

    #endregion

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
            _payoff = payoff;
            _exercise = exercise;
        }

        public override void SetupArguments(IPricingEngine.IArguments args)
        {
            if (args is not Arguments optionArgs)
                throw new ArgumentException($"Wrong argument type. Expected {nameof(Arguments)}, got {args?.GetType().Name ?? "null"}.");

            optionArgs.Payoff = _payoff;
            optionArgs.Exercise = _exercise;
        }

        /// <summary>
        /// The option's payoff.
        /// </summary>
        public IPayoff Payoff => _payoff;

        /// <summary>
        /// The option's exercise schedule.
        /// </summary>
        public IExercise Exercise => _exercise;

        /// <summary>
        /// Basic option arguments.
        /// </summary>
        public class Arguments : IPricingEngine.IArguments
        {
            public IPayoff Payoff { get; set; }
            public IExercise Exercise { get; set; }

            public virtual void Validate()
            {
                if (Payoff == null)
                    throw new InvalidOperationException("No payoff given.");
                if (Exercise == null)
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
        public Real? Delta { get; set; }
        public Real? Gamma { get; set; }
        public Real? Theta { get; set; }
        public Real? Vega { get; set; }
        public Real? Rho { get; set; }
        public Real? DividendRho { get; set; }

        public virtual void Reset()
        {
            Delta = null;
            Gamma = null;
            Theta = null;
            Vega = null;
            Rho = null;
            DividendRho = null;
        }
    }

    /// <summary>
    /// More additional option results.
    /// </summary>
    public class MoreGreeks : IPricingEngine.IResults
    {
        public Real? ItmCashProbability { get; set; }
        public Real? DeltaForward { get; set; }
        public Real? Elasticity { get; set; }
        public Real? ThetaPerDay { get; set; }
        public Real? StrikeSensitivity { get; set; }

        public virtual void Reset()
        {
            ItmCashProbability = null;
            DeltaForward = null;
            Elasticity = null;
            ThetaPerDay = null;
            StrikeSensitivity = null;
        }
    }
}