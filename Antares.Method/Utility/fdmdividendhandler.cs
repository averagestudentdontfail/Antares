// C# code for FdmDividendHandler.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Time;

namespace Antares.Method.Utility
{
    /// <summary>
    /// Interface for FDM layout information
    /// </summary>
    public interface IFdmLinearOpLayout
    {
        /// <summary>
        /// Gets the dimensions of the layout
        /// </summary>
        List<Size> Dim { get; }

        /// <summary>
        /// Gets the spacing information for each dimension
        /// </summary>
        List<Size> Spacing { get; }
    }

    /// <summary>
    /// Interface for finite difference mesher
    /// </summary>
    public interface IFdmMesher
    {
        /// <summary>
        /// Gets the layout information
        /// </summary>
        IFdmLinearOpLayout Layout { get; }

        /// <summary>
        /// Gets the locations for a specific direction
        /// </summary>
        /// <param name="direction">The direction</param>
        /// <returns>Array of locations</returns>
        Array Locations(Size direction);
    }

    /// <summary>
    /// Dividend handler for FDM method for one equity direction
    /// </summary>
    public class FdmDividendHandler : StepCondition<Array>
    {
        private readonly Array _x; // grid-equity values in physical units
        private readonly List<Time> _dividendTimes;
        private readonly List<Date> _dividendDates;
        private readonly List<Real> _dividends;
        private readonly IFdmMesher _mesher;
        private readonly Size _equityDirection;

        /// <summary>
        /// Constructs a dividend handler for finite difference methods
        /// </summary>
        /// <param name="schedule">The dividend schedule</param>
        /// <param name="mesher">The finite difference mesher</param>
        /// <param name="referenceDate">The reference date for time calculations</param>
        /// <param name="dayCounter">The day counter for time calculations</param>
        /// <param name="equityDirection">The equity direction in the mesh</param>
        public FdmDividendHandler(
            DividendSchedule schedule,
            IFdmMesher mesher,
            Date referenceDate,
            DayCounter dayCounter,
            Size equityDirection)
        {
            _mesher = mesher ?? throw new ArgumentNullException(nameof(mesher));
            _equityDirection = equityDirection;
            
            _x = new Array((int)mesher.Layout.Dim[(int)equityDirection]);
            
            _dividends = new List<Real>();
            _dividendDates = new List<Date>();
            _dividendTimes = new List<Time>();

            // Process the dividend schedule
            if (schedule != null)
            {
                _dividends.Capacity = schedule.Count;
                _dividendDates.Capacity = schedule.Count;
                _dividendTimes.Capacity = schedule.Count;
                
                foreach (var dividend in schedule)
                {
                    _dividends.Add(dividend.Amount);
                    _dividendDates.Add(dividend.Date);
                    _dividendTimes.Add(dayCounter.yearFraction(referenceDate, dividend.Date));
                }
            }

            // Convert log-space locations to physical equity values
            Array tmp = mesher.Locations(equityDirection);
            Size spacing = mesher.Layout.Spacing[(int)equityDirection];
            
            for (Size i = 0; i < _x.Count; ++i)
            {
                _x[i] = Math.Exp(tmp[i * spacing]);
            }
        }

        /// <summary>
        /// Gets the dividend times
        /// </summary>
        public IReadOnlyList<Time> DividendTimes => _dividendTimes;

        /// <summary>
        /// Gets the dividend dates
        /// </summary>
        public IReadOnlyList<Date> DividendDates => _dividendDates;

        /// <summary>
        /// Gets the dividend amounts
        /// </summary>
        public IReadOnlyList<Real> Dividends => _dividends;

        /// <summary>
        /// Applies the dividend adjustment to the given array at time t
        /// </summary>
        /// <param name="a">The array to modify</param>
        /// <param name="t">The time at which to apply dividends</param>
        public override void ApplyTo(Array a, Time t)
        {
            var aCopy = a.Clone();

            // Find if there's a dividend at time t
            int dividendIndex = _dividendTimes.FindIndex(time => 
                Math.Abs(time - t) < QLDefines.EPSILON);

            if (dividendIndex >= 0)
            {
                Real dividend = _dividends[dividendIndex];

                if (_mesher.Layout.Dim.Count == 1)
                {
                    // 1D case
                    ApplyDividendAdjustment1D(a, aCopy, dividend);
                }
                else
                {
                    // Multi-dimensional case
                    ApplyDividendAdjustmentMultiD(a, aCopy, dividend);
                }
            }
        }

        /// <summary>
        /// Applies dividend adjustment for 1D case
        /// </summary>
        private void ApplyDividendAdjustment1D(Array a, Array aCopy, Real dividend)
        {
            // Create arrays for interpolation
            var xValues = new double[_x.Count];
            var yValues = new double[_x.Count];
            
            for (int i = 0; i < _x.Count; i++)
            {
                xValues[i] = _x[i];
                yValues[i] = aCopy[i];
            }

            var interp = new Antares.Math.Interpolation.LinearInterpolation(xValues, yValues);
            interp.Update();

            for (Size k = 0; k < _x.Count; ++k)
            {
                Real adjustedSpot = Math.Max(_x[0], _x[k] - dividend);
                a[k] = interp.Value(adjustedSpot, true); // true for extrapolation
            }
        }

        /// <summary>
        /// Applies dividend adjustment for multi-dimensional case
        /// </summary>
        private void ApplyDividendAdjustmentMultiD(Array a, Array aCopy, Real dividend)
        {
            var tmp = new Array((int)_x.Count);
            Size xSpacing = _mesher.Layout.Spacing[(int)_equityDirection];

            for (Size i = 0; i < _mesher.Layout.Dim.Count; ++i)
            {
                if (i != _equityDirection)
                {
                    Size ySpacing = _mesher.Layout.Spacing[(int)i];
                    
                    for (Size j = 0; j < _mesher.Layout.Dim[(int)i]; ++j)
                    {
                        // Extract slice for interpolation
                        for (Size k = 0; k < _x.Count; ++k)
                        {
                            Size index = j * ySpacing + k * xSpacing;
                            tmp[k] = aCopy[index];
                        }

                        // Create arrays for interpolation
                        var xValues = new double[_x.Count];
                        var yValues = new double[_x.Count];
                        
                        for (int idx = 0; idx < _x.Count; idx++)
                        {
                            xValues[idx] = _x[idx];
                            yValues[idx] = tmp[idx];
                        }

                        var interp = new Antares.Math.Interpolation.LinearInterpolation(xValues, yValues);
                        interp.Update();

                        // Apply interpolation back to array
                        for (Size k = 0; k < _x.Count; ++k)
                        {
                            Size index = j * ySpacing + k * xSpacing;
                            Real adjustedSpot = Math.Max(_x[0], _x[k] - dividend);
                            a[index] = interp.Value(adjustedSpot, true); // true for extrapolation
                        }
                    }
                }
            }
        }
    }
}