// Frequency.cs

using System;
using System.ComponentModel;

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
    /// Provides extension methods for the Frequency enum.
    /// </summary>
    public static class FrequencyExtensions
    {
        /// <summary>
        /// Returns a string representation of the frequency.
        /// This method mimics the C++ operator&lt;&lt; and provides error checking for invalid enum values.
        /// </summary>
        /// <param name="f">The Frequency enum value.</param>
        /// <returns>A string representation of the frequency.</returns>
        public static string ToEnumString(this Frequency f)
        {
            return f switch
            {
                Frequency.NoFrequency => "No-Frequency",
                Frequency.Once => "Once",
                Frequency.Annual => "Annual",
                Frequency.Semiannual => "Semiannual",
                Frequency.EveryFourthMonth => "Every-Fourth-Month",
                Frequency.Quarterly => "Quarterly",
                Frequency.Bimonthly => "Bimonthly",
                Frequency.Monthly => "Monthly",
                Frequency.EveryFourthWeek => "Every-fourth-week",
                Frequency.Biweekly => "Biweekly",
                Frequency.Weekly => "Weekly",
                Frequency.Daily => "Daily",
                Frequency.OtherFrequency => "Unknown frequency",
                _ => throw new InvalidEnumArgumentException(nameof(f), (int)f, typeof(Frequency))
            };
        }
    }
}