// C# code for TrBDF2Scheme.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Math.Functional;
using Antares.Math.MatrixUtilities;
using Antares.Method.Operator;
using Antares.Method.Utilities;

namespace Antares.Method.Scheme
{
    /// <summary>
    /// A generic interface for the first step of the TR-BDF2 scheme.
    /// This allows different trapezoidal-like schemes to be used.
    /// </summary>
    public interface ITrapezoidalScheme
    {
        void SetStep(double dt);
        void Step(ref Array a, double t);
    }

    /// <summary>
    /// Trapezoidal-Backward Differentiation Formula 2 (TR-BDF2) scheme.
    /// </summary>
    /// <remarks>
    /// This is a second-order, L-stable, two-stage scheme. The first stage is a
    /// trapezoidal rule step, and the second stage is a backward differentiation
    /// formula (BDF2) step.
    /// </remarks>
    /// <typeparam name="TTrapezoidalScheme">The type of scheme to use for the first (trapezoidal) stage.</typeparam>
    public class TrBDF2Scheme<TTrapezoidalScheme> where TTrapezoidalScheme : ITrapezoidalScheme
    {
        public enum SolverType
        {
            BiCGstab,
            GMRES
        }

        private double? _dt;
        private double? _beta;
        private int _iterations;

        private readonly double _alpha;
        private readonly IFdmLinearOpComposite _map;
        private readonly TTrapezoidalScheme _trapezoidalScheme;
        private readonly BoundaryConditionSchemeHelper _bcSet;
        private readonly double _relTol;
        private readonly SolverType _solverType;

        /// <summary>
        /// Initializes a new instance of the TrBDF2Scheme class.
        /// </summary>
        /// <param name="alpha">The alpha parameter, controlling the size of the first trapezoidal step.</param>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="trapezoidalScheme">The scheme to use for the first stage.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        /// <param name="relTol">The relative tolerance for the iterative solver.</param>
        /// <param name="solverType">The type of iterative solver to use.</param>
        public TrBDF2Scheme(
            double alpha,
            IFdmLinearOpComposite map,
            TTrapezoidalScheme trapezoidalScheme,
            IReadOnlyList<IFdmBoundaryCondition> bcSet = null,
            double relTol = 1e-8,
            SolverType solverType = SolverType.BiCGstab)
        {
            _dt = null;
            _beta = null;
            _iterations = 0;
            _alpha = alpha;
            _map = map;
            _trapezoidalScheme = trapezoidalScheme;
            _bcSet = new BoundaryConditionSchemeHelper(bcSet ?? new List<IFdmBoundaryCondition>());
            _relTol = relTol;
            _solverType = solverType;
        }

        /// <summary>
        /// Performs one full TR-BDF2 time step.
        /// </summary>
        /// <param name="fn">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step.</param>
        public void Step(ref Array fn, double t)
        {
            QL.Require(_dt.HasValue, "Time step not set.");
            QL.Require(t - _dt.Value > -1e-8, "A step towards negative time was given.");

            double intermediateTimeStep = _dt.Value * _alpha;

            // First stage: Trapezoidal step
            Array fStar = fn.Clone();
            _trapezoidalScheme.SetStep(intermediateTimeStep);
            _trapezoidalScheme.Step(ref fStar, t);

            _bcSet.SetTime(System.Math.Max(0.0, t - _dt.Value));
            _bcSet.ApplyBeforeSolving(_map, fn);

            // Second stage: BDF2 step
            Array f = (1.0 / _alpha * fStar - Functional.Squared(1.0 - _alpha) / _alpha * fn) / (2.0 - _alpha);

            if (_map.Size == 1)
            {
                // For 1D problems, use the direct tridiagonal solver
                fn = _map.SolveSplitting(0, f, -_beta.Value);
            }
            else
            {
                // For multi-dimensional problems, use an iterative solver
                BiCGstab.MatrixMult preconditioner = r => _map.Preconditioner(r, -_beta.Value);
                BiCGstab.MatrixMult applyF = r => Apply(r);

                if (_solverType == SolverType.BiCGstab)
                {
                    var solver = new BiCGstab(applyF, System.Math.Max(10, fn.Count), _relTol, preconditioner);
                    BiCGStabResult result = solver.Solve(f, f); // Use 'f' as both RHS and initial guess

                    _iterations += result.Iterations;
                    fn = result.X;
                }
                else if (_solverType == SolverType.GMRES)
                {
                    var solver = new GMRES(applyF, System.Math.Max(10, fn.Count / 10), _relTol, preconditioner);
                    GMRESResult result = solver.Solve(f, f); // Use 'f' as both RHS and initial guess

                    _iterations += result.Errors.Count;
                    fn = result.X;
                }
                else
                {
                    QL.Fail("Unknown/illegal solver type.");
                }
            }
            _bcSet.ApplyAfterSolving(fn);
        }

        /// <summary>
        /// Sets the time step size for subsequent calls to Step.
        /// </summary>
        /// <param name="dt">The time step size.</param>
        public void SetStep(double dt)
        {
            _dt = dt;
            _beta = (1.0 - _alpha) / (2.0 - _alpha) * dt;
        }

        /// <summary>
        /// Gets the total number of iterations performed by the iterative solver.
        /// </summary>
        public int NumberOfIterations() => _iterations;

        /// <summary>
        /// Applies the operator for the BDF2 stage: (I - beta * L).
        /// </summary>
        private Array Apply(Array r)
        {
            return r - _beta.Value * _map.Apply(r);
        }
    }
}