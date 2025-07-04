// Flatforward.cs

using System;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term.Yield
{
    /// <summary>
    /// Flat interest-rate curve.
    /// </summary>
    public class FlatForward : YieldTermStructure
    {
        private readonly Handle<IQuote> _forward;
        private readonly Compounding _compounding;
        private readonly Frequency _frequency;

        // Fields to replicate LazyObject behavior
        private bool _calculated;
        private InterestRate _rate;

        #region Constructors
        public FlatForward(
            Date referenceDate,
            Handle<IQuote> forward,
            DayCounter dayCounter,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.Annual)
            : base(referenceDate, new NullCalendar(), dayCounter)
        {
            _forward = forward;
            _compounding = compounding;
            _frequency = frequency;
            RegisterWith(_forward);
        }

        public FlatForward(
            Date referenceDate,
            Rate forward,
            DayCounter dayCounter,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.Annual)
            : base(referenceDate, new NullCalendar(), dayCounter)
        {
            _forward = new Handle<IQuote>(new SimpleQuote(forward));
            _compounding = compounding;
            _frequency = frequency;
            // No need to register with a static SimpleQuote handle
        }

        public FlatForward(
            int settlementDays,
            Calendar calendar,
            Handle<IQuote> forward,
            DayCounter dayCounter,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.Annual)
            : base(settlementDays, calendar, dayCounter)
        {
            _forward = forward;
            _compounding = compounding;
            _frequency = frequency;
            RegisterWith(_forward);
        }

        public FlatForward(
            int settlementDays,
            Calendar calendar,
            Rate forward,
            DayCounter dayCounter,
            Compounding compounding = Compounding.Continuous,
            Frequency frequency = Frequency.Annual)
            : base(settlementDays, calendar, dayCounter)
        {
            _forward = new Handle<IQuote>(new SimpleQuote(forward));
            _compounding = compounding;
            _frequency = frequency;
        }
        #endregion

        #region Inspectors
        public Compounding Compounding => _compounding;
        public Frequency CompoundingFrequency => _frequency;
        #endregion

        #region TermStructure interface
        public override Date MaxDate => Date.MaxDate;
        #endregion

        #region Observer interface
        /// <summary>
        /// Combines the update logic from LazyObject and YieldTermStructure.
        /// </summary>
        public override void Update()
        {
            // This part is from LazyObject::update()
            _calculated = false;

            // This part is from YieldTermStructure::update()
            base.Update();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Triggers the actual calculations. Replicates LazyObject behavior.
        /// </summary>
        private void Calculate()
        {
            if (!_calculated)
            {
                // No freeze/unfreeze logic needed for now, as it's not used by this class.
                _calculated = true;
                PerformCalculations();
            }
        }

        /// <summary>
        /// Performs the actual calculations, creating the interest rate object.
        /// </summary>
        private void PerformCalculations()
        {
            _rate = new InterestRate(_forward.Value.Value, DayCounter, _compounding, _frequency);
        }
        #endregion

        #region YieldTermStructure implementation
        /// <summary>
        /// Calculates the discount factor for a given time.
        /// </summary>
        protected override DiscountFactor DiscountImpl(Time t)
        {
            Calculate();
            return _rate.DiscountFactor(t);
        }
        #endregion
    }
}