/*
 Copyright (C) 2008-2024 Andrea Maggiulli (a.maggiulli@gmail.com)
 Copyright (C) 2008 Alessandro Duci
 Copyright (C) 2008, 2009 Siarhei Novik (snovik@gmail.com)

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
using System.Text;

namespace Antares
{
   //! %Singapore calendars
   /*! Holidays for the Singapore exchange
       (data from <http://www.sgx.com/wps/portal/sgxweb/home/trading/securities/trading_hours_calendar>):
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's day, January 1st</li>
       <li>Good Friday</li>
       <li>Labour Day, May 1st</li>
       <li>National Day, August 9th</li>
       <li>Christmas, December 25th </li>
       </ul>

       Other holidays for which no rule is given
       (data available for 2004-2010, 2012-2014,  2019-2024 only:)
       <ul>
       <li>Chinese New Year</li>
       <li>Hari Raya Haji</li>
       <li>Vesak Poya Day</li>
       <li>Deepavali</li>
       <li>Diwali</li>
       <li>Hari Raya Puasa</li>
       </ul>

       \ingroup calendars
   */
   public class Singapore : Calendar
   {
      public Singapore() : base(Impl.Singleton) { }

      private class Impl : WesternImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Singapore exchange"; }
         public override bool isBusinessDay(Date date)
         {
            var w = date.DayOfWeek;
            int d = date.Day, dd = date.DayOfYear;
            var m = (Month)date.Month;
            var y = date.Year;
            var em = easterMonday(y);

            if (isWeekend(w)
                // New Year's Day
                || ((d == 1 || (d == 2 && w == DayOfWeek.Monday)) && m == Month.January)
                // Good Friday
                || (dd == em - 3)
                // Labor Day
                || (d == 1 && m == Month.May)
                // National Day
                || ((d == 9 || (d == 10 && w == DayOfWeek.Monday)) && m == Month.August)
                // Christmas Day
                || (d == 25 && m == Month.December)

                // Chinese New Year
                || ((d == 22 || d == 23) && m == Month.January && y == 2004)
                || ((d == 9 || d == 10) && m == Month.February && y == 2005)
                || ((d == 30 || d == 31) && m == Month.January && y == 2006)
                || ((d == 19 || d == 20) && m == Month.February && y == 2007)
                || ((d == 7 || d == 8) && m == Month.February && y == 2008)
                || ((d == 26 || d == 27) && m == Month.January && y == 2009)
                || ((d == 15 || d == 16) && m == Month.January && y == 2010)
                || ((d == 23 || d == 24) && m == Month.January && y == 2012)
                || ((d == 11 || d == 12) && m == Month.February && y == 2013)
                || (d == 31 && m == Month.January && y == 2014)
                || (d == 1 && m == Month.February && y == 2014)

                // Hari Raya Haji
                || ((d == 1 || d == 2) && m == Month.February && y == 2004)
                || (d == 21 && m == Month.January && y == 2005)
                || (d == 10 && m == Month.January && y == 2006)
                || (d == 2 && m == Month.January && y == 2007)
                || (d == 20 && m == Month.December && y == 2007)
                || (d == 8 && m == Month.December && y == 2008)
                || (d == 27 && m == Month.November && y == 2009)
                || (d == 17 && m == Month.November && y == 2010)
                || (d == 26 && m == Month.October && y == 2012)
                || (d == 15 && m == Month.October && y == 2013)
                || (d == 6 && m == Month.October && y == 2014)

                // Vesak Poya Day
                || (d == 2 && m == Month.June && y == 2004)
                || (d == 22 && m == Month.May && y == 2005)
                || (d == 12 && m == Month.May && y == 2006)
                || (d == 31 && m == Month.May && y == 2007)
                || (d == 18 && m == Month.May && y == 2008)
                || (d == 9 && m == Month.May && y == 2009)
                || (d == 28 && m == Month.May && y == 2010)
                || (d == 5 && m == Month.May && y == 2012)
                || (d == 24 && m == Month.May && y == 2013)
                || (d == 13 && m == Month.May && y == 2014)

                // Deepavali
                || (d == 11 && m == Month.November && y == 2004)
                || (d == 8 && m == Month.November && y == 2007)
                || (d == 28 && m == Month.October && y == 2008)
                || (d == 16 && m == Month.November && y == 2009)
                || (d == 5 && m == Month.November && y == 2010)
                || (d == 13 && m == Month.November && y == 2012)
                || (d == 2 && m == Month.November && y == 2013)
                || (d == 23 && m == Month.October && y == 2014)

                // Diwali
                || (d == 1 && m == Month.November && y == 2005)

                // Hari Raya Puasa
                || ((d == 14 || d == 15) && m == Month.November && y == 2004)
                || (d == 3 && m == Month.November && y == 2005)
                || (d == 24 && m == Month.October && y == 2006)
                || (d == 13 && m == Month.October && y == 2007)
                || (d == 1 && m == Month.October && y == 2008)
                || (d == 21 && m == Month.September && y == 2009)
                || (d == 10 && m == Month.September && y == 2010)
                || (d == 20 && m == Month.August && y == 2012)
                || (d == 8 && m == Month.August && y == 2013)
                || (d == 28 && m == Month.July && y == 2014)
               )
               return false;

            if (y == 2019)
            {
               if ( // Chinese New Year
                   ((d == 5 || d == 6) && m == Month.February)
                   // Vesak Poya Day
                   || (d == 20 && m == Month.May)
                  // Hari Raya Puasa
                   || (d == 5 && m == Month.June)
                   // Hari Raya Haji
                   || (d == 12 && m == Month.August)
                   // Deepavali
                   || (d == 28 && m == Month.October)
                   )
                return false;
            }

            // https://api2.sgx.com/sites/default/files/2020-11/SGX%20Derivatives%20Trading%20Calendar%202020_Dec%20Update_D3.pdf
            if (y == 2020)
            {
               if ( // Chinese New Year
                   (d == 27 && m == Month.January)
                   // Vesak Poya Day
                   || (d == 7 && m == Month.May)
                   // Hari Raya Puasa
                   || (d == 25 && m == Month.May)
                   // Hari Raya Haji
                   || (d == 31 && m == Month.July)
                   // Deepavali
                   || (d == 14 && m == Month.November)
                  )
                  return false;
            }

            // https://api2.sgx.com/sites/default/files/2021-07/SGX_Derivatives%20Trading%20Calendar%202021%20%28Final%20-%20Jul%29.pdf
            if (y == 2021)
            {
               if ( // Chinese New Year
                   (d == 12 && m == Month.February)
                   // Hari Raya Puasa
                   || (d == 13 && m == Month.May)
                   // Vesak Poya Day
                   || (d == 26 && m == Month.May)
                   // Hari Raya Haji
                   || (d == 20 && m == Month.July)
                   // Deepavali
                   || (d == 4 && m == Month.November)
                  )
                  return false;
            }

            // https://api2.sgx.com/sites/default/files/2022-06/DT%20Trading%20Calendar%202022%20%28Final%29.pdf
            if (y == 2022)
            {
               if (// Chinese New Year
                   ((d == 1 || d == 2) && m == Month.February)
                   // Labour Day
                   || (d == 2 && m == Month.May)
                   // Hari Raya Puasa
                   || (d == 3 && m == Month.May)
                   // Vesak Poya Day
                   || (d == 16 && m == Month.May)
                   // Hari Raya Haji
                   || (d == 11 && m == Month.July)
                   // Deepavali
                   || (d == 24 && m == Month.October)
                   // Christmas Day
                   || (d == 26 && m == Month.December)
                  )
                  return false;
            }

            // https://api2.sgx.com/sites/default/files/2023-01/SGX%20Calendar%202023_0.pdf
            if (y == 2023)
            {
               if (// Chinese New Year
                   ((d == 23 || d == 24) && m == Month.January)
                   // Hari Raya Puasa
                   || (d == 22 && m == Month.April)
                   // Vesak Poya Day
                   || (d == 2 && m == Month.June)
                   // Hari Raya Haji
                   || (d == 29 && m == Month.June)
                   // Public holiday on polling day
                   || (d == 1 && m == Month.September)
                   // Deepavali
                   || (d == 13 && m == Month.November))
                  return false;
            }
            // https://api2.sgx.com/sites/default/files/2024-01/SGX%20Calendar%202024_2.pdf
            if (y == 2024)
            {
               if (// Chinese New Year
                   (d == 12 && m == Month.February)
                   // Hari Raya Puasa
                   || (d == 10 && m == Month.April)
                   // Vesak Poya Day
                   || (d == 22 && m == Month.May)
                   // Hari Raya Haji
                   || (d == 17 && m == Month.June)
                   // Deepavali
                   || (d == 31 && m == Month.October))
                  return false;
            }
            return true;
         }
      }
   }
}


