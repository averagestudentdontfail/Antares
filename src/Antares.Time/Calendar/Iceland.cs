/*
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008 Siarhei Novik (snovik@gmail.com)
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)

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
   //! Icelandic calendars
   /*! Holidays for the Iceland stock exchange
       (data from <http://www.icex.is/is/calendar?languageID=1>):
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st (possibly moved to Monday)</li>
       <li>Holy Thursday</li>
       <li>Good Friday</li>
       <li>Easter Monday</li>
       <li>First day of Summer (third or fourth Thursday in April)</li>
       <li>Labour Day, May 1st</li>
       <li>Ascension Thursday</li>
       <li>Pentecost Monday</li>
       <li>Independence Day, June 17th</li>
       <li>Commerce Day, first Monday in August</li>
       <li>Christmas, December 25th</li>
       <li>Boxing Day, December 26th</li>
       </ul>

       \ingroup calendars
   */
   public class Iceland : Calendar
   {
      public Iceland() : base(Impl.Singleton) { }

      private class Impl : WesternImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Iceland stock exchange"; }
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
                // Holy Thursday
                || (dd == em - 4)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // First day of Summer
                || (d >= 19 && d <= 25 && w == DayOfWeek.Thursday && m == Month.April)
                // Ascension Thursday
                || (dd == em + 38)
                // Pentecost Monday
                || (dd == em + 49)
                // Labour Day
                || (d == 1 && m == Month.May)
                // Independence Day
                || (d == 17 && m == Month.June)
                // Commerce Day
                || (d <= 7 && w == DayOfWeek.Monday && m == Month.August)
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
