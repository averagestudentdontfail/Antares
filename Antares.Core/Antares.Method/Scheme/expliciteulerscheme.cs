// C# code for ExplicitEulerScheme.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Method.Operator;
using Antares.Method.Utilities;

namespace Antares.Method.Scheme
{
    /// <summary>
    /// Explicit Euler scheme for finite difference methods.
    /// </summary>
    public class ExplicitEulerScheme
    {
        private double? _dt;
        private readonly IFdmLinearOpComposite _map;
        private readonly BoundaryConditionSchemeHelper _bcSet;

        /// <summary>
        /// Initializes a new instance of the ExplicitEulerScheme class.
        /// </summary>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        public ExplicitEulerScheme(IFdmLinearOpComposite map,
            IReadOnlyList<IFdmBoundaryCondition> bcSet = null)
        {
            _dt = null;
            _map = map;
            _bcSet = new BoundaryConditionSchemeHelper(bcSet ?? new List<IFdmBoundaryCondition>());
        }

        /// <summary>
        /// Performs one full explicit Euler step.
        /// </summary>
        /// <param name="a">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step.</param>
        public void Step(ref Array a, double t)
        {
            Step(ref a, t, 1.0);
        }

        /// <summary>
        /// Performs a weighted explicit Euler step.
        /// This method is primarily for use by other schemes like Crank-Nicolson.
        /// </summary>
        /// <param name="a">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step.</param>
        public void WeightedStep(ref Array a, double t)
        {
            // Implementation here
        }

        /// <summary>
        /// Performs a weighted explicit Euler step.
        /// This method is primarily for use by other schemes like Crank-Nicolson.
        /// </summary>
        /// <param name="a">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step.</param>
        /// <param name="theta">The weighting factor for the step.</param>
        internal void Step(ref Array a, double t, double theta)
        {
            QL.Require(_dt.HasValue, "Time step not set.");
            QL.Require(t - _dt.Value > -1e-8, "A step towards negative time was given.");

            double currentTime = System.Math.Max(0.0, t - _dt.Value);
            _map.SetTime(currentTime, t);
            _bcSet.SetTime(currentTime);

            _bcSet.ApplyBeforeApplying(_map);
            
            // The core explicit Euler step: a_{n+1} = a_n + dt * L(a_n)
            // The operation is performed in-place on 'a'.
            a.AddAssign((theta * _dt.Value) * _map.Apply(a));
            
            _bcSet.ApplyAfterApplying(a);
        }

        /// <summary>
        /// Sets the time step size for subsequent calls to Step.
        /// </summary>
        /// <param name="dt">The time step size.</param>
        public void SetStep(double dt)
        {
            _dt = dt;
        }
    }
}

// Add necessary extensions to Antares.Math.Array if they don't exist.
namespace Antares.Math
{
    public partial class Array
    {
        /// <summary>
        /// Performs in-place addition: this = this + other.
        /// </summary>
        public void AddAssign(Array other)
        {
            if (this.Count != other.Count)
                throw new ArgumentException("Arrays must have the same size for AddAssign.");
            
            _storage.Add(other._storage, _storage); // result stored in-place
        }
    }
}