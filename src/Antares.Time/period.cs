// Period.cs

using System;
using System.ComponentModel;
using System.Diagnostics;

namespace Antares.Time
{
    /// <summary>
    /// Frequency of events.
    /// </summary>
    public enum Frequency
    {
        NoFrequency = -1,      //!< null frequency
        Once = 0,              //!< only once, e.g., a zero-coupon
        Annual = 1,            //!< once a year
        Semiannual = 2,        //!< twice a year
        EveryFourthMonth = 3,  //!< every fourth month
        Quarterly = 4,         //!< every third month
        Bimonthly = 6,         //!< every second month
        Monthly = 12,          //!< once a month
        EveryFourthWeek = 13,  //!< every fourth week
        Biweekly = 26,         //!< every second week
        Weekly = 52,           //!< once a week
        Daily = 365,           //!< once a day
        OtherFrequency = 999   //!< some other unknown frequency
    }

    /// <summary>
    /// Represents a period of time defined by a length and a time unit.
    /// This is an immutable struct with value semantics.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public readonly struct Period : IEquatable<Period>, IComparable<Period>
    {
        public int Length { get; }
        public TimeUnit Units { get; }

        #region Constructors
        public Period(int length, TimeUnit units)
        {
            Length = length;
            Units = units;
        }

        public Period(Frequency f)
        {
            switch (f)
            {
                case Frequency.NoFrequency:
                    Units = TimeUnit.Days;
                    Length = 0;
                    break;
                case Frequency.Once:
                    Units = TimeUnit.Years;
                    Length = 0;
                    break;
                case Frequency.Annual:
                    Units = TimeUnit.Years;
                    Length = 1;
                    break;
                case Frequency.Semiannual:
                case Frequency.EveryFourthMonth:
                case Frequency.Quarterly:
                case Frequency.Bimonthly:
                case Frequency.Monthly:
                    Units = TimeUnit.Months;
                    Length = 12 / (int)f;
                    break;
                case Frequency.EveryFourthWeek:
                case Frequency.Biweekly:
                case Frequency.Weekly:
                    Units = TimeUnit.Weeks;
                    Length = 52 / (int)f;
                    break;
                case Frequency.Daily:
                    Units = TimeUnit.Days;
                    Length = 1;
                    break;
                case Frequency.OtherFrequency:
                    QL.Fail("unknown frequency");
                    // The following is unreachable but required by the compiler
                    Units = default;
                    Length = default;
                    break;
                default:
                    QL.Fail($"unknown frequency ({f})");
                    // The following is unreachable but required by the compiler
                    Units = default;
                    Length = default;
                    break;
            }
        }
        #endregion

        #region Public Methods
        public Frequency ToFrequency()
        {
            int length = Math.Abs(Length);
            if (length == 0)
            {
                return Units == TimeUnit.Years ? Frequency.Once : Frequency.NoFrequency;
            }

            switch (Units)
            {
                case TimeUnit.Years:
                    return length == 1 ? Frequency.Annual : Frequency.OtherFrequency;
                case TimeUnit.Months:
                    if (12 % length == 0 && length <= 12)
                        return (Frequency)(12 / length);
                    return Frequency.OtherFrequency;
                case TimeUnit.Weeks:
                    switch (length)
                    {
                        case 1: return Frequency.Weekly;
                        case 2: return Frequency.Biweekly;
                        case 4: return Frequency.EveryFourthWeek;
                        default: return Frequency.OtherFrequency;
                    }
                case TimeUnit.Days:
                    return length == 1 ? Frequency.Daily : Frequency.OtherFrequency;
                default:
                    QL.Fail($"unknown time unit ({Units})");
                    return default; // Unreachable
            }
        }

        public Period Normalized()
        {
            if (Length == 0)
                return new Period(0, TimeUnit.Days);

            int length = this.Length;
            TimeUnit units = this.Units;

            switch (units)
            {
                case TimeUnit.Months:
                    if (length % 12 == 0)
                    {
                        length /= 12;
                        units = TimeUnit.Years;
                    }
                    break;
                case TimeUnit.Days:
                    if (length % 7 == 0)
                    {
                        length /= 7;
                        units = TimeUnit.Weeks;
                    }
                    break;
                case TimeUnit.Weeks:
                case TimeUnit.Years:
                    break; // Already in normalized form
                default:
                    QL.Fail($"unknown time unit ({units})");
                    break;
            }
            return new Period(length, units);
        }

        public string ToLongString()
        {
            string s = Length == 1 ? "" : "s";
            return $"{Length} {Units.ToString().ToLowerInvariant()}{s}";
        }

        public string ToShortString()
        {
            return Units switch
            {
                TimeUnit.Days => $"{Length}D",
                TimeUnit.Weeks => $"{Length}W",
                TimeUnit.Months => $"{Length}M",
                TimeUnit.Years => $"{Length}Y",
                _ => throw new InvalidOperationException($"unknown time unit ({Units})")
            };
        }

        public override string ToString() => ToShortString();
        #endregion

        #region Operators
        public static Period operator -(Period p) => new Period(-p.Length, p.Units);

        public static Period operator +(Period p1, Period p2)
        {
            if (p1.Length == 0) return p2;
            if (p2.Length == 0) return p1;
            if (p1.Units == p2.Units) return new Period(p1.Length + p2.Length, p1.Units);

            return (p1.Units, p2.Units) switch
            {
                (TimeUnit.Years, TimeUnit.Months) => new Period(p1.Length * 12 + p2.Length, TimeUnit.Months),
                (TimeUnit.Months, TimeUnit.Years) => new Period(p1.Length + p2.Length * 12, TimeUnit.Months),
                (TimeUnit.Weeks, TimeUnit.Days) => new Period(p1.Length * 7 + p2.Length, TimeUnit.Days),
                (TimeUnit.Days, TimeUnit.Weeks) => new Period(p1.Length + p2.Length * 7, TimeUnit.Days),
                (_, _) when p2.Length == 0 => p1,
                (_, _) when p1.Length == 0 => p2,
                _ => throw new ArgumentException($"impossible addition between {p1} and {p2}")
            };
        }

        public static Period operator -(Period p1, Period p2) => p1 + (-p2);

        public static Period operator *(Period p, int n) => new Period(p.Length * n, p.Units);
        public static Period operator *(int n, Period p) => new Period(p.Length * n, p.Units);

        public static Period operator /(Period p, int n)
        {
            QL.Require(n != 0, "cannot be divided by zero");
            if (p.Length % n == 0)
            {
                return new Period(p.Length / n, p.Units);
            }

            TimeUnit units = p.Units;
            int length = p.Length;

            switch (units)
            {
                case TimeUnit.Years:
                    length *= 12;
                    units = TimeUnit.Months;
                    break;
                case TimeUnit.Weeks:
                    length *= 7;
                    units = TimeUnit.Days;
                    break;
            }

            QL.Require(length % n == 0, $"{p} cannot be divided by {n}");
            return new Period(length / n, units);
        }
        #endregion

        #region Comparisons
        public static bool operator <(Period p1, Period p2)
        {
            if (p1.Length == 0) return p2.Length > 0;
            if (p2.Length == 0) return p1.Length < 0;

            if (p1.Units == p2.Units) return p1.Length < p2.Length;
            if (p1.Units == TimeUnit.Months && p2.Units == TimeUnit.Years) return p1.Length < 12 * p2.Length;
            if (p1.Units == TimeUnit.Years && p2.Units == TimeUnit.Months) return 12 * p1.Length < p2.Length;
            if (p1.Units == TimeUnit.Days && p2.Units == TimeUnit.Weeks) return p1.Length < 7 * p2.Length;
            if (p1.Units == TimeUnit.Weeks && p2.Units == TimeUnit.Days) return 7 * p1.Length < p2.Length;

            (int p1Min, int p1Max) = DaysMinMax(p1);
            (int p2Min, int p2Max) = DaysMinMax(p2);

            if (p1Max < p2Min) return true;
            if (p1Min > p2Max) return false;
            QL.Fail($"undecidable comparison between {p1} and {p2}");
            return false; // Unreachable
        }

        private static (int min, int max) DaysMinMax(Period p)
        {
            return p.Units switch
            {
                TimeUnit.Days => (p.Length, p.Length),
                TimeUnit.Weeks => (7 * p.Length, 7 * p.Length),
                TimeUnit.Months => (28 * p.Length, 31 * p.Length),
                TimeUnit.Years => (365 * p.Length, 366 * p.Length),
                _ => throw new InvalidOperationException($"unknown time unit ({p.Units})")
            };
        }

        public static bool operator >(Period p1, Period p2) => p2 < p1;
        public static bool operator <=(Period p1, Period p2) => !(p1 > p2);
        public static bool operator >=(Period p1, Period p2) => !(p1 < p2);
        public static bool operator ==(Period p1, Period p2) => !(p1 < p2 || p2 < p1);
        public static bool operator !=(Period p1, Period p2) => !(p1 == p2);

        public bool Equals(Period other) => this == other;
        public override bool Equals(object? obj) => obj is Period other && this.Equals(other);
        public override int GetHashCode() => HashCode.Combine(Length, Units);
        public int CompareTo(Period other)
        {
            if (this < other) return -1;
            if (this > other) return 1;
            return 0;
        }
        #endregion
    }
}