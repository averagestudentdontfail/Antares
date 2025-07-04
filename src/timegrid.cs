// TimeGrid.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Antares
{
    /// <summary>
    /// Provides mathematical comparison utilities.
    /// </summary>
    public static class Comparison
    {
        /// <summary>
        /// Tests whether two floating point numbers are close enough to be considered equal.
        /// </summary>
        public static bool CloseEnough(double x, double y, double tolerance = 1e-15)
        {
            return Math.Abs(x - y) <= tolerance;
        }

        /// <summary>
        /// Tests whether two floating point numbers are close.
        /// </summary>
        public static bool Close(double x, double y)
        {
            return CloseEnough(x, y, QLDefines.EPSILON);
        }
    }

    /// <summary>
    /// Time grid class
    /// </summary>
    /// <remarks>
    /// The original C++ code limited the grid to positive times. This implementation
    /// maintains that constraint.
    /// </remarks>
    public class TimeGrid : IReadOnlyList<Time>
    {
        private readonly List<Time> _times;
        private readonly List<Time> _dt;
        private readonly List<Time> _mandatoryTimes;

        #region Constructors
        /// <summary>
        /// Creates an empty time grid.
        /// </summary>
        public TimeGrid()
        {
            _times = new List<Time>();
            _dt = new List<Time>();
            _mandatoryTimes = new List<Time>();
        }

        /// <summary>
        /// Creates a regularly spaced time-grid.
        /// </summary>
        /// <param name="end">The final time of the grid.</param>
        /// <param name="steps">The number of steps in the grid.</param>
        public TimeGrid(Time end, Size steps)
        {
            if (end <= 0.0)
                throw new ArgumentException("Negative or zero end time not allowed");

            Time dt = end / steps;
            _times = new List<Time>(steps + 1);
            for (Size i = 0; i <= steps; i++)
                _times.Add(dt * i);

            _mandatoryTimes = new List<Time> { end };

            _dt = new List<Time>(steps);
            for(int i = 0; i < steps; ++i)
                _dt.Add(dt);
        }

        /// <summary>
        /// Creates a time grid with mandatory time points.
        /// Mandatory points are guaranteed to belong to the grid.
        /// No additional points are added.
        /// </summary>
        /// <param name="times">The sequence of mandatory times.</param>
        public TimeGrid(IEnumerable<Time> times)
        {
            _mandatoryTimes = times.ToList();
            if (_mandatoryTimes.Count == 0)
                throw new ArgumentException("Empty time sequence");

            _mandatoryTimes.Sort();
            if (_mandatoryTimes[0] < 0.0)
                throw new ArgumentException("Negative times not allowed");

            // Remove adjacent duplicates
            var uniqueMandatoryTimes = new List<Time> { _mandatoryTimes[0] };
            for (int i = 1; i < _mandatoryTimes.Count; ++i)
            {
                if (!Comparison.CloseEnough(_mandatoryTimes[i], uniqueMandatoryTimes.Last()))
                {
                    uniqueMandatoryTimes.Add(_mandatoryTimes[i]);
                }
            }
            _mandatoryTimes = uniqueMandatoryTimes;

            _times = new List<Time>();
            if (_mandatoryTimes[0] > 0.0)
                _times.Add(0.0);

            _times.AddRange(_mandatoryTimes);

            _dt = new List<Time>(_times.Count > 0 ? _times.Count - 1 : 0);
            for (int i = 1; i < _times.Count; ++i)
                _dt.Add(_times[i] - _times[i - 1]);
        }

        /// <summary>
        /// Creates a time grid with mandatory time points.
        /// Mandatory points are guaranteed to belong to the grid.
        /// Additional points are then added with regular spacing
        /// between pairs of mandatory times in order to reach the
        /// desired number of steps.
        /// </summary>
        /// <param name="times">The sequence of mandatory times.</param>
        /// <param name="steps">The total number of steps to aim for.</param>
        public TimeGrid(IEnumerable<Time> mandatoryTimes, Size steps)
        {
            _mandatoryTimes = mandatoryTimes.ToList();
            if (_mandatoryTimes.Count == 0)
                throw new ArgumentException("Empty time sequence");

            _mandatoryTimes.Sort();
            if (_mandatoryTimes[0] < 0.0)
                throw new ArgumentException("Negative times not allowed");

            // Remove adjacent duplicates
            var uniqueMandatoryTimes = new List<Time> { _mandatoryTimes[0] };
            for (int i = 1; i < _mandatoryTimes.Count; ++i)
            {
                if (!Comparison.CloseEnough(_mandatoryTimes[i], uniqueMandatoryTimes.Last()))
                {
                    uniqueMandatoryTimes.Add(_mandatoryTimes[i]);
                }
            }
            _mandatoryTimes = uniqueMandatoryTimes;

            Time last = _mandatoryTimes.Last();
            Time dtMax;

            if (steps == 0)
            {
                var diff = new List<Time>();
                for (int i = 1; i < _mandatoryTimes.Count; ++i)
                    diff.Add(_mandatoryTimes[i] - _mandatoryTimes[i - 1]);
                
                if (diff.Count == 0)
                    throw new ArgumentException("At least two distinct points required in time grid");
                
                dtMax = diff.Min();
                if (dtMax <= 0)
                     throw new ArgumentException("Not enough distinct points in time grid");
            }
            else
            {
                dtMax = last / steps;
            }

            _times = new List<Time>();
            Time periodBegin = 0.0;
            _times.Add(periodBegin);
            foreach (var t in _mandatoryTimes)
            {
                Time periodEnd = t;
                if (periodEnd > 0.0 && !Comparison.CloseEnough(periodBegin, periodEnd))
                {
                    // at least 1 step
                    Size nSteps = Math.Max(1, (int)Math.Round((periodEnd - periodBegin) / dtMax));
                    Time dt = (periodEnd - periodBegin) / nSteps;
                    for (Size n = 1; n <= nSteps; ++n)
                        _times.Add(periodBegin + n * dt);
                }
                periodBegin = periodEnd;
            }

            _dt = new List<Time>(_times.Count > 0 ? _times.Count - 1 : 0);
            for (int i = 1; i < _times.Count; ++i)
                _dt.Add(_times[i] - _times[i - 1]);
        }
        #endregion

        #region Time grid interface
        /// <summary>
        /// Returns the index i such that grid[i] = t.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if t is not on the grid.</exception>
        public int index(Time t)
        {
            int i = closestIndex(t);
            if (Comparison.Close(t, _times[i]))
            {
                return i;
            }

            if (t < _times.First())
            {
                throw new ArgumentException(
                    $"Using inadequate time grid: all nodes are later than the required time t = {t:F12} (earliest node is t1 = {_times.First():F12})");
            }
            if (t > _times.Last())
            {
                throw new ArgumentException(
                    $"Using inadequate time grid: all nodes are earlier than the required time t = {t:F12} (latest node is t1 = {_times.Last():F12})");
            }
            
            int j, k;
            if (t > _times[i])
            {
                j = i;
                k = i + 1;
            }
            else
            {
                j = i - 1;
                k = i;
            }
            throw new ArgumentException(
                $"Using inadequate time grid: the nodes closest to the required time t = {t:F12} are t1 = {_times[j]:F12} and t2 = {_times[k]:F12}");
        }

        /// <summary>
        /// Returns the index i such that grid[i] is closest to t.
        /// </summary>
        public int closestIndex(Time t)
        {
            int result = _times.BinarySearch(t);

            if (result >= 0)
                return result;

            int insertionPoint = ~result;

            if (insertionPoint == 0)
                return 0;
            if (insertionPoint == _times.Count)
                return _times.Count - 1;

            Time dt1 = _times[insertionPoint] - t;
            Time dt2 = t - _times[insertionPoint - 1];

            return dt1 < dt2 ? insertionPoint : insertionPoint - 1;
        }

        /// <summary>
        /// Returns the time on the grid closest to the given t.
        /// </summary>
        public Time closestTime(Time t) => _times[closestIndex(t)];

        /// <summary>
        /// Returns the list of mandatory times used to build the grid.
        /// </summary>
        public IReadOnlyList<Time> mandatoryTimes() => _mandatoryTimes;

        /// <summary>
        /// Returns the time step delta at a given index i.
        /// </summary>
        public Time dt(int i) => _dt[i];
        #endregion

        #region IReadOnlyList<Time> implementation
        public Time this[int index] => _times[index];
        public int Count => _times.Count;
        public bool empty() => _times.Count == 0;
        public Size size() => _times.Count;
        public Time front() => _times[0];
        public Time back() => _times[_times.Count - 1];

        public IEnumerator<Time> GetEnumerator() => _times.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}