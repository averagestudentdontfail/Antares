// C# code for ImplicitEulerScheme.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Math.MatrixUtilities;
using Antares.Method.Operator;
using Antares.Method.Utilities;

namespace Antares.Method.Scheme
{
    /// <summary>
    /// Implicit-Euler scheme for finite difference methods.
    /// </summary>
    /// <remarks>
    /// This scheme solves the linear system (I - theta * dt * L) u_t = u_{t-1},
    /// where L is the FDM operator. It is unconditionally stable.
    /// </remarks>
    public class ImplicitEulerScheme
    {
        public enum SolverType
        {
            BiCGstab,
            GMRES
        }

        private double? _dt;
        private int _iterations; // Using a simple int instead of shared_ptr<Size>

        private readonly double _relTol;
        private readonly IFdmLinearOpComposite _map;
        private readonly BoundaryConditionSchemeHelper _bcSet;
        private readonly SolverType _solverType;

        /// <summary>
        /// Initializes a new instance of the ImplicitEulerScheme class.
        /// </summary>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        /// <param name="relTol">The relative tolerance for the iterative solver.</param>
        /// <param name="solverType">The type of iterative solver to use.</param>
        public ImplicitEulerScheme(
            IFdmLinearOpComposite map,
            IReadOnlyList<IFdmBoundaryCondition> bcSet = null,
            double relTol = 1e-8,
            SolverType solverType = SolverType.BiCGstab)
        {
            _dt = null;
            _iterations = 0;
            _relTol = relTol;
            _map = map;
            _bcSet = new BoundaryConditionSchemeHelper(bcSet ?? new List<IFdmBoundaryCondition>());
            _solverType = solverType;
        }

        /// <summary>
        /// Performs one full implicit Euler step.
        /// </summary>
        /// <param name="a">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step.</param>
        public void Step(ref Array a, double t)
        {
            Step(ref a, t, 1.0);
        }

        /// <summary>
        /// Sets the time step size for subsequent calls to Step.
        /// </summary>
        /// <param name="dt">The time step size.</param>
        public void SetStep(double dt)
        {
            _dt = dt;
        }

        /// <summary>
        /// Gets the total number of iterations performed by the iterative solver.
        /// </summary>
        public int NumberOfIterations() => _iterations;

        /// <summary>
        /// Performs a weighted implicit Euler step.
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

            _bcSet.ApplyBeforeSolving(_map, a);

            if (_map.Size == 1)
            {
                // For 1D problems, use the direct tridiagonal solver
                a = _map.SolveSplitting(0, a, -theta * _dt.Value);
            }
            else
            {
                // For multi-dimensional problems, use an iterative solver
                BiCGstab.MatrixMult preconditioner = r => _map.Preconditioner(r, -theta * _dt.Value);
                BiCGstab.MatrixMult applyF = r => Apply(r, theta);

                if (_solverType == SolverType.BiCGstab)
                {
                    var solver = new BiCGstab(applyF, System.Math.Max(10, a.Count), _relTol, preconditioner);
                    BiCGStabResult result = solver.Solve(a, a); // Use current 'a' as both RHS and initial guess

                    _iterations += result.Iterations;
                    a = result.X;
                }
                else if (_solverType == SolverType.GMRES)
                {
                    var solver = new GMRES(applyF, System.Math.Max(10, a.Count / 10), _relTol, preconditioner);
                    GMRESResult result = solver.Solve(a, a); // Use current 'a' as both RHS and initial guess

                    _iterations += result.Errors.Count;
                    a = result.X;
                }
                else
                {
                    QL.Fail("Unknown/illegal solver type.");
                }
            }
            _bcSet.ApplyAfterSolving(a);
        }
        
        /// <summary>
        /// Applies the operator (I - theta * dt * L) to a vector.
        /// This is the "matrix" part of the linear system to be solved.
        /// </summary>
        private Array Apply(Array r, double theta)
        {
            return r - (theta * _dt.Value) * _map.Apply(r);
        }
    }
}