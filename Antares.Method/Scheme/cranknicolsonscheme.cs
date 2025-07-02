// C# code for CrankNicolsonScheme.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Method.Operator;
using Antares.Method.Utilities;

// Placeholders for dependent types. In a full project, these would be in their own files.
namespace Antares.Method.Scheme
{
    /// <summary>
    /// Placeholder for the ExplicitEulerScheme.
    /// </summary>
    public class ExplicitEulerScheme
    {
        public ExplicitEulerScheme(IFdmLinearOpComposite map, IReadOnlyList<IFdmBoundaryCondition> bcSet) { }
        public void SetStep(double? dt) { }
        public void Step(ref Array a, double t, double theta) { }
    }

    /// <summary>
    /// Placeholder for the ImplicitEulerScheme.
    /// </summary>
    public class ImplicitEulerScheme
    {
        public enum SolverType { BiCGstab, GMRES }
        public ImplicitEulerScheme(IFdmLinearOpComposite map, IReadOnlyList<IFdmBoundaryCondition> bcSet, double relTol, SolverType solverType) { }
        public void SetStep(double? dt) { }
        public void Step(ref Array a, double t, double theta) { }
        public int NumberOfIterations() => 0;
    }
}


namespace Antares.Method.Scheme
{
    /// <summary>
    /// Crank-Nicolson scheme for finite difference methods.
    /// </summary>
    /// <remarks>
    /// In one dimension, the Crank-Nicolson scheme is equivalent to the
    /// Douglas scheme. In higher dimensions, it is usually inferior to
    /// operator splitting methods like Craig-Sneyd or Hundsdorfer-Verwer.
    ///
    /// This scheme is a weighted average of the explicit and implicit Euler schemes.
    /// </remarks>
    public class CrankNicolsonScheme
    {
        private double? _dt;
        private readonly double _theta;
        private readonly ExplicitEulerScheme _explicitScheme;
        private readonly ImplicitEulerScheme _implicitScheme;

        /// <summary>
        /// Initializes a new instance of the CrankNicolsonScheme class.
        /// </summary>
        /// <param name="theta">The weighting factor. theta=0.0 is fully explicit, theta=1.0 is fully implicit, and theta=0.5 is the standard Crank-Nicolson.</param>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        /// <param name="relTol">The relative tolerance for the implicit solver.</param>
        /// <param name="solverType">The type of iterative solver to use for the implicit part.</param>
        public CrankNicolsonScheme(
            double theta,
            IFdmLinearOpComposite map,
            IReadOnlyList<IFdmBoundaryCondition> bcSet = null,
            double relTol = 1e-8,
            ImplicitEulerScheme.SolverType solverType = ImplicitEulerScheme.SolverType.BiCGstab)
        {
            _dt = null;
            _theta = theta;
            
            // The scheme is implemented by composing the explicit and implicit Euler schemes.
            _explicitScheme = new ExplicitEulerScheme(map, bcSet);
            _implicitScheme = new ImplicitEulerScheme(map, bcSet, relTol, solverType);
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

            // Apply the explicit part of the scheme with weight (1-theta)
            if (_theta != 1.0)
            {
                _explicitScheme.Step(ref a, t, 1.0 - _theta);
            }

            // Apply the implicit part of the scheme with weight theta
            if (_theta != 0.0)
            {
                _implicitScheme.Step(ref a, t, _theta);
            }
        }

        /// <summary>
        /// Sets the time step size for subsequent calls to Step.
        /// </summary>
        /// <param name="dt">The time step size.</param>
        public void SetStep(double dt)
        {
            _dt = dt;
            _explicitScheme.SetStep(_dt);
            _implicitScheme.SetStep(_dt);
        }

        /// <summary>
        /// Gets the number of iterations performed by the implicit solver in the last step.
        /// </summary>
        public int NumberOfIterations()
        {
            return _implicitScheme.NumberOfIterations();
        }
    }
}