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
   //! Swedish calendar
   /*! Holidays:
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Epiphany, January 6th</li>
       <li>Good Friday</li>
       <li>Easter Monday</li>
       <li>Ascension</li>
       <li>Whit(Pentecost) Monday </li>
       <li>May Day, May 1st</li>
       <li>National Day, June 6th</li>
       <li>Midsummer Eve (Friday between June 19-25)</li>
       <li>Christmas Eve, December 24th</li>
       <li>Christmas Day, December 25th</li>
       <li>Boxing Day, December 26th</li>
       <li>New Year's Eve, December 31th</li>
       </ul>

       \ingroup calendars
   */
   public class Sweden :  Calendar
   {
      public Sweden() : base(Impl.Singleton) { }

      private class Impl : WesternImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Sweden"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            int em = easterMonday(y);
            if (isWeekend(w)
                // Good Friday
                || (dd == em - 3)
                // Easter Monday
                || (dd == em)
                // Ascension Thursday
                || (dd == em + 38)
                // Whit Monday
                || (dd == em + 49)
                // New Year's Day
                || (d == 1  && m == Month.January)
                // Epiphany
                || (d == 6  && m == Month.January)
                // May Day
                || (d == 1  && m == Month.May)
                // June 6 id National Day but is not a holiday.
                // holiday since 2005
                || (d == 6 && m == Month.June && y >= 2005)
                // Midsummer Eve (Friday between June 19-25)
                || (w == DayOfWeek.Friday && (d >= 19 && d <= 25) && m == Month.June)
                // Christmas Eve
                || (d == 24 && m == Month.December)
                // Christmas Day
                || (d == 25 && m == Month.December)
                // Boxing Day
                || (d == 26 && m == Month.December)
                // New Year's Eve
                || (d == 31 && m == Month.December))
               return false;
            return true;
         }
      }
   }
}

