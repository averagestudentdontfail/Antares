/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)
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
   //! Finnish calendar
   /*! Holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Epiphany, January 6th</li>
       <li>Good Friday</li>
       <li>Easter Monday</li>
       <li>Ascension Thursday</li>
       <li>Labour Day, May 1st</li>
       <li>Midsummer Eve (Friday between June 18-24)</li>
       <li>Independence Day, December 6th</li>
       <li>Christmas Eve, December 24th</li>
       <li>Christmas, December 25th</li>
       <li>Boxing Day, December 26th</li>
       </ul>

       \ingroup calendars
   */
   public class Finland : Calendar
   {
      public Finland() : base(Impl.Singleton) { }

      private class Impl : WesternImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Finland"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);

            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                // Epiphany
                || (d == 6 && m == Month.January)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // Ascension Thursday
                || (dd == em + 38)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Midsummer Eve (Friday between June 18-24)
                || (w == DayOfWeek.Friday && (d >= 18 && d <= 24) && m == Month.June)
                // Independence Day
                || (d == 6 && m == Month.December)
                // Christmas Eve
                || (d == 24 && m == Month.December)
                // Christmas
                || (d == 25 && m == Month.December)
                // Boxing Day
                || (d == 26 && m == Month.December))
               return false;
            return true;
         }
      }
   }
}

