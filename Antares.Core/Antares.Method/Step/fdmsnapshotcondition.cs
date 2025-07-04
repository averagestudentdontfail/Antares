// C# code for FdmSnapshotCondition.cs

using System;
using Antares.Math;
using Antares.Method;

namespace Antares.Method.Step
{
    /// <summary>
    /// Step condition for taking a snapshot of the solution array at a specific time.
    /// </summary>
    /// <remarks>
    /// This condition checks if the current time step matches a pre-defined time.
    /// If it does, it saves a copy of the solution array for later inspection.
    /// </remarks>
    public class FdmSnapshotCondition : IStepCondition<Array>
    {
        private readonly double _time;
        private Array _values;

        /// <summary>
        /// Initializes a new instance of the FdmSnapshotCondition class.
        /// </summary>
        /// <param name="t">The specific time at which to take the snapshot.</param>
        public FdmSnapshotCondition(double t)
        {
            _time = t;
            _values = null; // Initially no snapshot has been taken
        }

        /// <summary>
        /// Gets the time at which the snapshot is taken.
        /// </summary>
        public double Time => _time;

        /// <summary>
        /// Gets the solution array values at the snapshot time.
        /// Returns null if the snapshot has not yet been taken.
        /// </summary>
        public Array Values => _values;

        /// <summary>
        /// Checks if the current time `t` matches the snapshot time.
        /// If it matches, a copy of the solution array `a` is stored.
        /// </summary>
        /// <param name="a">The solution array at the current time step.</param>
        /// <param name="t">The current time.</param>
        public void ApplyTo(Array a, double t)
        {
            // Use a tolerance for floating-point comparison
            if (System.Math.Abs(t - _time) < 1e-12)
            {
                // Store a copy, not a reference, to capture the state at this specific time.
                _values = a.Clone();
            }
        }
    }
}