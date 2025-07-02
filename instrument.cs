// Instrument.cs

using System;
using System.Collections.Generic;
using QLNet;
using QLNet.Time;
using Antares.Pattern;

namespace Antares
{
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
        /// <remarks>
        /// Calling this method will trigger a recalculation of the instrument value.
        /// </remarks>
        public void SetPricingEngine(IPricingEngine e)
        {
            if (_engine != null)
                _engine.UnregisterWith(this);
            _engine = e;
            if (_engine != null)
                _engine.RegisterWith(this);
            Update(); // trigger recalculation when engine changes
        }
        #endregion

        #region Results
        /// <summary>
        /// When a derived instrument defines additional results, it should call this method to notify the base class.
        /// </summary>
        protected void SetupArguments(IPricingEngine.IArguments args)
        {
            throw new NotImplementedException("setupArguments() must be overridden");
        }

        /// <summary>
        /// When a derived instrument defines additional results, it should call this method to notify the base class.
        /// </summary>
        protected void FetchResults(IPricingEngine.IResults r)
        {
            var results = r as Instrument.Results;
            if (results != null)
            {
                _npv = results.Value;
                _errorEstimate = results.ErrorEstimate;
                _valuationDate = results.ValuationDate;
                _additionalResults = new Dictionary<string, object>(results.AdditionalResults);
            }
            else
            {
                _npv = null;
                _errorEstimate = null;
                _valuationDate = null;
                _additionalResults.Clear();
            }
        }
        #endregion

        #region LazyObject interface
        /// <summary>
        /// This method must be implemented in derived classes to provide the specific calculations.
        /// In the base Instrument class, it delegates to the pricing engine.
        /// </summary>
        protected override void PerformCalculations()
        {
            if (_engine == null)
                throw new InvalidOperationException("No pricing engine provided");

            // Reset our previous results
            _npv = null;
            _errorEstimate = null;
            _valuationDate = null;
            _additionalResults.Clear();

            // Set up the engine's arguments and calculate
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
        #endregion

        #region Engine interface
        /// <summary>
        /// Arguments required for instrument pricing.
        /// </summary>
        public class Arguments : IPricingEngine.IArguments
        {
            public virtual void Validate()
            {
                // Base implementation does nothing.
                // Derived classes should override this to validate their specific arguments.
            }
        }

        /// <summary>
        /// Results returned by instrument pricing.
        /// </summary>
        public class Results : IPricingEngine.IResults
        {
            public double? Value { get; set; }
            public double? ErrorEstimate { get; set; }
            public Date ValuationDate { get; set; }
            public Dictionary<string, object> AdditionalResults { get; private set; } = new Dictionary<string, object>();

            public virtual void Reset()
            {
                Value = null;
                ErrorEstimate = null;
                ValuationDate = null;
                AdditionalResults.Clear();
            }
        }
        #endregion
    }
}