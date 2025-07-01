// Actual365fixed.cs

using System;

namespace Antares.Time.Day
{
    /// <summary>
    /// "Actual/365 (Fixed)" day count convention.
    /// </summary>
    /// <remarks>
    /// Also known as "Act/365 (Fixed)", "A/365 (Fixed)", or "A/365F".
    /// According to ISDA, "Actual/365" (without "Fixed") is an alias for "Actual/Actual (ISDA)".
    /// If Actual/365 is not explicitly specified as fixed in an instrument specification,
    /// you might want to double-check its meaning.
    /// </remarks>
    public class Actual365Fixed : DayCounter
    {
        /// <summary>
        /// Defines the available conventions for the Actual/365 (Fixed) day counter.
        /// </summary>
        public enum Convention
        {
            Standard,
            Canadian,
            NoLeap
        }

        private readonly Convention _convention;

        public Actual365Fixed(Convention c = Convention.Standard)
        {
            _convention = c;
        }

        public override string Name =>
            _convention switch
            {
                Convention.Standard => "Actual/365 (Fixed)",
                Convention.Canadian => "Actual/365 (Fixed) Canadian Bond",
                Convention.NoLeap => "Actual/365 (No Leap)",
                _ => throw new ArgumentOutOfRangeException()
            };

        public override int DayCount(Date d1, Date d2)
        {
            if (_convention == Convention.NoLeap)
            {
                // Custom implementation for NoLeap convention
                int[] monthOffset = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };

                int s1 = d1.DayOfMonth + monthOffset[(int)d1.Month - 1] + (d1.Year * 365);
                int s2 = d2.DayOfMonth + monthOffset[(int)d2.Month - 1] + (d2.Year * 365);

                if (d1.Month == Month.February && d1.DayOfMonth == 29)
                {
                    s1--;
                }

                if (d2.Month == Month.February && d2.DayOfMonth == 29)
                {
                    s2--;
                }

                return s2 - s1;
            }

            // Standard and Canadian conventions use the default implementation
            return base.DayCount(d1, d2);
        }

        public override double YearFraction(Date d1, Date d2, Date? refPeriodStart = null, Date? refPeriodEnd = null)
        {
            switch (_convention)
            {
                case Convention.Standard:
                case Convention.NoLeap:
                    // For NoLeap, DayCount is overridden to handle the logic.
                    return DayCount(d1, d2) / 365.0;

                case Convention.Canadian:
                    if (d1 == d2)
                        return 0.0;

                    QL.Require(refPeriodStart.HasValue, "invalid refPeriodStart");
                    QL.Require(refPeriodEnd.HasValue, "invalid refPeriodEnd");

                    double dcs = DayCount(d1, d2); // Uses base.DayCount
                    double dcc = DayCount(refPeriodStart.Value, refPeriodEnd.Value); // Uses base.DayCount

                    int months = (int)Math.Round(12.0 * dcc / 365.0);
                    QL.Require(months != 0, "invalid reference period for Act/365 Canadian; must be longer than a month");

                    int frequency = 12 / months;
                    QL.Require(frequency != 0, "invalid reference period for Act/365 Canadian; must not be longer than a year");

                    if (dcs < 365.0 / frequency)
                        return dcs / 365.0;

                    return 1.0 / frequency - (dcc - dcs) / 365.0;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}