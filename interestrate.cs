// Interestrate.cs

using System;
using System.Globalization;
using System.Text;
using QLNet;
using QLNet.Time;
using QLNet.Time.DayCounters;

namespace Antares
{
    /// <summary>
    /// Concrete interest rate class.
    /// </summary>
    /// <remarks>
    /// This class encapsulate the interest rate compounding algebra.
    /// It manages day-counting conventions, compounding conventions,
    /// conversion between different conventions, discount/compound factor
    /// calculations, and implied/equivalent rate calculations.
    /// </remarks>
    public class InterestRate
    {
        private readonly Rate? _rate;
        private readonly DayCounter _dayCounter;
        private readonly Compounding _compounding;
        private readonly bool _freqMakesSense;
        private readonly Real _frequencyValue;

        #region Constructors
        /// <summary>
        /// Default constructor returning a null interest rate.
        /// </summary>
        public InterestRate()
        {
            _rate = null;
        }

        /// <summary>
        /// Standard constructor
        /// </summary>
        public InterestRate(Rate r, DayCounter dc, Compounding comp, Frequency freq)
        {
            _rate = r;
            _dayCounter = dc;
            _compounding = comp;
            _freqMakesSense = false;

            if (_compounding == Compounding.Compounded ||
                _compounding == Compounding.SimpleThenCompounded ||
                _compounding == Compounding.CompoundedThenSimple)
            {
                _freqMakesSense = true;
                if (freq == Frequency.Once || freq == Frequency.NoFrequency)
                    throw new ArgumentException($"{freq} frequency not allowed for this interest rate compounding");

                _frequencyValue = (double)freq;
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// The interest rate value.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the rate is null.</exception>
        public Rate Rate
        {
            get
            {
                if (!_rate.HasValue)
                    throw new InvalidOperationException("null interest rate");
                return _rate.Value;
            }
        }

        /// <summary>
        /// The day counter used for date/time conversion.
        /// </summary>
        public DayCounter DayCounter => _dayCounter;

        /// <summary>
        /// The compounding convention.
        /// </summary>
        public Compounding Compounding => _compounding;

        /// <summary>
        /// The compounding frequency.
        /// </summary>
        public Frequency Frequency => _freqMakesSense ? (Frequency)(int)_frequencyValue : Frequency.NoFrequency;
        #endregion

        #region Conversions
        /// <summary>
        /// Implicit conversion to Rate.
        /// </summary>
        public static implicit operator Rate(InterestRate ir) => ir.Rate;
        #endregion

        #region Discount/compound factor calculations
        /// <summary>
        /// Compound factor implied by the rate compounded at time t.
        /// </summary>
        /// <param name="t">Time must be measured using this InterestRate's own day counter.</param>
        /// <returns>The compound (a.k.a capitalization) factor.</returns>
        public Real CompoundFactor(Time t)
        {
            if (t < 0.0)
                throw new ArgumentException($"negative time ({t}) not allowed");
            if (!_rate.HasValue)
                throw new InvalidOperationException("null interest rate");

            switch (_compounding)
            {
                case Compounding.Simple:
                    return 1.0 + _rate.Value * t;
                case Compounding.Compounded:
                    return Math.Pow(1.0 + _rate.Value / _frequencyValue, _frequencyValue * t);
                case Compounding.Continuous:
                    return Math.Exp(_rate.Value * t);
                case Compounding.SimpleThenCompounded:
                    return t <= 1.0 / _frequencyValue
                        ? 1.0 + _rate.Value * t
                        : Math.Pow(1.0 + _rate.Value / _frequencyValue, _frequencyValue * t);
                case Compounding.CompoundedThenSimple:
                    return t > 1.0 / _frequencyValue
                        ? 1.0 + _rate.Value * t
                        : Math.Pow(1.0 + _rate.Value / _frequencyValue, _frequencyValue * t);
                default:
                    throw new NotSupportedException($"unknown compounding convention: {_compounding}");
            }
        }

        /// <summary>
        /// Compound factor implied by the rate compounded between two dates.
        /// </summary>
        public Real CompoundFactor(Date d1, Date d2, Date refStart = null, Date refEnd = null)
        {
            if (d2 < d1)
                throw new ArgumentException($"d1 ({d1}) later than d2 ({d2})");
            Time t = DayCounter.yearFraction(d1, d2, refStart, refEnd);
            return CompoundFactor(t);
        }

        /// <summary>
        /// Discount factor implied by the rate compounded at time t.
        /// </summary>
        /// <param name="t">Time must be measured using this InterestRate's own day counter.</param>
        public DiscountFactor DiscountFactor(Time t) => 1.0 / CompoundFactor(t);

        /// <summary>
        /// Discount factor implied by the rate compounded between two dates.
        /// </summary>
        public DiscountFactor DiscountFactor(Date d1, Date d2, Date refStart = null, Date refEnd = null)
        {
            if (d2 < d1)
                throw new ArgumentException($"d1 ({d1}) later than d2 ({d2})");
            Time t = DayCounter.yearFraction(d1, d2, refStart, refEnd);
            return DiscountFactor(t);
        }
        #endregion

        #region Implied and equivalent rate calculations
        /// <summary>
        /// Implied interest rate for a given compound factor at a given time.
        /// The resulting InterestRate has the day-counter provided as input.
        /// </summary>
        /// <param name="compound">The compound factor.</param>
        /// <param name="resultDC">The day counter for the resulting rate.</param>
        /// <param name="comp">The compounding for the resulting rate.</param>
        /// <param name="freq">The frequency for the resulting rate.</param>
        /// <param name="t">Time must be measured using the day-counter provided as input.</param>
        public static InterestRate ImpliedRate(Real compound, DayCounter resultDC, Compounding comp, Frequency freq, Time t)
        {
            if (compound <= 0.0)
                throw new ArgumentException("positive compound factor required");

            Rate r;
            if (Math.Abs(compound - 1.0) < QLDefines.EPSILON)
            {
                if (t < 0.0) throw new ArgumentException($"non-negative time ({t}) required");
                r = 0.0;
            }
            else
            {
                if (t <= 0.0) throw new ArgumentException($"positive time ({t}) required");
                double frequencyValue = (double)freq;
                switch (comp)
                {
                    case Compounding.Simple:
                        r = (compound - 1.0) / t;
                        break;
                    case Compounding.Compounded:
                        r = (Math.Pow(compound, 1.0 / (frequencyValue * t)) - 1.0) * frequencyValue;
                        break;
                    case Compounding.Continuous:
                        r = Math.Log(compound) / t;
                        break;
                    case Compounding.SimpleThenCompounded:
                        r = t <= 1.0 / frequencyValue
                            ? (compound - 1.0) / t
                            : (Math.Pow(compound, 1.0 / (frequencyValue * t)) - 1.0) * frequencyValue;
                        break;
                    case Compounding.CompoundedThenSimple:
                        r = t > 1.0 / frequencyValue
                            ? (compound - 1.0) / t
                            : (Math.Pow(compound, 1.0 / (frequencyValue * t)) - 1.0) * frequencyValue;
                        break;
                    default:
                        throw new NotSupportedException($"unknown compounding convention: {comp}");
                }
            }
            return new InterestRate(r, resultDC, comp, freq);
        }

        /// <summary>
        /// Implied rate for a given compound factor between two dates.
        /// The resulting rate is calculated taking the required day-counting rule into account.
        /// </summary>
        public static InterestRate ImpliedRate(Real compound, DayCounter resultDC, Compounding comp, Frequency freq,
                                               Date d1, Date d2, Date refStart = null, Date refEnd = null)
        {
            if (d2 < d1) throw new ArgumentException($"d1 ({d1}) later than d2 ({d2})");
            Time t = resultDC.yearFraction(d1, d2, refStart, refEnd);
            return ImpliedRate(compound, resultDC, comp, freq, t);
        }

        /// <summary>
        /// Equivalent interest rate for a compounding period t.
        /// The resulting InterestRate shares the same implicit day-counting rule of the original InterestRate instance.
        /// </summary>
        /// <param name="comp">The target compounding convention.</param>
        /// <param name="freq">The target frequency.</param>
        /// <param name="t">Time must be measured using this InterestRate's own day counter.</param>
        public InterestRate EquivalentRate(Compounding comp, Frequency freq, Time t) =>
            ImpliedRate(CompoundFactor(t), _dayCounter, comp, freq, t);

        /// <summary>
        /// Equivalent rate for a compounding period between two dates.
        /// The resulting rate is calculated taking the required day-counting rule into account.
        /// </summary>
        public InterestRate EquivalentRate(DayCounter resultDC, Compounding comp, Frequency freq,
                                           Date d1, Date d2, Date refStart = null, Date refEnd = null)
        {
            if (d2 < d1) throw new ArgumentException($"d1 ({d1}) later than d2 ({d2})");
            Time t1 = DayCounter.yearFraction(d1, d2, refStart, refEnd);
            Time t2 = resultDC.yearFraction(d1, d2, refStart, refEnd);
            return ImpliedRate(CompoundFactor(t1), resultDC, comp, freq, t2);
        }
        #endregion

        public override string ToString()
        {
            if (!_rate.HasValue)
                return "null interest rate";

            var sb = new StringBuilder();
            sb.Append($"{_rate.Value:P2} {_dayCounter.name()} ");

            switch (_compounding)
            {
                case Compounding.Simple:
                    sb.Append("simple compounding");
                    break;
                case Compounding.Compounded:
                    sb.Append($"{Frequency.ToEnumString()} compounding");
                    break;
                case Compounding.Continuous:
                    sb.Append("continuous compounding");
                    break;
                case Compounding.SimpleThenCompounded:
                    sb.Append($"simple compounding up to {12 / (int)Frequency} months, then {Frequency.ToEnumString()} compounding");
                    break;
                case Compounding.CompoundedThenSimple:
                    sb.Append($"compounding up to {12 / (int)Frequency} months, then simple compounding");
                    break;
                default:
                    throw new NotSupportedException($"unknown compounding convention: {_compounding}");
            }

            return sb.ToString();
        }
    }
}