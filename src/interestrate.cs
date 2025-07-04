// Interestrate.cs

using System;
using System.Text;

namespace Antares
{
    #region Supporting Types

    /// <summary>
    /// Frequency enumeration.
    /// </summary>
    public enum Frequency
    {
        NoFrequency = -1,     //!< null frequency
        Once = 0,             //!< only once, e.g., a zero-coupon
        Annual = 1,           //!< once a year
        Semiannual = 2,       //!< twice a year
        EveryFourthMonth = 3, //!< every fourth month
        Quarterly = 4,        //!< every third month
        Bimonthly = 6,        //!< every second month
        Monthly = 12,         //!< once a month
        EveryFourthWeek = 13, //!< every fourth week
        Biweekly = 26,        //!< every second week
        Weekly = 52,          //!< once a week
        Daily = 365,          //!< once a day
        OtherFrequency = 999  //!< some other unknown frequency
    }

    /// <summary>
    /// Provides extension methods for the Frequency enum.
    /// </summary>
    public static class FrequencyExtensions
    {
        /// <summary>
        /// Returns a string representation of the frequency.
        /// </summary>
        public static string ToEnumString(this Frequency f)
        {
            if (Enum.IsDefined(typeof(Frequency), f))
            {
                return f.ToString();
            }
            throw new ArgumentException($"Unknown frequency: {(int)f}");
        }
    }

    /// <summary>
    /// Abstract base class for day counter implementations.
    /// </summary>
    public abstract class DayCounter : IEquatable<DayCounter>
    {
        public abstract string name();
        
        public virtual int dayCount(Date d1, Date d2)
        {
            return Math.Abs((d2.CompareTo(d1)));
        }
        
        public abstract double yearFraction(Date d1, Date d2, Date? refStart = null, Date? refEnd = null);
        
        public bool empty() => string.IsNullOrEmpty(name());
        
        public bool Equals(DayCounter? other)
        {
            if (other is null) return false;
            if (this.empty() && other.empty()) return true;
            return this.name() == other.name();
        }

        public override bool Equals(object? obj) => obj is DayCounter other && this.Equals(other);
        public override int GetHashCode() => name()?.GetHashCode() ?? 0;
        public override string ToString() => name() ?? "No day counter implementation provided";
        
        public static bool operator ==(DayCounter d1, DayCounter d2)
        {
            if (d1 is null) return d2 is null;
            return d1.Equals(d2);
        }
        public static bool operator !=(DayCounter d1, DayCounter d2) => !(d1 == d2);
    }

    /// <summary>
    /// Actual/365 Fixed day counter.
    /// </summary>
    public class Actual365Fixed : DayCounter
    {
        public override string name() => "Actual/365 (Fixed)";
        
        public override double yearFraction(Date d1, Date d2, Date? refStart = null, Date? refEnd = null)
        {
            return dayCount(d1, d2) / 365.0;
        }
    }

    #endregion

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
        public Rate rate()
        {
            if (!_rate.HasValue)
                throw new InvalidOperationException("null interest rate");
            return _rate.Value;
        }

        /// <summary>
        /// The day counter used for date/time conversion.
        /// </summary>
        public DayCounter dayCounter() => _dayCounter;

        /// <summary>
        /// The compounding convention.
        /// </summary>
        public Compounding compounding() => _compounding;

        /// <summary>
        /// The compounding frequency.
        /// </summary>
        public Frequency frequency() => _freqMakesSense ? (Frequency)(int)_frequencyValue : Frequency.NoFrequency;
        #endregion

        #region Conversions
        /// <summary>
        /// Implicit conversion to Rate.
        /// </summary>
        public static implicit operator Rate(InterestRate ir) => ir.rate();
        #endregion

        #region Discount/compound factor calculations
        /// <summary>
        /// Discount factor implied by the rate compounded between two dates.
        /// </summary>
        public DiscountFactor discountFactor(Date d1, Date d2)
        {
            if (!_rate.HasValue)
                throw new InvalidOperationException("null interest rate");
            return discountFactor(_dayCounter.yearFraction(d1, d2));
        }

        /// <summary>
        /// Discount factor implied by the rate compounded for a given time.
        /// </summary>
        public DiscountFactor discountFactor(Time t)
        {
            return 1.0 / compoundFactor(t);
        }

        /// <summary>
        /// Compound factor implied by the rate compounded between two dates.
        /// </summary>
        public Real compoundFactor(Date d1, Date d2)
        {
            if (!_rate.HasValue)
                throw new InvalidOperationException("null interest rate");
            return compoundFactor(_dayCounter.yearFraction(d1, d2));
        }

        /// <summary>
        /// Compound factor implied by the rate compounded for a given time.
        /// </summary>
        public Real compoundFactor(Time t)
        {
            if (!_rate.HasValue)
                throw new InvalidOperationException("null interest rate");

            QL.Require(t >= 0.0, "negative time not allowed");

            switch (_compounding)
            {
                case Compounding.Simple:
                    return 1.0 + _rate.Value * t;
                case Compounding.Compounded:
                    return Math.Pow(1.0 + _rate.Value / _frequencyValue, _frequencyValue * t);
                case Compounding.Continuous:
                    return Math.Exp(_rate.Value * t);
                case Compounding.SimpleThenCompounded:
                    if (t <= 1.0 / _frequencyValue)
                        return 1.0 + _rate.Value * t;
                    else
                        return Math.Pow(1.0 + _rate.Value / _frequencyValue, _frequencyValue * t);
                case Compounding.CompoundedThenSimple:
                    if (t > 1.0 / _frequencyValue)
                        return 1.0 + _rate.Value * t;
                    else
                        return Math.Pow(1.0 + _rate.Value / _frequencyValue, _frequencyValue * t);
                default:
                    QL.Fail($"Unknown compounding convention ({_compounding})");
                    return 0.0; // Never reached
            }
        }
        #endregion

        #region Equivalent rate calculations
        /// <summary>
        /// Equivalent interest rate for a different compounding convention.
        /// </summary>
        public InterestRate equivalentRate(Compounding compounding, Frequency frequency, Time t)
        {
            return impliedRate(compoundFactor(t), t, _dayCounter, compounding, frequency);
        }

        /// <summary>
        /// Equivalent interest rate for a different day count convention.
        /// </summary>
        public InterestRate equivalentRate(DayCounter dayCounter, Compounding compounding, Frequency frequency, Date d1, Date d2)
        {
            Time t1 = _dayCounter.yearFraction(d1, d2);
            Time t2 = dayCounter.yearFraction(d1, d2);
            return impliedRate(compoundFactor(t1), t2, dayCounter, compounding, frequency);
        }
        #endregion

        #region Static utility methods
        /// <summary>
        /// Implied interest rate for a given compound factor at a given time.
        /// </summary>
        public static InterestRate impliedRate(Real compound, Time t, DayCounter dayCounter, Compounding compounding, Frequency frequency = Frequency.Annual)
        {
            QL.Require(compound > 0.0, "positive compound factor required");

            Rate r;
            if (compound == 1.0)
            {
                QL.Require(t >= 0.0, "non negative time (" + t + ") required");
                r = 0.0;
            }
            else
            {
                QL.Require(t > 0.0, "positive time (" + t + ") required");
                switch (compounding)
                {
                    case Compounding.Simple:
                        r = (compound - 1.0) / t;
                        break;
                    case Compounding.Compounded:
                        r = (Math.Pow(compound, 1.0 / ((double)frequency * t)) - 1.0) * (double)frequency;
                        break;
                    case Compounding.Continuous:
                        r = Math.Log(compound) / t;
                        break;
                    case Compounding.SimpleThenCompounded:
                        if (t <= 1.0 / (double)frequency)
                            r = (compound - 1.0) / t;
                        else
                            r = (Math.Pow(compound, 1.0 / ((double)frequency * t)) - 1.0) * (double)frequency;
                        break;
                    case Compounding.CompoundedThenSimple:
                        if (t > 1.0 / (double)frequency)
                            r = (compound - 1.0) / t;
                        else
                            r = (Math.Pow(compound, 1.0 / ((double)frequency * t)) - 1.0) * (double)frequency;
                        break;
                    default:
                        QL.Fail($"Unknown compounding convention ({compounding})");
                        r = 0.0; // Never reached
                        break;
                }
            }

            return new InterestRate(r, dayCounter, compounding, frequency);
        }

        /// <summary>
        /// Implied interest rate for a given compound factor between two dates.
        /// </summary>
        public static InterestRate impliedRate(Real compound, Date d1, Date d2, DayCounter dayCounter, Compounding compounding, Frequency frequency = Frequency.Annual)
        {
            QL.Require(d1 <= d2, "d1 (" + d1 + ") later than d2 (" + d2 + ")");
            Time t = dayCounter.yearFraction(d1, d2);
            return impliedRate(compound, t, dayCounter, compounding, frequency);
        }
        #endregion

        #region String representation
        /// <summary>
        /// Returns a string representation of the interest rate.
        /// </summary>
        public override string ToString()
        {
            if (!_rate.HasValue)
                return "null interest rate";

            var result = new StringBuilder();
            result.AppendFormat("{0:F6} % ", _rate.Value * 100);
            result.Append(_dayCounter.name());
            result.Append(" ");
            result.Append(_compounding.ToEnumString());

            if (_freqMakesSense)
            {
                result.Append(" compounding");
                switch (_compounding)
                {
                    case Compounding.SimpleThenCompounded:
                    case Compounding.CompoundedThenSimple:
                        result.AppendFormat(" {0} frequency", frequency().ToEnumString());
                        break;
                    case Compounding.Compounded:
                        result.AppendFormat(" {0} frequency", frequency().ToEnumString());
                        break;
                }
            }

            return result.ToString();
        }
        #endregion
    }
}