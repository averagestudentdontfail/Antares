// C# code for Fdm1DimSolver.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math;
using Antares.Math.Interpolation;
using Antares.Method.Mesh;
using Antares.Method.Operator;
using Antares.Method.Scheme;
using Antares.Method.Step;
using Antares.Method.Utilities;
using Antares.Pattern;

// Placeholders for dependent types. In a full project, these would be in their own files.
namespace Antares.Method.Solver
{
    /// <summary>
    /// Placeholder for FdmSolverDesc.
    /// Encapsulates the description of an FDM solver's components.
    /// </summary>
    public class FdmSolverDesc
    {
        public FdmMesher Mesher { get; set; }
        public FdmStepConditionComposite Condition { get; set; }
        public IFdmInnerValueCalculator Calculator { get; set; }
        public IReadOnlyList<IFdmBoundaryCondition> BcSet { get; set; }
        public double Maturity { get; set; }
        public int TimeSteps { get; set; }
        public int DampingSteps { get; set; }
    }

    /// <summary>
    /// Placeholder for FdmSchemeDesc.
    /// Describes the FDM scheme to be used (e.g., Douglas, Crank-Nicolson).
    /// </summary>
    public class FdmSchemeDesc
    {
        // In a full implementation, this might be a factory or hold scheme parameters.
        public object Scheme { get; set; } // Generic placeholder
    }

    /// <summary>
    /// Placeholder for FdmBackwardSolver.
    /// The engine that performs the time-stepping rollback.
    /// </summary>
    public class FdmBackwardSolver
    {
        public FdmBackwardSolver(IFdmLinearOpComposite op, IReadOnlyList<IFdmBoundaryCondition> bcSet, FdmStepConditionComposite condition, FdmSchemeDesc schemeDesc) { }
        public void Rollback(ref Array a, double from, double to, int steps, int dampingSteps) { }
    }
}

namespace Antares.Pattern
{
    /// <summary>
    /// Placeholder for LazyObject.
    /// Provides a mechanism for lazy calculation.
    /// </summary>
    public abstract class LazyObject
    {
        private bool _calculated = false;
        protected abstract void PerformCalculations();
        protected void Calculate()
        {
            if (!_calculated)
            {
                PerformCalculations();
                _calculated = true;
            }
        }
    }
}


namespace Antares.Method.Solver
{
    /// <summary>
    /// A solver for one-dimensional FDM problems.
    /// </summary>
    public class Fdm1DimSolver : LazyObject
    {
        private readonly FdmSolverDesc _solverDesc;
        private readonly FdmSchemeDesc _schemeDesc;
        private readonly IFdmLinearOpComposite _op;

        private readonly FdmSnapshotCondition _thetaCondition;
        private readonly FdmStepConditionComposite _conditions;

        private readonly double[] _x;
        private readonly Array _initialValues;
        private Array _resultValues;
        private CubicInterpolation _interpolation;

        public Fdm1DimSolver(FdmSolverDesc solverDesc,
                             FdmSchemeDesc schemeDesc,
                             IFdmLinearOpComposite op)
        {
            _solverDesc = solverDesc;
            _schemeDesc = schemeDesc;
            _op = op;

            double snapshotTime = 0.99 * System.Math.Min(1.0 / 365.0,
                !solverDesc.Condition.StoppingTimes().Any() ?
                    solverDesc.Maturity :
                    solverDesc.Condition.StoppingTimes().First());

            _thetaCondition = new FdmSnapshotCondition(snapshotTime);
            _conditions = FdmStepConditionComposite.JoinConditions(_thetaCondition, solverDesc.Condition);

            int size = solverDesc.Mesher.Layout.Size;
            _x = new double[size];
            _initialValues = new Array(size);
            _resultValues = new Array(size);

            foreach (var iter in (FdmLinearOpLayout)solverDesc.Mesher.Layout)
            {
                int index = iter.Index;
                _initialValues[index] = solverDesc.Calculator.AvgInnerValue(iter, solverDesc.Maturity);
                _x[index] = solverDesc.Mesher.Location(iter, 0);
            }
        }

        /// <summary>
        /// Interpolates the solution at a given spatial point x.
        /// </summary>
        public double InterpolateAt(double x)
        {
            Calculate();
            return _interpolation.Value(x);
        }

        /// <summary>
        /// Calculates the theta (time decay) of the solution at a given spatial point x.
        /// </summary>
        public double? ThetaAt(double x)
        {
            if (_conditions.StoppingTimes().Any() && _conditions.StoppingTimes().First() == 0.0)
                return null;

            Calculate();

            Array thetaValues = _thetaCondition.Values;
            if (thetaValues == null)
                return null; // Snapshot was not taken
            
            var thetaInterp = new MonotonicCubicNaturalSpline(_x, thetaValues._storage.ToArray());
            double temp = thetaInterp.Value(x);
            
            return (temp - InterpolateAt(x)) / _thetaCondition.Time;
        }

        /// <summary>
        /// Calculates the first spatial derivative (delta) of the solution at a given point x.
        /// </summary>
        public double DerivativeX(double x)
        {
            Calculate();
            return _interpolation.Derivative(x);
        }

        /// <summary>
        /// Calculates the second spatial derivative (gamma) of the solution at a given point x.
        /// </summary>
        public double DerivativeXX(double x)
        {
            Calculate();
            return _interpolation.SecondDerivative(x);
        }

        protected override void PerformCalculations()
        {
            Array rhs = _initialValues.Clone();

            // This assumes a C# implementation of FdmBackwardSolver exists.
            var solver = new FdmBackwardSolver(_op, _solverDesc.BcSet, _conditions, _schemeDesc);
            solver.Rollback(ref rhs, _solverDesc.Maturity, 0.0,
                            _solverDesc.TimeSteps, _solverDesc.DampingSteps);

            _resultValues = rhs;
            
            // This assumes a C# implementation of MonotonicCubicNaturalSpline exists.
            _interpolation = new MonotonicCubicNaturalSpline(_x, _resultValues._storage.ToArray());
        }
    }
}