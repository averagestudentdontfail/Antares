/*
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
   //! %TARGET calendar
   /*! Holidays (see http://www.ecb.int):
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Good Friday (since 2000)</li>
       <li>Easter Monday (since 2000)</li>
       <li>Labour Day, May 1st (since 2000)</li>
       <li>Christmas, December 25th</li>
       <li>Day of Goodwill, December 26th (since 2000)</li>
       <li>December 31st (1998, 1999, and 2001)</li>
       </ul>
   */
   public class TARGET : Calendar
   {
      public TARGET() : base(Impl.Singleton) {}

      private class Impl : WesternImpl
      {
         internal static readonly Impl Singleton = new();
         private Impl() {}

         public override string name() { return "TARGET calendar"; }
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
                // Good Friday
                || (dd == em - 3 && y >= 2000)
                // Easter Monday
                || (dd == em && y >= 2000)
                // Labour Day
                || (d == 1 && m == Month.May && y >= 2000)
                // Christmas
                || (d == 25 && m == Month.December)
                // Day of Goodwill
                || (d == 26 && m == Month.December && y >= 2000)
                // December 31st, 1998, 1999, and 2001 only
                || (d == 31 && m == Month.December && (y == 1998 || y == 1999 || y == 2001)))
               return false;
            return true;
         }

      }
   }
}
