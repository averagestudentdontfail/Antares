// C# code for ModifiedCraigSneydScheme.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Method.Operator;
using Antares.Method.Utilities;

namespace Antares.Method.Scheme
{
    /// <summary>
    /// Modified Craig-Sneyd operator splitting scheme.
    /// </summary>
    /// <remarks>
    /// This is an Alternating Direction Implicit (ADI) scheme, often used for
    /// its stability properties in multi-dimensional problems, particularly
    /// those with mixed-derivative terms.
    ///
    /// References:
    /// K. J. in ’t Hout and S. Foulon,
    /// ADI finite difference schemes for option pricing in the Heston
    /// model with correlation, http://arxiv.org/pdf/0811.3427
    /// </remarks>
    public class ModifiedCraigSneydScheme
    {
        private double? _dt;
        private readonly double _theta;
        private readonly double _mu;
        private readonly IFdmLinearOpComposite _map;
        private readonly BoundaryConditionSchemeHelper _bcSet;

        /// <summary>
        /// Initializes a new instance of the ModifiedCraigSneydScheme class.
        /// </summary>
        /// <param name="theta">The theta parameter of the scheme.</param>
        /// <param name="mu">The mu parameter of the scheme.</param>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        public ModifiedCraigSneydScheme(double theta, double mu,
            IFdmLinearOpComposite map,
            IReadOnlyList<IFdmBoundaryCondition> bcSet = null)
        {
            _dt = null;
            _theta = theta;
            _mu = mu;
            _map = map;
            _bcSet = new BoundaryConditionSchemeHelper(bcSet ?? new List<IFdmBoundaryCondition>());
        }

        /// <summary>
        /// Performs one time step of the scheme.
        /// </summary>
        /// <param name="a">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step.</param>
        public void Step(ref Array a, double t)
        {
            QL.Require(_dt.HasValue, "Time step not set.");
            QL.Require(t - _dt.Value > -1e-8, "A step towards negative time was given.");

            double currentTime = System.Math.Max(0.0, t - _dt.Value);
            _map.SetTime(currentTime, t);
            _bcSet.SetTime(currentTime);

            _bcSet.ApplyBeforeApplying(_map);
            Array y = a + _dt.Value * _map.Apply(a);
            _bcSet.ApplyAfterApplying(y);

            Array y0 = y.Clone();

            for (int i = 0; i < _map.Size; ++i)
            {
                Array rhs = y - _theta * _dt.Value * _map.ApplyDirection(i, a);
                y = _map.SolveSplitting(i, rhs, -_theta * _dt.Value);
            }

            _bcSet.ApplyBeforeApplying(_map);
            
            // The core modification from the standard Craig-Sneyd scheme is in this step
            Array yDiff = y - a;
            Array yt = y0 + _mu * _dt.Value * _map.ApplyMixed(yDiff)
                          + (0.5 - _mu) * _dt.Value * _map.Apply(yDiff);
            
            _bcSet.ApplyAfterApplying(yt);

            for (int i = 0; i < _map.Size; ++i)
            {
                Array rhs = yt - _theta * _dt.Value * _map.ApplyDirection(i, a);
                yt = _map.SolveSplitting(i, rhs, -_theta * _dt.Value);
            }
            _bcSet.ApplyAfterSolving(yt);

            a = yt;
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