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
   //! Hungarian calendar
   /*! Holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>Easter Monday</li>
       <li>Whit(Pentecost) Monday </li>
       <li>New Year's Day, January 1st</li>
       <li>National Day, March 15th</li>
       <li>Labour Day, May 1st</li>
       <li>Constitution Day, August 20th</li>
       <li>Republic Day, October 23rd</li>
       <li>All Saints Day, November 1st</li>
       <li>Christmas, December 25th</li>
       <li>2nd Day of Christmas, December 26th</li>
       </ul>

       \ingroup calendars
   */
   public class Hungary : Calendar
   {
      public Hungary() : base(Impl.Singleton) { }

      private class Impl : WesternImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Hungary"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);
            if (isWeekend(w)
                // Good Friday (since 2017)
                || (dd == em - 3 && y >= 2017)
                // Easter Monday
                || (dd == em)
                // Whit Monday
                || (dd == em + 49)
                // New Year's Day
                || (d == 1  && m == Month.January)
                // National Day
                || (d == 15  && m == Month.March)
                // Labour Day
                || (d == 1  && m == Month.May)
                // Constitution Day
                || (d == 20  && m == Month.August)
                // Republic Day
                || (d == 23  && m == Month.October)
                // All Saints Day
                || (d == 1  && m == Month.November)
                // Christmas
                || (d == 25 && m == Month.December)
                // 2nd Day of Christmas
                || (d == 26 && m == Month.December))
               return false;
            return true;
         }
      }
   }
}
