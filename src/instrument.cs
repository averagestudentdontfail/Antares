// Instrument.cs

using System;
using System.Collections.Generic;

namespace Antares
{
    /// <summary>
    /// Abstract instrument class.
    /// </summary>
    /// <remarks>
    /// This class is purely abstract and defines the interface of concrete
    /// instruments which will be derived from this one.
    /// </remarks>
    public abstract class Instrument : LazyObject
    {
        protected Real? _NPV;
        protected Real? _errorEstimate;
        protected Date? _valuationDate;
        protected Dictionary<string, object> _additionalResults = new Dictionary<string, object>();
        protected IPricingEngine? _engine;

        protected Instrument()
        {
            // Register with evaluation date changes
            Settings.evaluationDate().RegisterWith(this);
        }

        #region Inspectors
        /// <summary>
        /// Returns the net present value of the instrument.
        /// </summary>
        public Real NPV()
        {
            calculate();
            if (!_NPV.HasValue)
                throw new InvalidOperationException("NPV not provided");
            return _NPV.Value;
        }

        /// <summary>
        /// Returns the error estimate on the NPV when available.
        /// </summary>
        public Real errorEstimate()
        {
            calculate();
            if (!_errorEstimate.HasValue)
                throw new InvalidOperationException("error estimate not provided");
            return _errorEstimate.Value;
        }

        /// <summary>
        /// Returns the date the net present value refers to.
        /// </summary>
        public Date valuationDate()
        {
            calculate();
            if (_valuationDate == null)
                throw new InvalidOperationException("valuation date not provided");
            return _valuationDate;
        }

        /// <summary>
        /// Returns any additional result returned by the pricing engine.
        /// </summary>
        public T result<T>(string tag)
        {
            calculate();
            if (_additionalResults.TryGetValue(tag, out var value))
            {
                return (T)value;
            }
            throw new KeyNotFoundException($"{tag} not provided");
        }

        /// <summary>
        /// Returns all additional results returned by the pricing engine.
        /// </summary>
        public Dictionary<string, object> additionalResults()
        {
            calculate();
            return _additionalResults;
        }

        /// <summary>
        /// Returns whether the instrument might have value greater than zero.
        /// </summary>
        public abstract bool isExpired();
        #endregion

        #region Modifiers
        /// <summary>
        /// Set the pricing engine to be used.
        /// </summary>
        /// <remarks>
        /// Calling this method will trigger a recalculation of the instrument value.
        /// </remarks>
        public void setPricingEngine(IPricingEngine engine)
        {
            if (_engine != null)
                _engine.UnregisterWith(this);
            _engine = engine;
            if (_engine != null)
                _engine.RegisterWith(this);
            Update(); // trigger recalculation when engine changes
        }
        #endregion

        #region Engine interface
        /// <summary>
        /// When a derived argument structure is defined for an instrument,
        /// this method should be overridden to fill it.
        /// This is mandatory in case a pricing engine is used.
        /// </summary>
        public virtual void setupArguments(IPricingEngine.IArguments args)
        {
            throw new NotImplementedException("setupArguments() must be overridden");
        }

        /// <summary>
        /// When a derived result structure is defined for an instrument,
        /// this method should be overridden to read from it.
        /// This is mandatory in case a pricing engine is used.
        /// </summary>
        public virtual void fetchResults(IPricingEngine.IResults r)
        {
            var results = r as Results;
            if (results != null)
            {
                _NPV = results.Value;
                _errorEstimate = results.ErrorEstimate;
                _valuationDate = results.ValuationDate;
                _additionalResults = new Dictionary<string, object>(results.AdditionalResults);
            }
            else
            {
                _NPV = null;
                _errorEstimate = null;
                _valuationDate = null;
                _additionalResults.Clear();
            }
        }
        #endregion

        #region LazyObject interface
        /// <summary>
        /// This method performs the actual calculation.
        /// </summary>
        public void calculate()
        {
            if (!_calculated)
            {
                if (isExpired())
                {
                    setupExpired();
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
        protected virtual void setupExpired()
        {
            _NPV = 0.0;
            _errorEstimate = 0.0;
            _valuationDate = new Date();
            _additionalResults.Clear();
        }

        /// <summary>
        /// In case a pricing engine is not used, this method must be overridden to perform
        /// the actual calculations and set any needed results.
        /// In case a pricing engine is used, the default implementation can be used.
        /// </summary>
        protected override void PerformCalculations()
        {
            if (_engine == null)
                throw new InvalidOperationException("null pricing engine");

            _engine.Reset();
            setupArguments(_engine.GetArguments());
            _engine.GetArguments().Validate();
            _engine.Calculate();
            fetchResults(_engine.GetResults());
        }
        #endregion

        /// <summary>
        /// Results returned by instrument pricing.
        /// </summary>
        public class Results : IPricingEngine.IResults
        {
            public Real? Value { get; set; }
            public Real? ErrorEstimate { get; set; }
            public Date? ValuationDate { get; set; }
            public Dictionary<string, object> AdditionalResults { get; private set; } = new Dictionary<string, object>();

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