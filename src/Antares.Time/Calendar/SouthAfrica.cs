/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008-2024 Andrea Maggiulli (a.maggiulli@gmail.com)
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

   //! South-African calendar
   /*! Holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st (possibly moved to Monday)</li>
       <li>Good Friday</li>
       <li>Family Day, Easter Monday</li>
       <li>Human Rights Day, March 21st (possibly moved to Monday)</li>
       <li>Freedom Day, April 27th (possibly moved to Monday)</li>
       <li>Workers Day, May 1st (possibly moved to Monday)</li>
       <li>Youth Day, June 16th (possibly moved to Monday)</li>
       <li>National Women's Day, August 9th
       (possibly moved to Monday)</li>
       <li>Heritage Day, September 24th (possibly moved to Monday)</li>
       <li>Day of Reconciliation, December 16th
       (possibly moved to Monday)</li>
       <li>Christmas December 25th </li>
       <li>Day of Goodwill December 26th (possibly moved to Monday)</li>
       </ul>

       \ingroup calendars
   */
   public class SouthAfrica :  Calendar
   {
      public SouthAfrica() : base(Impl.Singleton) { }

      private class Impl : WesternImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "South Africa"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);

            if (isWeekend(w)
                // New Year's Day (possibly moved to Monday)
                || ((d == 1 || (d == 2 && w == DayOfWeek.Monday)) && m == Month.January)
                // Good Friday
                || (dd == em - 3)
                // Family Day
                || (dd == em)
                // Human Rights Day, March 21st (possibly moved to Monday)
                || ((d == 21 || (d == 22 && w == DayOfWeek.Monday))
                    && m == Month.March)
                // Freedom Day, April 27th (possibly moved to Monday)
                || ((d == 27 || (d == 28 && w == DayOfWeek.Monday))
                    && m == Month.April)
                // Election Day, April 14th 2004
                || (d == 14 && m == Month.April && y == 2004)
                // Workers Day, May 1st (possibly moved to Monday)
                || ((d == 1 || (d == 2 && w == DayOfWeek.Monday))
                    && m == Month.May)
                // Youth Day, June 16th (possibly moved to Monday)
                || ((d == 16 || (d == 17 && w == DayOfWeek.Monday))
                    && m == Month.June)
                // National Women's Day, August 9th (possibly moved to Monday)
                || ((d == 9 || (d == 10 && w == DayOfWeek.Monday))
                    && m == Month.August)
                // Heritage Day, September 24th (possibly moved to Monday)
                || ((d == 24 || (d == 25 && w == DayOfWeek.Monday))
                    && m == Month.September)
                // Day of Reconciliation, December 16th
                // (possibly moved to Monday)
                || ((d == 16 || (d == 17 && w == DayOfWeek.Monday))
                    && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // Day of Goodwill (possibly moved to Monday)
                || ((d == 26 || (d == 27 && w == DayOfWeek.Monday))
                    && m == Month.December)
                // one-shot: Election day 2009
                || (d == 22 && m == Month.April && y == 2009)
                // one-shot: Election day 2016
                || (d == 3 && m == Month.August && y == 2016)
                // one-shot: In lieu of Christmas falling on Sunday in 2022
                || (d == 27 && m == Month.December && y == 2022)
                // one-shot: Special holiday to celebrate winning of Rugby World Cup 2023
                || (d == 15 && m == Month.December && y == 2023)
                // one-shot: Election day 2024
                || (d == 29 && m == Month.May && y == 2024)
               )
               return false;
            return true;
         }
      }
   }
}

