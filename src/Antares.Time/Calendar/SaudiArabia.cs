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
   //! Saudi Arabian calendar
   /*! Holidays for the Tadawul financial market
       (data from <http://www.tadawul.com.sa>):
       <ul>
       <li>Thursdays</li>
       <li>Fridays</li>
       <li>National Day of Saudi Arabia, September 23rd</li>
       </ul>

       Other holidays for which no rule is given
       (data available for 2004-2011 only:)
       <ul>
       <li>Eid Al-Adha</li>
       <li>Eid Al-Fitr</li>
       </ul>

       \ingroup calendars
   */
   public class SaudiArabia : Calendar
   {
      public SaudiArabia() : base(Impl.Singleton) { }

      private class Impl : CalendarImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Tadawul"; }
         public override bool isWeekend(DayOfWeek w)
         {
            return w == DayOfWeek.Thursday || w == DayOfWeek.Friday;
         }

         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;

            if (isWeekend(w)
                // National Day
                || (d == 23 && m == Month.September)
                // Eid Al-Adha
                || (d >= 1 && d <= 6 && m == Month.February && y == 2004)
                || (d >= 21 && d <= 25 && m == Month.January && y == 2005)
                || (d >= 26 && m == Month.November && y == 2009)
                || (d <= 4 && m == Month.December && y == 2009)
                || (d >= 11 && d <= 19 && m == Month.November && y == 2010)
                // Eid Al-Fitr
                || (d >= 25 && d <= 29 && m == Month.November && y == 2004)
                || (d >= 14 && d <= 18 && m == Month.November && y == 2005)
                || (d >= 25 && m == Month.August && y == 2011)
                || (d <= 2 && m == Month.September && y == 2011)
                // other one-shot holidays
                || (d == 26 && m == Month.February && y == 2011)
                || (d == 19 && m == Month.March && y == 2011)
               )
               return false;
            return true;
         }
      }
   }
}
