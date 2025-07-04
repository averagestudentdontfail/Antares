/*
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)

 This file is part of Antares Project https://github.com/amaggiulli/Antares

 Antares is free software: you can redistribute it and/or modify it
 under the terms of the Antares license.  You should have received a
 copy of the license along with this program; if not, license is
 available at <https://github.com/amaggiulli/Antares/blob/develop/LICENSE>.

 Antares is a based on QuantLib, a free-software/open-source library
 for financial quantitative analysts and developers - http://quantlib.org/
 The QuantLib license is available online at http://quantlib.org/license.shtml.

 This program is distributed in the hope that it will be useful, but WITHOUT
 ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 FOR A PARTICULAR PURPOSE.  See the license for more details.
*/

using System;

namespace Antares
{
   //! Japanese calendar
   /*! Holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Bank Holiday, January 2nd</li>
       <li>Bank Holiday, January 3rd</li>
       <li>Coming of Age Day, 2nd Monday in January</li>
       <li>National Foundation Day, February 11th</li>
       <li>Vernal Equinox</li>
       <li>Greenery Day, April 29th</li>
       <li>Constitution Memorial Day, May 3rd</li>
       <li>Holiday for a Nation, May 4th</li>
       <li>Children's Day, May 5th</li>
       <li>Marine Day, 3rd Monday in July</li>
       <li>Mountain Day, August 11th (from 2016 onwards)</li>
       <li>Respect for the Aged Day, 3rd Monday in September</li>
       <li>Autumnal Equinox</li>
       <li>Health and Sports Day, 2nd Monday in October</li>
       <li>National Culture Day, November 3rd</li>
       <li>Labor Thanksgiving Day, November 23rd</li>
       <li>Emperor's Birthday, December 23rd</li>
       <li>Bank Holiday, December 31st</li>
       <li>a few one-shot holidays</li>
       </ul>
       Holidays falling on a Sunday are observed on the Monday following
       except for the bank holidays associated with the new year.

       \ingroup calendars
   */
   public class Japan : Calendar
   {
      public Japan() : base(Impl.Singleton) { }

      private class Impl : CalendarImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Japan"; }
         public override bool isWeekend(DayOfWeek w)
         {
            return w == DayOfWeek.Saturday || w == DayOfWeek.Sunday;
         }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;

            // equinox calculation
            var exact_vernal_equinox_time = 20.69115;
            var exact_autumnal_equinox_time = 23.09;
            var diff_per_year = 0.242194;
            var moving_amount = (y - 2000) * diff_per_year;
            var number_of_leap_years = (y - 2000) / 4 + (y - 2000) / 100 - (y - 2000) / 400;
            var ve = (int)(exact_vernal_equinox_time + moving_amount - number_of_leap_years); // vernal equinox day
            var ae = (int)(exact_autumnal_equinox_time + moving_amount - number_of_leap_years);   // autumnal equinox day
            // checks
            if (isWeekend(w)
                // New Year's Day
                || (d == 1  && m == Month.January)
                // Bank Holiday
                || (d == 2  && m == Month.January)
                // Bank Holiday
                || (d == 3  && m == Month.January)
                // Coming of Age Day (2nd Monday in January),
                // was January 15th until 2000
                || (w == DayOfWeek.Monday && (d >= 8 && d <= 14) && m == Month.January
                    && y >= 2000)
                || ((d == 15 || (d == 16 && w == DayOfWeek.Monday)) && m == Month.January
                    && y < 2000)
                // National Foundation Day
                || ((d == 11 || (d == 12 && w == DayOfWeek.Monday)) && m == Month.February)
                // Emperor's Birthday (Emperor Naruhito)
                || ((d == 23 || (d == 24 && w == DayOfWeek.Monday)) && m == Month.February && y >= 2020)
                // Emperor's Birthday (Emperor Akihito)
                || ((d == 23 || (d == 24 && w == DayOfWeek.Monday)) && m == Month.December && (y >= 1989 && y < 2019))
                // Vernal Equinox
                || ((d == ve || (d == ve + 1 && w == DayOfWeek.Monday)) && m == Month.March)
                // Greenery Day
                || ((d == 29 || (d == 30 && w == DayOfWeek.Monday)) && m == Month.April)
                // Constitution Memorial Day
                || (d == 3  && m == Month.May)
                // Holiday for a Nation
                || (d == 4  && m == Month.May)
                // Children's Day
                || (d == 5 && m == Month.May)
                // any of the three above observed later if on Saturday or Sunday
                || (d == 6 && m == Month.May && (w == DayOfWeek.Monday || w == DayOfWeek.Tuesday || w == DayOfWeek.Wednesday))
                // Marine Day (3rd Monday in July),
                // was July 20th until 2003, not a holiday before 1996,
                // July 23rd in 2020 due to Olympics games
                // July 22nd in 2021 due to Olympics games
                || (w == DayOfWeek.Monday && (d >= 15 && d <= 21) && m == Month.July
                    && ((y >= 2003 && y < 2020) || y >= 2022))
                || ((d == 20 || (d == 21 && w == DayOfWeek.Monday)) && m == Month.July && y >= 1996 && y < 2003)
                || (d == 23 && m == Month.July && y == 2020)
                || (d == 22 && m == Month.July && y == 2021)
                // Mountain Day
                // (moved in 2020 due to Olympics games)
                // (moved in 2021 due to Olympics games)
                || ((d == 11 || (d == 12 && w == DayOfWeek.Monday)) && m == Month.August && ((y >= 2016 && y < 2020) || y >= 2022))
                || (d == 10 && m == Month.August && y == 2020)
                || (d == 9 && m == Month.August && y == 2021)
                // Respect for the Aged Day (3rd Monday in September),
                // was September 15th until 2003
                || (w == DayOfWeek.Monday && (d >= 15 && d <= 21) && m == Month.September && y >= 2003)
                || ((d == 15 || (d == 16 && w == DayOfWeek.Monday)) && m == Month.September && y < 2003)
                // If a single day falls between Respect for the Aged Day
                // and the Autumnal Equinox, it is holiday
                || (w == DayOfWeek.Tuesday && d+1 == ae && d >= 16 && d <= 22 && m == Month.September && y >= 2003)
                // Autumnal Equinox
                || ((d == ae || (d == ae+1 && w == DayOfWeek.Monday)) && m == Month.September)
                // Health and Sports Day (2nd Monday in October),
                // was October 10th until 2000,
                // July 24th in 2020 due to Olympics games
                // July 23rd in 2021 due to Olympics games
                || (w == DayOfWeek.Monday && (d >= 8 && d <= 14) && m == Month.October
                    && ((y >= 2000 && y < 2020) || y >= 2022))
                || ((d == 10 || (d == 11 && w == DayOfWeek.Monday)) && m == Month.October && y < 2000)
                || (d == 24 && m == Month.July && y == 2020)
                || (d == 23 && m == Month.July && y == 2021)
                // National Culture Day
                || ((d == 3  || (d == 4 && w == DayOfWeek.Monday)) && m == Month.November)
                // Labor Thanksgiving Day
                || ((d == 23 || (d == 24 && w == DayOfWeek.Monday)) && m == Month.November)
                // Bank Holiday
                || (d == 31 && m == Month.December)
                // one-shot holidays
                // Marriage of Prince Akihito
                || (d == 10 && m == Month.April && y == 1959)
                // Rites of Imperial Funeral
                || (d == 24 && m == Month.February && y == 1989)
                // Enthronement Ceremony (Emperor Akihito)
                || (d == 12 && m == Month.November && y == 1990)
                // Marriage of Prince Naruhito
                || (d == 9 && m == Month.June && y == 1993)
                // Special holiday based on Japanese public holidays law
                || (d == 30 && m == Month.April && y == 2019)
                // Enthronement Day (Emperor Naruhito)
                || (d == 1 && m == Month.May && y == 2019)
                // Special holiday based on Japanese public holidays law
                || (d == 2 && m == Month.May && y == 2019)
                // Enthronement Ceremony (Emperor Naruhito)
                || (d == 22 && m == Month.October && y == 2019))
               return false;
            return true;
         }
      }
   }
}

