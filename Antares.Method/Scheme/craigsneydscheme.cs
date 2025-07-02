// C# code for CraigSneydScheme.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Method.Operator;
using Antares.Method.Utilities;

// Placeholders for dependent types. In a full project, these would be in their own files.
namespace Antares.Method.Operator
{
    /// <summary>
    /// Placeholder for the FdmLinearOpComposite interface.
    /// Represents a composite linear operator for FDM, composed of operators for each dimension.
    /// </summary>
    public interface IFdmLinearOpComposite : IFdmLinearOp
    {
        int Size { get; }
        void SetTime(double t1, double t2);
        Array ApplyDirection(int direction, Array r);
        Array ApplyMixed(Array r);
        Array SolveSplitting(int direction, Array r, double s);
    }
}


namespace Antares.Method.Scheme
{
    /// <summary>
    /// Craig-Sneyd operator splitting scheme.
    /// </summary>
    /// <remarks>
    /// This is a high-order ADI (Alternating Direction Implicit) scheme for solving
    /// multi-dimensional parabolic PDEs. It is known for its stability properties.
    /// </remarks>
    public class CraigSneydScheme
    {
        private double? _dt;
        private readonly double _theta;
        private readonly double _mu;
        private readonly IFdmLinearOpComposite _map;
        private readonly BoundaryConditionSchemeHelper _bcSet;

        /// <summary>
        /// Initializes a new instance of the CraigSneydScheme class.
        /// </summary>
        /// <param name="theta">The theta parameter of the scheme.</param>
        /// <param name="mu">The mu parameter of the scheme.</param>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        public CraigSneydScheme(double theta, double mu,
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
            Array yt = y0 + _mu * _dt.Value * _map.ApplyMixed(y - a);
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