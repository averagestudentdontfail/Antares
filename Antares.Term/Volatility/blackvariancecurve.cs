// Blackvariancecurve.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;
using MathNet.Numerics.Interpolation;

namespace Antares.Term.Volatility
{
    /// <summary>
    /// Black volatility curve modelled as a variance curve.
    /// </summary>
    /// <remarks>
    /// This class calculates time-dependent Black volatilities using as input a
    /// vector of (ATM) Black volatilities observed in the market.
    /// The calculation is performed by interpolating on the variance curve.
    /// Linear interpolation is used as default.
    /// </remarks>
    public class BlackVarianceCurve : BlackVarianceTermStructure
    {
        private readonly DayCounter _dayCounter;
        private readonly Date _maxDate;
        private readonly List<double> _times;
        private readonly List<double> _variances;
        private IInterpolation _varianceCurve;

        public BlackVarianceCurve(
            Date referenceDate,
            IReadOnlyList<Date> dates,
            IReadOnlyList<Volatility> blackVolCurve,
            DayCounter dayCounter,
            bool forceMonotoneVariance = true)
            : base(referenceDate, new NullCalendar(), BusinessDayConvention.Following, dayCounter)
        {
            _dayCounter = dayCounter;
            _maxDate = dates.Last();

            QL.Require(dates.Count == blackVolCurve.Count, "mismatch between date vector and black vol vector");
            QL.Require(dates[0] > referenceDate, "cannot have dates[0] <= referenceDate");

            _variances = new List<double>(dates.Count + 1);
            _times = new List<double>(dates.Count + 1);

            _variances.Add(0.0);
            _times.Add(0.0);

            for (int j = 1; j <= blackVolCurve.Count; j++)
            {
                _times.Add(TimeFromReference(dates[j - 1]));
                QL.Require(_times[j] > _times[j - 1], "dates must be sorted unique!");

                _variances.Add(_times[j] * blackVolCurve[j - 1] * blackVolCurve[j - 1]);
                if (forceMonotoneVariance)
                {
                    QL.Require(_variances[j] >= _variances[j - 1], "variance must be non-decreasing");
                }
            }

            // Default: linear interpolation
            SetInterpolation(Linear.Interpolate(_times, _variances));
        }

        #region TermStructure and VolatilityTermStructure interface overrides
        public override DayCounter DayCounter => _dayCounter;
        public override Date MaxDate => _maxDate;
        public override Rate MinStrike => double.MinValue;
        public override Rate MaxStrike => double.MaxValue;
        #endregion

        #region Modifiers
        /// <summary>
        /// Sets the interpolation method to be used on the variance curve.
        /// </summary>
        public void SetInterpolation(IInterpolation interpolation)
        {
            _varianceCurve = interpolation;
            NotifyObservers();
        }
        #endregion

        #region BlackVarianceTermStructure implementation
        protected override Real BlackVarianceImpl(Time t, Real dummyStrike)
        {
            if (t <= _times.Last())
            {
                return _varianceCurve.Interpolate(t);
            }
            else
            {
                // Extrapolate with flat vol
                return _varianceCurve.Interpolate(_times.Last()) * t / _times.Last();
            }
        }
        #endregion

        #region Visitability
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<BlackVarianceCurve> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                base.Accept(v); // Fall back to BlackVarianceTermStructure visitor
            }
        }
        #endregion
    }
}