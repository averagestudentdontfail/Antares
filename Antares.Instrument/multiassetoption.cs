// C# code for MultiAssetOption.cs

using System;
using Antares.Pattern;
using Antares.Time;

namespace Antares.Instrument
{
    /// <summary>
    /// Base class for options on multiple assets.
    /// </summary>
    public abstract class MultiAssetOption : Option
    {
        // Results fields
        protected Real? _delta;
        protected Real? _gamma;
        protected Real? _theta;
        protected Real? _vega;
        protected Real? _rho;
        protected Real? _dividendRho;

        protected MultiAssetOption(IPayoff payoff, IExercise exercise)
            : base(payoff, exercise) { }

        #region Instrument interface
        public override bool IsExpired
        {
            get
            {
                // Create a temporary event to check if the last exercise date has passed.
                return new SimpleEvent(Exercise.LastDate).HasOccurred();
            }
        }

        protected override void SetupExpired()
        {
            base.SetupExpired();
            _delta = _gamma = _theta = _vega = _rho = _dividendRho = 0.0;
        }

        public override void SetupArguments(IPricingEngine.IArguments args)
        {
            if (args is not Arguments multiAssetArgs)
                throw new ArgumentException($"Wrong argument type. Expected {nameof(Arguments)}, got {args?.GetType().Name ?? "null"}.");

            // Base class setup
            base.SetupArguments(multiAssetArgs);
        }

        public override void FetchResults(IPricingEngine.IResults r)
        {
            base.FetchResults(r);

            if (r is not Results results)
            {
                QL.Ensure(false, "no greeks returned from pricing engine");
                return;
            }

            // Greeks are on the Results object itself due to composition.
            _delta = results.Greeks.Delta;
            _gamma = results.Greeks.Gamma;
            _theta = results.Greeks.Theta;
            _vega = results.Greeks.Vega;
            _rho = results.Greeks.Rho;
            _dividendRho = results.Greeks.DividendRho;
        }
        #endregion

        #region Greeks
        public Real Delta
        {
            get
            {
                Calculate();
                QL.Require(_delta.HasValue, "delta not provided");
                return _delta.Value;
            }
        }

        public Real Gamma
        {
            get
            {
                Calculate();
                QL.Require(_gamma.HasValue, "gamma not provided");
                return _gamma.Value;
            }
        }

        public Real Theta
        {
            get
            {
                Calculate();
                QL.Require(_theta.HasValue, "theta not provided");
                return _theta.Value;
            }
        }

        public Real Vega
        {
            get
            {
                Calculate();
                QL.Require(_vega.HasValue, "vega not provided");
                return _vega.Value;
            }
        }

        public Real Rho
        {
            get
            {
                Calculate();
                QL.Require(_rho.HasValue, "rho not provided");
                return _rho.Value;
            }
        }

        public Real DividendRho
        {
            get
            {
                Calculate();
                QL.Require(_dividendRho.HasValue, "dividend rho not provided");
                return _dividendRho.Value;
            }
        }
        #endregion

        #region Engine and Results
        /// <summary>
        /// Arguments for multi-asset option calculation.
        /// This class is currently identical to the base Option.Arguments.
        /// It is defined for consistency with the C++ API.
        /// </summary>
        public new class Arguments : Option.Arguments { }

        /// <summary>
        /// Results from multi-asset option calculation.
        /// </summary>
        public new class Results : Instrument.Results, ICloneable
        {
            public Greeks Greeks { get; set; } = new Greeks();

            public override void Reset()
            {
                base.Reset();
                Greeks.Reset();
            }

            public object Clone()
            {
                var clone = (Results)this.MemberwiseClone();
                clone.Greeks = (Greeks)this.Greeks.Clone();
                return clone;
            }
        }

        /// <summary>
        /// Generic pricing engine for multi-asset options.
        /// </summary>
        public abstract class Engine : GenericEngine<Arguments, Results> { }
        #endregion
    }
}