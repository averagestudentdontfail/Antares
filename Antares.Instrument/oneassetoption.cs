// C# code for OneAssetOption.cs

using System;
using Antares.Pattern;
using Antares.Time;

namespace Antares.Instrument
{
    /// <summary>
    /// Base class for options on a single asset.
    /// </summary>
    public abstract class OneAssetOption : Option
    {
        // Results fields
        protected Real? _delta;
        protected Real? _deltaForward;
        protected Real? _elasticity;
        protected Real? _gamma;
        protected Real? _theta;
        protected Real? _thetaPerDay;
        protected Real? _vega;
        protected Real? _rho;
        protected Real? _dividendRho;
        protected Real? _strikeSensitivity;
        protected Real? _itmCashProbability;

        protected OneAssetOption(IPayoff payoff, IExercise exercise)
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
            _delta = _deltaForward = _elasticity = _gamma = _theta =
            _thetaPerDay = _vega = _rho = _dividendRho =
            _strikeSensitivity = _itmCashProbability = 0.0;
        }

        public override void FetchResults(IPricingEngine.IResults r)
        {
            base.FetchResults(r);

            // C# pattern matching provides a safe way to check and cast.
            if (r is Greeks greeks)
            {
                _delta = greeks.Delta;
                _gamma = greeks.Gamma;
                _theta = greeks.Theta;
                _vega = greeks.Vega;
                _rho = greeks.Rho;
                _dividendRho = greeks.DividendRho;
            }
            else
            {
                // This mimics the QL_ENSURE check but is more specific.
                // Depending on desired strictness, one could also set greeks to null and check later.
                // For a direct port, ensuring the engine provides the right results is key.
                QL.Ensure(false, "no greeks returned from pricing engine");
            }

            if (r is MoreGreeks moreGreeks)
            {
                _deltaForward = moreGreeks.DeltaForward;
                _elasticity = moreGreeks.Elasticity;
                _thetaPerDay = moreGreeks.ThetaPerDay;
                _strikeSensitivity = moreGreeks.StrikeSensitivity;
                _itmCashProbability = moreGreeks.ItmCashProbability;
            }
            else
            {
                QL.Ensure(false, "no more greeks returned from pricing engine");
            }
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

        public Real DeltaForward
        {
            get
            {
                Calculate();
                QL.Require(_deltaForward.HasValue, "forward delta not provided");
                return _deltaForward.Value;
            }
        }

        public Real Elasticity
        {
            get
            {
                Calculate();
                QL.Require(_elasticity.HasValue, "elasticity not provided");
                return _elasticity.Value;
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

        public Real ThetaPerDay
        {
            get
            {
                Calculate();
                QL.Require(_thetaPerDay.HasValue, "theta per-day not provided");
                return _thetaPerDay.Value;
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

        public Real StrikeSensitivity
        {
            get
            {
                Calculate();
                QL.Require(_strikeSensitivity.HasValue, "strike sensitivity not provided");
                return _strikeSensitivity.Value;
            }
        }

        public Real ItmCashProbability
        {
            get
            {
                Calculate();
                QL.Require(_itmCashProbability.HasValue, "in-the-money cash probability not provided");
                return _itmCashProbability.Value;
            }
        }
        #endregion

        #region Engine and Results
        /// <summary>
        /// Results from single-asset option calculation.
        /// </summary>
        public new class Results : Option.Results, ICloneable
        {
            public Greeks Greeks { get; set; } = new Greeks();
            public MoreGreeks MoreGreeks { get; set; } = new MoreGreeks();

            public override void Reset()
            {
                base.Reset();
                Greeks.Reset();
                MoreGreeks.Reset();
            }

            public object Clone()
            {
                var clone = (Results)this.MemberwiseClone();
                clone.Greeks = (Greeks)this.Greeks.Clone();
                clone.MoreGreeks = (MoreGreeks)this.MoreGreeks.Clone();
                return clone;
            }
        }

        /// <summary>
        /// Generic pricing engine for one-asset options.
        /// </summary>
        public abstract class Engine : GenericEngine<Arguments, Results> { }
        #endregion
    }
}