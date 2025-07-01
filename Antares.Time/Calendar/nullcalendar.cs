// NullCalendar.cs

namespace Antares.Time.Calendar
{
    /// <summary>
    /// Calendar for reproducing theoretical calculations.
    /// This calendar has no holidays. Every day is a business day.
    /// It ensures that dates at whole-month distances have the same day of month.
    /// </summary>
    public sealed class NullCalendar : Antares.Time.Calendar
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NullCalendar"/> class.
        /// </summary>
        public NullCalendar() { }

        /// <summary>
        /// Gets the name of the calendar, which is "Null".
        /// </summary>
        public override string Name => "Null";

        /// <summary>
        /// Determines if the given weekday is a weekend. For NullCalendar, this is always false.
        /// </summary>
        /// <param name="w">The weekday to check.</param>
        /// <returns>Always returns false.</returns>
        public override bool IsWeekend(Weekday w) => false;

        /// <summary>
        /// Determines if the given date is a business day. For NullCalendar, this is always true.
        /// </summary>
        /// <param name="d">The date to check.</param>
        /// <returns>Always returns true.</returns>
        protected override bool IsBusinessDayImpl(Date d) => true;
    }
}