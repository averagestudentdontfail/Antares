// Daycounter.cs

using System;

namespace Antares.Time
{
    /// <summary>
    /// Day counter class.
    /// </summary>
    /// <remarks>
    /// This class provides methods for determining the length of a time period
    /// according to a given market convention, both as a number of days and as a
    /// year fraction.
    ///
    /// This is an abstract base class. Concrete implementations must derive from it.
    /// </remarks>
    public abstract class DayCounter : IEquatable<DayCounter>
    {
        #region Abstract Interface
        /// <summary>
        /// The name of the day counter.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Returns the number of days between two dates.
        /// </summary>
        /// <remarks>
        /// The default implementation returns the difference in serial numbers,
        /// which is correct for most day counters. It can be overridden by more
        /// complex day counters if necessary.
        /// </remarks>
        public virtual int DayCount(Date d1, Date d2)
        {
            return d2 - d1;
        }

        /// <summary>
        /// Returns the period between two dates as a fraction of a year.
        /// </summary>
        /// <param name="d1">The start date.</param>
        /// <param name="d2">The end date.</param>
        /// <param name="refPeriodStart">An optional reference-period start date.</param>
        /// <param name="refPeriodEnd">An optional reference-period end date.</param>
        /// <returns>The year fraction.</returns>
        public abstract double YearFraction(Date d1, Date d2, Date? refPeriodStart = null, Date? refPeriodEnd = null);
        #endregion

        #region Public Interface
        /// <summary>
        /// Returns whether the day counter is uninitialized.
        /// An uninitialized day counter has a null or empty name.
        /// </summary>
        public bool IsEmpty() => string.IsNullOrEmpty(Name);
        #endregion

        #region Equality and Formatting
        /// <summary>
        /// Checks for equality based on the day counter's name.
        /// </summary>
        public bool Equals(DayCounter other)
        {
            if (other is null) return false;
            // Two uninitialized day counters are considered equal.
            if (this.IsEmpty() && other.IsEmpty()) return true;
            return this.Name == other.Name;
        }

        public override bool Equals(object obj) => obj is DayCounter other && this.Equals(other);

        public override int GetHashCode() => Name?.GetHashCode() ?? 0;

        public override string ToString() => Name ?? "No day counter implementation provided";

        public static bool operator ==(DayCounter d1, DayCounter d2)
        {
            if (d1 is null) return d2 is null;
            return d1.Equals(d2);
        }

        public static bool operator !=(DayCounter d1, DayCounter d2) => !(d1 == d2);
        #endregion
    }
}