// Instrument.cs

using System;
using System.Collections.Generic;
using QLNet;
using QLNet.Time;

namespace Antares
{
    #region Supporting Infrastructure (Placeholders)
    // This infrastructure is included to make the file self-contained and compilable.
    // In a real project, these would be in their own files.

    /// <summary>
    /// Base class for objects that can be calculated lazily.
    /// This is a C# translation of QuantLib's LazyObject.
    /// </summary>
    public abstract class LazyObject : IObserver, IObservable
    {
        private readonly Observable _observable = new Observable();
        [ThreadStatic]
        private static bool _frozen;
        [ThreadStatic]
        private static bool _triggerNotifications;

        protected bool _calculated;

        protected LazyObject()
        {
            // In C#, there's no need to explicitly register with Settings.EvaluationDate
            // if the object doesn't have a moving reference date.
            // Derived classes like TermStructure will handle this.
            // For a general Instrument, this is handled by registering with its engine.
        }

        public virtual void Update()
        {
            _calculated = false;
            if (_frozen)
            {
                _triggerNotifications = true;
            }
            else
            {
                NotifyObservers();
            }
        }

        public virtual void Calculate()
        {
            if (!_calculated && !_frozen)
            {
                _calculated = true; // prevent infinite recursion
                try
                {
                    PerformCalculations();
                }
                catch
                {
                    _calculated = false; // allow recalculation
                    throw;
                }
            }
        }

        protected abstract void PerformCalculations();

        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
    }

    #endregion

    /// <summary>
    /// Abstract instrument class.
    /// </summary>
    public abstract class Instrument : LazyObject
    {
        protected double? _npv;
        protected double? _errorEstimate;
        protected Date _valuationDate;
        protected Dictionary<string, object> _additionalResults = new Dictionary<string, object>();
        protected IPricingEngine _engine;

        protected Instrument()
        {
            // The original C++ code registers with Settings::instance().evaluationDate().
            // This is implicitly handled in C# as any term structure used by the pricing engine
            // will be observing the evaluation date. When the term structure is updated,
            // it notifies its observers, which includes the engine, which in turn notifies the instrument.
        }

        #region Inspectors
        /// <summary>
        /// Returns the net present value of the instrument.
        /// </summary>
        public double NPV
        {
            get
            {
                Calculate();
                if (!_npv.HasValue)
                    throw new InvalidOperationException("NPV not provided");
                return _npv.Value;
            }
        }

        /// <summary>
        /// Returns the error estimate on the NPV when available.
        /// </summary>
        public double ErrorEstimate
        {
            get
            {
                Calculate();
                if (!_errorEstimate.HasValue)
                    throw new InvalidOperationException("Error estimate not provided");
                return _errorEstimate.Value;
            }
        }

        /// <summary>
        /// Returns the date the net present value refers to.
        /// </summary>
        public Date ValuationDate
        {
            get
            {
                Calculate();
                if (_valuationDate == null || _valuationDate == new Date())
                    throw new InvalidOperationException("Valuation date not provided");
                return _valuationDate;
            }
        }

        /// <summary>
        /// Returns any additional result returned by the pricing engine.
        /// </summary>
        public T Result<T>(string tag)
        {
            Calculate();
            if (_additionalResults.TryGetValue(tag, out var value))
            {
                return (T)value;
            }
            throw new KeyNotFoundException($"{tag} not provided");
        }

        /// <summary>
        /// Returns all additional results returned by the pricing engine.
        /// </summary>
        public IReadOnlyDictionary<string, object> AdditionalResults
        {
            get
            {
                Calculate();
                return _additionalResults;
            }
        }

        /// <summary>
        /// Returns whether the instrument might have value greater than zero.
        /// </summary>
        public abstract bool IsExpired { get; }
        #endregion

        #region Modifiers
        /// <summary>
        /// Sets the pricing engine to be used.
        /// </summary>
        public void SetPricingEngine(IPricingEngine engine)
        {
            if (_engine != null)
                _engine.UnregisterWith(this);

            _engine = engine;

            if (_engine != null)
                _engine.RegisterWith(this);

            // Trigger (lazy) recalculation and notify observers
            Update();
        }
        #endregion

        /// <summary>
        /// When a derived argument structure is defined for an instrument, this method
        /// should be overridden to fill it. This is mandatory if a pricing engine is used.
        /// </summary>
        public virtual void SetupArguments(IPricingEngine.IArguments args)
        {
            throw new NotImplementedException("Instrument.SetupArguments() must be implemented in a derived class.");
        }

        /// <summary>
        /// When a derived result structure is defined for an instrument, this method
        /// should be overridden to read from it. This is mandatory if a pricing engine is used.
        /// </summary>
        public virtual void FetchResults(IPricingEngine.IResults r)
        {
            if (r is not Results results)
                throw new InvalidOperationException("No results returned from pricing engine or wrong result type.");

            _npv = results.Value;
            _errorEstimate = results.ErrorEstimate;
            _valuationDate = results.ValuationDate;
            _additionalResults = results.AdditionalResults ?? new Dictionary<string, object>();
        }

        #region Calculations
        public override void Calculate()
        {
            if (!_calculated)
            {
                if (IsExpired)
                {
                    SetupExpired();
                    _calculated = true;
                }
                else
                {
                    base.Calculate();
                }
            }
        }

        /// <summary>
        /// This method must leave the instrument in a consistent state when the expiration condition is met.
        /// </summary>
        protected virtual void SetupExpired()
        {
            _npv = 0.0;
            _errorEstimate = 0.0;
            _valuationDate = null;
            _additionalResults.Clear();
        }

        /// <summary>
        /// If a pricing engine is used, this method performs the actual calculations.
        /// </summary>
        protected override void PerformCalculations()
        {
            if (_engine == null)
                throw new InvalidOperationException("Null pricing engine.");

            _engine.Reset();

            var arguments = _engine.GetArguments();
            SetupArguments(arguments);
            arguments.Validate();

            _engine.Calculate();

            var results = _engine.GetResults();
            FetchResults(results);
        }
        #endregion

        /// <summary>
        /// Base class for instrument results.
        /// </summary>
        public class Results : IPricingEngine.IResults
        {
            public double? Value { get; set; }
            public double? ErrorEstimate { get; set; }
            public Date ValuationDate { get; set; }
            public Dictionary<string, object> AdditionalResults { get; set; } = new Dictionary<string, object>();

            public virtual void Reset()
            {
                Value = null;
                ErrorEstimate = null;
                ValuationDate = null;
                AdditionalResults.Clear();
            }
        }
    }
}