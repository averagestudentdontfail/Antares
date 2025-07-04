// C# code for CrankNicolsonScheme.cs

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
        private readonly IFdmLinearOpComposite _map;
        private readonly BoundaryConditionSchemeHelper _bcSet;
        private double? _dt;

        public ExplicitEulerScheme(IFdmLinearOpComposite map, IReadOnlyList<IFdmBoundaryCondition> bcSet)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _bcSet = new BoundaryConditionSchemeHelper(bcSet ?? new List<IFdmBoundaryCondition>());
        }

        public void SetStep(double? dt)
        {
            _dt = dt;
        }

        public void Step(ref Array a, double t, double theta)
        {
            QL.Require(_dt.HasValue, "Time step not set");
            
            _map.SetTime(t, t - _dt.Value);
            _bcSet.ApplyBeforeApplying(_map);
            
            var result = _map.Apply(a);
            a = result;
            
            _bcSet.ApplyAfterApplying(a);
        }
    }

    /// <summary>
    /// Implicit Euler scheme for finite difference methods.
    /// </summary>
    public class ImplicitEulerScheme
    {
        public enum SolverType { BiCGstab, GMRES }

        private readonly IFdmLinearOpComposite _map;
        private readonly BoundaryConditionSchemeHelper _bcSet;
        private readonly double _relTol;
        private readonly SolverType _solverType;
        private double? _dt;
        private int _iterations;

        public ImplicitEulerScheme(IFdmLinearOpComposite map, 
                                  IReadOnlyList<IFdmBoundaryCondition> bcSet, 
                                  double relTol, 
                                  SolverType solverType)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _bcSet = new BoundaryConditionSchemeHelper(bcSet ?? new List<IFdmBoundaryCondition>());
            _relTol = relTol;
            _solverType = solverType;
            _iterations = 0;
        }

        public void SetStep(double? dt)
        {
            _dt = dt;
        }

        public void Step(ref Array a, double t, double theta)
        {
            QL.Require(_dt.HasValue, "Time step not set");
            
            _map.SetTime(t, t - _dt.Value);
            _bcSet.ApplyBeforeSolving(_map, a);
            
            // Solve the implicit system
            // This is a simplified implementation - a full version would use iterative solvers
            var result = _map.SolveFor(a);
            a = result;
            _iterations = 1; // Placeholder iteration count
            
            _bcSet.ApplyAfterSolving(a);
        }

        public int NumberOfIterations() => _iterations;
    }

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
        /// Sets the time step for the scheme.
        /// </summary>
        /// <param name="dt">The time step.</param>
        public void SetStep(double? dt)
        {
            _dt = dt;
            _explicitScheme.SetStep(dt);
            _implicitScheme.SetStep(dt);
        }

        /// <summary>
        /// Performs one time step of the Crank-Nicolson scheme.
        /// </summary>
        /// <param name="a">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step.</param>
        public void Step(ref Array a, double t)
        {
            QL.Require(_dt.HasValue, "Time step not set.");
            QL.Require(t - _dt.Value > -1e-8, "A step towards negative time was given.");

            if (_theta.Equals(1.0))
            {
                // Pure implicit Euler
                _implicitScheme.Step(ref a, t, _theta);
            }
            else if (_theta.Equals(0.0))
            {
                // Pure explicit Euler
                _explicitScheme.Step(ref a, t, _theta);
            }
            else
            {
                // Mixed Crank-Nicolson scheme
                // This is a simplified implementation of the weighted combination
                var explicitPart = new Array(a);
                var implicitPart = new Array(a);
                
                _explicitScheme.Step(ref explicitPart, t, 1.0 - _theta);
                _implicitScheme.Step(ref implicitPart, t, _theta);
                
                // Combine the results
                for (int i = 0; i < a.Count; i++)
                {
                    a[i] = (1.0 - _theta) * explicitPart[i] + _theta * implicitPart[i];
                }
            }
        }

        /// <summary>
        /// Returns the number of iterations required for the implicit part.
        /// </summary>
        /// <returns>The number of iterations.</returns>
        public int NumberOfIterations()
        {
            return _implicitScheme.NumberOfIterations();
        }
    }
}