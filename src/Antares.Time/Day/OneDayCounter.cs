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

namespace Antares
{
   //! 1/1 day count convention
   public class OneDayCounter : DayCounter
   {
      public OneDayCounter() : base(new Impl()) { }

      private class Impl : DayCounterImpl
      {
         public override string name() { return "1/1"; }
         public override int dayCount(Date d1, Date d2)
         {
            // the sign is all we need
            return (d2 >= d1 ? 1 : -1);
         }

         public override double yearFraction(Date d1, Date d2, Date refPeriodStart, Date refPeriodEnd)
         {
            return dayCount(d1, d2);
         }
      }
   }
}
