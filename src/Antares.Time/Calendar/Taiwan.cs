/*
 Copyright (C) 2008-2022 Andrea Maggiulli (a.maggiulli@gmail.com)
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

namespace Antares
{
   //! Taiwanese calendars
   /*! Holidays for the Taiwan stock exchange
       (data from <http://www.tse.com.tw/en/trading/trading_days.php>):
       <ul>
       <li>Saturdays</li>
       <li>Sundays</li>
       <li>New Year's Day, January 1st</li>
       <li>Peace Memorial Day, February 28</li>
       <li>Labor Day, May 1st</li>
       <li>Double Tenth National Day, October 10th</li>
       </ul>

       Other holidays for which no rule is given
       (data available for 2002-2013 only:)
       <ul>
       <li>Chinese Lunar New Year</li>
       <li>Tomb Sweeping Day</li>
       <li>Dragon Boat Festival</li>
       <li>Moon Festival</li>
       </ul>

       \ingroup calendars
   */
   public class Taiwan :  Calendar
   {
      public Taiwan() : base(Impl.Singleton) { }

      private class Impl : CalendarImpl
      {
         public static readonly Impl Singleton = new();
         private Impl() { }

         public override string name() { return "Taiwan stock exchange"; }
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

            if (isWeekend(w)
                // New Year's Day
                || (d == 1 && m == Month.January)
                // Peace Memorial Day
                || (d == 28 && m == Month.February)
                // Labor Day
                || (d == 1 && m == Month.May)
                // Double Tenth
                || (d == 10 && m == Month.October)
               )
               return false;

            if (y == 2002)
            {
               // Dragon Boat Festival and Moon Festival fall on Saturday
               if (// Chinese Lunar New Year
                  (d >= 9 && d <= 17 && m == Month.February)
                  // Tomb Sweeping Day
                  || (d == 5 && m == Month.April)
               )
                  return false;
            }

            if (y == 2003)
            {
               // Tomb Sweeping Day falls on Saturday
               if (// Chinese Lunar New Year
                  ((d >= 31 && m == Month.January) || (d <= 5 && m == Month.February))
                  // Dragon Boat Festival
                  || (d == 4 && m == Month.June)
                  // Moon Festival
                  || (d == 11 && m == Month.September)
               )
                  return false;
            }

            if (y == 2004)
            {
               // Tomb Sweeping Day falls on Sunday
               if (// Chinese Lunar New Year
                  (d >= 21 && d <= 26 && m == Month.January)
                  // Dragon Boat Festival
                  || (d == 22 && m == Month.June)
                  // Moon Festival
                  || (d == 28 && m == Month.September)
               )
                  return false;
            }

            if (y == 2005)
            {
               // Dragon Boat and Moon Festival fall on Saturday or Sunday
               if (// Chinese Lunar New Year
                  (d >= 6 && d <= 13 && m == Month.February)
                  // Tomb Sweeping Day
                  || (d == 5 && m == Month.April)
                  // make up for Labor Day, not seen in other years
                  || (d == 2 && m == Month.May)
               )
                  return false;
            }

            if (y == 2006)
            {
               // Dragon Boat and Moon Festival fall on Saturday or Sunday
               if (// Chinese Lunar New Year
                  ((d >= 28 && m == Month.January) || (d <= 5 && m == Month.February))
                  // Tomb Sweeping Day
                  || (d == 5 && m == Month.April)
                  // Dragon Boat Festival
                  || (d == 31 && m == Month.May)
                  // Moon Festival
                  || (d == 6 && m == Month.October)
               )
                  return false;
            }

            if (y == 2007)
            {
               if (// Chinese Lunar New Year
                  (d >= 17 && d <= 25 && m == Month.February)
                  // Tomb Sweeping Day
                  || (d == 5 && m == Month.April)
                  // adjusted holidays
                  || (d == 6 && m == Month.April)
                  || (d == 18 && m == Month.June)
                  // Dragon Boat Festival
                  || (d == 19 && m == Month.June)
                  // adjusted holiday
                  || (d == 24 && m == Month.September)
                  // Moon Festival
                  || (d == 25 && m == Month.September)
               )
                  return false;
            }

            if (y == 2008)
            {
               if (// Chinese Lunar New Year
                  (d >= 4 && d <= 11 && m == Month.February)
                  // Tomb Sweeping Day
                  || (d == 4 && m == Month.April)
               )
                  return false;
            }

            if (y == 2009)
            {
               if (// Public holiday
                  (d == 2 && m == Month.January)
                  // Chinese Lunar New Year
                  || (d >= 24 && m == Month.January)
                  // Tomb Sweeping Day
                  || (d == 4 && m == Month.April)
                  // Dragon Boat Festival
                  || ((d == 28 || d == 29) && m == Month.May)
                  // Moon Festival
                  || (d == 3 && m == Month.October)
               )
                  return false;
            }

            if (y == 2010)
            {
               if (// Chinese Lunar New Year
                  (d >= 13 && d <= 21 && m == Month.January)
                  // Tomb Sweeping Day
                  || (d == 5 && m == Month.April)
                  // Dragon Boat Festival
                  || (d == 16 && m == Month.May)
                  // Moon Festival
                  || (d == 22 && m == Month.September)
               )
                  return false;
            }

            if (y == 2011)
            {
               if (// Spring Festival
                  (d >= 2 && d <= 7 && m == Month.February)
                  // Children's Day
                  || (d == 4 && m == Month.April)
                  // Tomb Sweeping Day
                  || (d == 5 && m == Month.April)
                  // Labour Day
                  || (d == 2 && m == Month.May)
                  // Dragon Boat Festival
                  || (d == 6 && m == Month.June)
                  // Mid-Autumn Festival
                  || (d == 12 && m == Month.September)
               )
                  return false;
            }

            if (y == 2012)
            {
               if (// Spring Festival
                  (d >= 23 && d <= 27 && m == Month.January)
                  // Peace Memorial Day
                  || (d == 27 && m == Month.February)
                  // Children's Day
                  // Tomb Sweeping Day
                  || (d == 4 && m == Month.April)
                  // Labour Day
                  || (d == 1 && m == Month.May)
                  // Dragon Boat Festival
                  || (d == 23 && m == Month.June)
                  // Mid-Autumn Festival
                  || (d == 30 && m == Month.September)
                  // Memorial Day:
                  // Founding of the Republic of China
                  || (d == 31 && m == Month.December)
               )
                  return false;
            }

            if (y == 2013)
            {
               if (// Spring Festival
                  (d >= 10 && d <= 15 && m == Month.February)
                  // Children's Day
                  || (d == 4 && m == Month.April)
                  // Tomb Sweeping Day
                  || (d == 5 && m == Month.April)
                  // Labour Day
                  || (d == 1 && m == Month.May)
                  // Dragon Boat Festival
                  || (d == 12 && m == Month.June)
                  // Mid-Autumn Festival
                  || (d >= 19 && d <= 20 && m == Month.September)
               )
                  return false;
            }

            if (y == 2014)
            {
               if (// Lunar New Year
                  (d >= 28 && d <= 30 && m == Month.January)
                  // Spring Festival
                  || ((d == 31 && m == Month.January) || (d <= 4 && m == Month.February))
                  // Children's Day
                  || (d == 4 && m == Month.April)
                  // Tomb Sweeping Day
                  || (d == 5 && m == Month.April)
                  // Labour Day
                  || (d == 1 && m == Month.May)
                  // Dragon Boat Festival
                  || (d == 2 && m == Month.June)
                  // Mid-Autumn Festival
                  || (d == 8 && m == Month.September)
               )
                  return false;
            }
            if (y == 2015)
            {
               if (// adjusted holidays
                   (d == 2 && m == Month.January)
                   // Lunar New Year
                   || (d >= 18 && d <= 23 && m == Month.February)
                   // adjusted holidays
                   || (d == 27 && m == Month.February)
                   // adjusted holidays
                   || (d == 3 && m == Month.April)
                   // adjusted holidays
                   || (d == 6 && m == Month.April)
                   // adjusted holidays
                   || (d == 19 && m == Month.June)
                   // adjusted holidays
                   || (d == 28 && m == Month.September)
                   // adjusted holidays
                   || (d == 9 && m == Month.October)
                   )
                  return false;
            }
            if (y == 2016)
            {
               if (// Lunar New Year
                   (d >= 8 && d <= 12 && m == Month.February)
                   // adjusted holidays
                   || (d == 29 && m == Month.February)
                   // Children's Day
                   || (d == 4 && m == Month.April)
                   // adjusted holidays
                   || (d == 5 && m == Month.April)
                   // adjusted holidays
                   || (d == 2 && m == Month.May)
                   // Dragon Boat Festival
                   || (d == 9 && m == Month.June)
                   // adjusted holidays
                   || (d == 10 && m == Month.June)
                   // Mid-Autumn Festival
                   || (d == 15 && m == Month.September)
                   // adjusted holidays
                   || (d == 16 && m == Month.September)
                   )
                  return false;
            }

            if (y == 2017)
            {
               if (// adjusted holidays
                   (d == 2 && m == Month.January)
                   // Lunar New Year
                   || ((d >= 27 && m == Month.January) || (d == 1 && m == Month.February))
                   // adjusted holidays
                   || (d == 27 && m == Month.February)
                   // adjusted holidays
                   || (d == 3 && m == Month.April)
                   // Children's Day
                   || (d == 4 && m == Month.April)
                   // adjusted holidays
                   || (d == 29 && m == Month.May)
                   // Dragon Boat Festival
                   || (d == 30 && m == Month.May)
                   // Mid-Autumn Festival
                   || (d == 4 && m == Month.October)
                   // adjusted holidays
                   || (d == 9 && m == Month.October)
                   )
                  return false;
            }
            if (y == 2018)
            {
               if (// Lunar New Year
                   (d >= 15 && d <= 20 && m == Month.February)
                   // Children's Day
                   || (d == 4 && m == Month.April)
                   // Tomb Sweeping Day
                   || (d == 5 && m == Month.April)
                   // adjusted holidays
                   || (d == 6 && m == Month.April)
                   // Dragon Boat Festival
                   || (d == 18 && m == Month.June)
                   // Mid-Autumn Festival
                   || (d == 24 && m == Month.September)
                   // adjusted holidays
                   || (d == 31 && m == Month.December)
                   )
                  return false;
            }
            if (y == 2019)
            {
               if (// Lunar New Year
                   (d >= 4 && d <= 8 && m == Month.February)
                   // adjusted holidays
                   || (d == 1 && m == Month.March)
                   // Children's Day
                   || (d == 4 && m == Month.April)
                   // Tomb Sweeping Day
                   || (d == 5 && m == Month.April)
                   // Dragon Boat Festival
                   || (d == 7 && m == Month.June)
                   // Mid-Autumn Festival
                   || (d == 13 && m == Month.September)
                   // adjusted holidays
                   || (d == 11 && m == Month.October)
                   )
                  return false;
            }
            return true;
         }
      }

   }

}
