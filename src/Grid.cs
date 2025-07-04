// doubleGrid.cs

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
            return CloseEnough(x, y, Define.EPSILON);
        }
    }

    /// <summary>
    /// double grid class
    /// </summary>
    /// <remarks>
    /// The original C++ code limited the grid to positive times. This implementation
    /// maintains that constraint.
    /// </remarks>
    public class doubleGrid : IReadOnlyList<double>
    {
        private readonly List<double> _times;
        private readonly List<double> _dt;
        private readonly List<double> _mandatorydoubles;

        #region Constructors
        /// <summary>
        /// Creates an empty time grid.
        /// </summary>
        public doubleGrid()
        {
            _times = new List<double>();
            _dt = new List<double>();
            _mandatorydoubles = new List<double>();
        }

        /// <summary>
        /// Creates a regularly spaced time-grid.
        /// </summary>
        /// <param name="end">The final time of the grid.</param>
        /// <param name="steps">The number of steps in the grid.</param>
        public doubleGrid(double end, Size steps)
        {
            if (end <= 0.0)
                throw new ArgumentException("Negative or zero end time not allowed");

            double dt = end / steps;
            _times = new List<double>(steps + 1);
            for (Size i = 0; i <= steps; i++)
                _times.Add(dt * i);

            _mandatorydoubles = new List<double> { end };

            _dt = new List<double>(steps);
            for(int i = 0; i < steps; ++i)
                _dt.Add(dt);
        }

        /// <summary>
        /// Creates a time grid with mandatory time points.
        /// Mandatory points are guaranteed to belong to the grid.
        /// No additional points are added.
        /// </summary>
        /// <param name="times">The sequence of mandatory times.</param>
        public doubleGrid(IEnumerable<double> times)
        {
            _mandatorydoubles = times.ToList();
            if (_mandatorydoubles.Count == 0)
                throw new ArgumentException("Empty time sequence");

            _mandatorydoubles.Sort();
            if (_mandatorydoubles[0] < 0.0)
                throw new ArgumentException("Negative times not allowed");

            // Remove adjacent duplicates
            var uniqueMandatorydoubles = new List<double> { _mandatorydoubles[0] };
            for (int i = 1; i < _mandatorydoubles.Count; ++i)
            {
                if (!Comparison.CloseEnough(_mandatorydoubles[i], uniqueMandatorydoubles.Last()))
                {
                    uniqueMandatorydoubles.Add(_mandatorydoubles[i]);
                }
            }
            _mandatorydoubles = uniqueMandatorydoubles;

            _times = new List<double>();
            if (_mandatorydoubles[0] > 0.0)
                _times.Add(0.0);

            _times.AddRange(_mandatorydoubles);

            _dt = new List<double>(_times.Count > 0 ? _times.Count - 1 : 0);
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
        public doubleGrid(IEnumerable<double> mandatorydoubles, Size steps)
        {
            _mandatorydoubles = mandatorydoubles.ToList();
            if (_mandatorydoubles.Count == 0)
                throw new ArgumentException("Empty time sequence");

            _mandatorydoubles.Sort();
            if (_mandatorydoubles[0] < 0.0)
                throw new ArgumentException("Negative times not allowed");

            // Remove adjacent duplicates
            var uniqueMandatorydoubles = new List<double> { _mandatorydoubles[0] };
            for (int i = 1; i < _mandatorydoubles.Count; ++i)
            {
                if (!Comparison.CloseEnough(_mandatorydoubles[i], uniqueMandatorydoubles.Last()))
                {
                    uniqueMandatorydoubles.Add(_mandatorydoubles[i]);
                }
            }
            _mandatorydoubles = uniqueMandatorydoubles;

            double last = _mandatorydoubles.Last();
            double dtMax;

            if (steps == 0)
            {
                var diff = new List<double>();
                for (int i = 1; i < _mandatorydoubles.Count; ++i)
                    diff.Add(_mandatorydoubles[i] - _mandatorydoubles[i - 1]);
                
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

            _times = new List<double>();
            double periodBegin = 0.0;
            _times.Add(periodBegin);
            foreach (var t in _mandatorydoubles)
            {
                double periodEnd = t;
                if (periodEnd > 0.0 && !Comparison.CloseEnough(periodBegin, periodEnd))
                {
                    // at least 1 step
                    Size nSteps = Math.Max(1, (int)Math.Round((periodEnd - periodBegin) / dtMax));
                    double dt = (periodEnd - periodBegin) / nSteps;
                    for (Size n = 1; n <= nSteps; ++n)
                        _times.Add(periodBegin + n * dt);
                }
                periodBegin = periodEnd;
            }

            _dt = new List<double>(_times.Count > 0 ? _times.Count - 1 : 0);
            for (int i = 1; i < _times.Count; ++i)
                _dt.Add(_times[i] - _times[i - 1]);
        }
        #endregion

        #region double grid interface
        /// <summary>
        /// Returns the index i such that grid[i] = t.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown if t is not on the grid.</exception>
        public int index(double t)
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
        public int closestIndex(double t)
        {
            int result = _times.BinarySearch(t);

            if (result >= 0)
                return result;

            int insertionPoint = ~result;

            if (insertionPoint == 0)
                return 0;
            if (insertionPoint == _times.Count)
                return _times.Count - 1;

            double dt1 = _times[insertionPoint] - t;
            double dt2 = t - _times[insertionPoint - 1];

            return dt1 < dt2 ? insertionPoint : insertionPoint - 1;
        }

        /// <summary>
        /// Returns the time on the grid closest to the given t.
        /// </summary>
        public double closestdouble(double t) => _times[closestIndex(t)];

        /// <summary>
        /// Returns the list of mandatory times used to build the grid.
        /// </summary>
        public IReadOnlyList<double> mandatorydoubles() => _mandatorydoubles;

        /// <summary>
        /// Returns the time step delta at a given index i.
        /// </summary>
        public double dt(int i) => _dt[i];
        #endregion

        #region IReadOnlyList<double> implementation
        public double this[int index] => _times[index];
        public int Count => _times.Count;
        public bool empty() => _times.Count == 0;
        public Size size() => _times.Count;
        public double front() => _times[0];
        public double back() => _times[_times.Count - 1];

        public IEnumerator<double> GetEnumerator() => _times.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}