// Option.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Antares
{
    #region Missing Type Definitions

    /// <summary>
    /// Base interface for pricing engines.
    /// </summary>
    public interface IPricingEngine : IObservable
    {
        /// <summary>
        /// Interface for pricing engine arguments.
        /// </summary>
        interface IArguments
        {
            void Validate();
        }

        /// <summary>
        /// Interface for pricing engine results.
        /// </summary>
        interface IResults
        {
            void Reset();
        }

        IArguments GetArguments();
        IResults GetResults();
        void Reset();
        void Calculate();
    }

    /// <summary>
    /// Base interface for payoffs.
    /// </summary>
    public interface IPayoff
    {
        string Name { get; }
        string Description { get; }
        Real GetValue(Real price);
        void Accept(IAcyclicVisitor v);
    }

    /// <summary>
    /// Base interface for exercise schedules.
    /// </summary>
    public interface IExercise
    {
        Date LastDate { get; }
        IReadOnlyList<Date> Dates { get; }
    }

    /// <summary>
    /// Base class for financial instruments.
    /// </summary>
    public abstract class Instrument : LazyObject
    {
        protected IPricingEngine _engine;
        protected Real? _npv;
        protected Real? _errorEstimate;
        protected Date _valuationDate;
        protected Dictionary<string, object> _additionalResults = new Dictionary<string, object>();

        public Real NPV
        {
            get
            {
                Calculate();
                if (!_npv.HasValue)
                    throw new InvalidOperationException("NPV not provided");
                return _npv.Value;
            }
        }

        public Real ErrorEstimate
        {
            get
            {
                Calculate();
                return _errorEstimate ?? 0.0;
            }
        }

        public Date ValuationDate
        {
            get
            {
                Calculate();
                return _valuationDate;
            }
        }

        public abstract bool IsExpired { get; }

        public void SetPricingEngine(IPricingEngine e)
        {
            if (_engine != null)
                _engine.UnregisterWith(this);
            _engine = e;
            if (_engine != null)
                _engine.RegisterWith(this);
            Update();
        }

        public virtual void SetupArguments(IPricingEngine.IArguments args)
        {
            throw new NotImplementedException("setupArguments() must be overridden");
        }

        public virtual void FetchResults(IPricingEngine.IResults r)
        {
            // Base implementation - derived classes should override
        }

        protected override void PerformCalculations()
        {
            if (_engine == null)
                throw new InvalidOperationException("No pricing engine provided");

            _npv = null;
            _errorEstimate = null;
            _valuationDate = null;
            _additionalResults.Clear();

            try
            {
                SetupArguments(_engine.GetArguments());
                _engine.GetArguments().Validate();
                _engine.Calculate();
                FetchResults(_engine.GetResults());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Pricing failed: {ex.Message}", ex);
            }
        }
    }

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