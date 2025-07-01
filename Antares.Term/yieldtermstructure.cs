// Yieldtermstructure.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term
{
    /// <summary>
    /// Interest-rate term structure.
    /// This abstract class defines the interface of concrete interest rate structures.
    /// </summary>
    public abstract class YieldTermStructure : TermStructure
    {
        private const double dt = 0.0001; // time interval used in finite differences

        private readonly List<Handle<IQuote>> _jumps;
        private List<Date> _jumpDates;
        private readonly List<Time> _jumpTimes;
        private Date _latestReference;

        #region Constructors
        protected YieldTermStructure(DayCounter dc = null) : base(dc)
        {
            _jumps = new List<Handle<IQuote>>();
            _jumpDates = new List<Date>();
            _jumpTimes = new List<Time>();
        }

        protected YieldTermStructure(Date referenceDate, Calendar cal = null, DayCounter dc = null,
                                     List<Handle<IQuote>> jumps = null, List<Date> jumpDates = null)
            : base(referenceDate, cal, dc)
        {
            _jumps = jumps ?? new List<Handle<IQuote>>();
            _jumpDates = jumpDates ?? new List<Date>();
            _jumpTimes = new List<Time>(_jumps.Count);
            InitializeJumps();
        }

        protected YieldTermStructure(int settlementDays, Calendar cal, DayCounter dc = null,
                                     List<Handle<IQuote>> jumps = null, List<Date> jumpDates = null)
            : base((uint)settlementDays, cal, dc)
        {
            _jumps = jumps ?? new List<Handle<IQuote>>();
            _jumpDates = jumpDates ?? new List<Date>();
            _jumpTimes = new List<Time>(_jumps.Count);
            InitializeJumps();
        }

        private void InitializeJumps()
        {
            SetJumps(ReferenceDate);
            foreach (var jump in _jumps)
            {
                jump.RegisterWith(this);
            }
        }
        #endregion

        #region Discount factors
        /// <summary>
        /// Returns the discount factor from a given date to the reference date.
        /// </summary>
        public DiscountFactor Discount(Date d, bool extrapolate = false)
        {
            return Discount(TimeFromReference(d), extrapolate);
        }

        /// <summary>
        /// Returns the discount factor for a given time.
        /// </summary>
        /// <remarks>The same day-counting rule used by the term structure should be used for calculating the passed time t.</remarks>
        public DiscountFactor Discount(Time t, bool extrapolate = false)
        {
            CheckRange(t, extrapolate);

            if (_jumps.Count == 0)
                return DiscountImpl(t);

            double jumpEffect = 1.0;
            for (int i = 0; i < _jumps.Count; ++i)
            {
                if (_jumpTimes[i] > 0 && _jumpTimes[i] < t)
                {
                    QL.Require(!_jumps[i].IsEmpty && _jumps[i].Value.IsValid, $"invalid {Utility.DataFormatters.ToOrdinal(i + 1)} jump quote");
                    double thisJump = _jumps[i].Value.Value;
                    QL.Require(thisJump > 0.0, $"invalid {Utility.DataFormatters.ToOrdinal(i + 1)} jump value: {thisJump}");
                    jumpEffect *= thisJump;
                }
            }
            return jumpEffect * DiscountImpl(t);
        }
        #endregion

        #region Zero-yield rates
        /// <summary>
        /// The resulting interest rate has the required day-counting rule.
        /// </summary>
        public InterestRate ZeroRate(Date d, DayCounter resultDayCounter, Compounding comp, Frequency freq = Frequency.Annual, bool extrapolate = false)
        {
            Time t = TimeFromReference(d);
            if (Math.Abs(t) < dt / 10.0) // Check if t is effectively zero
            {
                double compound = 1.0 / Discount(dt, extrapolate);
                return InterestRate.ImpliedRate(compound, resultDayCounter, comp, freq, dt);
            }
            double compoundFactor = 1.0 / Discount(t, extrapolate);
            return InterestRate.ImpliedRate(compoundFactor, resultDayCounter, comp, freq, ReferenceDate, d);
        }

        /// <summary>
        /// The resulting interest rate has the same day-counting rule used by the term structure.
        /// The same rule should be used for calculating the passed time t.
        /// </summary>
        public InterestRate ZeroRate(Time t, Compounding comp, Frequency freq = Frequency.Annual, bool extrapolate = false)
        {
            if (Math.Abs(t) < dt / 10.0) t = dt;
            double compound = 1.0 / Discount(t, extrapolate);
            return InterestRate.ImpliedRate(compound, DayCounter, comp, freq, t);
        }
        #endregion

        #region Forward rates
        public InterestRate ForwardRate(Date d1, Date d2, DayCounter resultDayCounter, Compounding comp, Frequency freq = Frequency.Annual, bool extrapolate = false)
        {
            if (d1 == d2)
            {
                CheckRange(d1, extrapolate);
                Time t1 = Math.Max(TimeFromReference(d1) - dt / 2.0, 0.0);
                Time t2 = t1 + dt;
                double compound = Discount(t1, true) / Discount(t2, true);
                return InterestRate.ImpliedRate(compound, resultDayCounter, comp, freq, dt);
            }

            QL.Require(d1 < d2, $"{d1} later than {d2}");
            double compoundFactor = Discount(d1, extrapolate) / Discount(d2, extrapolate);
            return InterestRate.ImpliedRate(compoundFactor, resultDayCounter, comp, freq, d1, d2);
        }

        public InterestRate ForwardRate(Date d, Period p, DayCounter resultDayCounter, Compounding comp, Frequency freq = Frequency.Annual, bool extrapolate = false)
        {
            return ForwardRate(d, d + p, resultDayCounter, comp, freq, extrapolate);
        }

        public InterestRate ForwardRate(Time t1, Time t2, Compounding comp, Frequency freq = Frequency.Annual, bool extrapolate = false)
        {
            double compound;
            if (Math.Abs(t2 - t1) < dt / 10.0)
            {
                CheckRange(t1, extrapolate);
                t1 = Math.Max(t1 - dt / 2.0, 0.0);
                t2 = t1 + dt;
                compound = Discount(t1, true) / Discount(t2, true);
            }
            else
            {
                QL.Require(t2 > t1, $"t2 ({t2}) < t1 ({t1})");
                compound = Discount(t1, extrapolate) / Discount(t2, extrapolate);
            }
            return InterestRate.ImpliedRate(compound, DayCounter, comp, freq, t2 - t1);
        }
        #endregion

        #region Jump inspectors
        public IReadOnlyList<Date> JumpDates => _jumpDates;
        public IReadOnlyList<Time> JumpTimes => _jumpTimes;
        #endregion

        #region Observer interface
        public override void Update()
        {
            base.Update();
            Date newReference = default;
            try
            {
                newReference = ReferenceDate;
                if (newReference != _latestReference)
                {
                    SetJumps(newReference);
                }
            }
            catch (Exception)
            {
                if (newReference == default)
                {
                    // Absorb exception if reference date couldn't be calculated.
                    // Jumps will be set correctly when a valid underlying is set.
                    return;
                }
                // Something else happened, rethrow.
                throw;
            }
        }
        #endregion

        #region Calculations
        /// <summary>
        /// This method must be implemented in derived classes to perform the actual calculations.
        /// When it is called, range check has already been performed; therefore, it
        /// must assume that extrapolation is required.
        /// </summary>
        protected abstract DiscountFactor DiscountImpl(Time t);
        #endregion

        private void SetJumps(Date referenceDate)
        {
            if (_jumpDates.Count == 0 && _jumps.Count > 0)
            { // Turn of year dates
                _jumpDates = new List<Date>(_jumps.Count);
                int year = referenceDate.Year;
                for (int i = 0; i < _jumps.Count; ++i)
                    _jumpDates.Add(new Date(31, Month.December, year + i));
            }
            else
            {
                QL.Require(_jumpDates.Count == _jumps.Count,
                    $"mismatch between number of jumps ({_jumps.Count}) and jump dates ({_jumpDates.Count})");
            }

            _jumpTimes.Clear();
            for (int i = 0; i < _jumps.Count; ++i)
                _jumpTimes.Add(TimeFromReference(_jumpDates[i]));

            _latestReference = referenceDate;
        }
    }
}