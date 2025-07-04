// C# code for FdmBlackScholesSolver.cs

using System;
using Antares.Math;
using Antares.Method.Finitedifferences;
using Antares.Method.Finitedifferences.Operators;
using Antares.Method.Finitedifferences.Utilities;
using Antares.Patterns;
using Antares.Processes;
using Antares.Time;

namespace Antares.Method.Solver
{
    /// <summary>
    /// FDM solver for the Black-Scholes equation.
    /// </summary>
    public class FdmBlackScholesSolver : LazyObject
    {
        private readonly Handle<GeneralizedBlackScholesProcess> _process;
        private readonly double _strike;
        private readonly FdmSolverDesc _solverDesc;
        private readonly FdmSchemeDesc _schemeDesc;
        private readonly bool _localVol;
        private readonly double _illegalLocalVolOverwrite;
        private readonly Handle<FdmQuantoHelper> _quantoHelper;

        private Fdm1DimSolver _solver;

        public FdmBlackScholesSolver(
            Handle<GeneralizedBlackScholesProcess> process,
            double strike,
            FdmSolverDesc solverDesc,
            FdmSchemeDesc schemeDesc = null,
            bool localVol = false,
            double? illegalLocalVolOverwrite = null,
            Handle<FdmQuantoHelper> quantoHelper = null)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _strike = strike;
            _solverDesc = solverDesc ?? throw new ArgumentNullException(nameof(solverDesc));
            _schemeDesc = schemeDesc ?? FdmSchemeDesc.Douglas();
            _localVol = localVol;
            _illegalLocalVolOverwrite = illegalLocalVolOverwrite ?? -double.NaN; // Using NaN for Null<Real>
            _quantoHelper = quantoHelper ?? new Handle<FdmQuantoHelper>();

            RegisterWith(_process);
            RegisterWith(_quantoHelper);
        }

        public double ValueAt(double s)
        {
            Calculate();
            return _solver.InterpolateAt(System.Math.Log(s));
        }

        public double DeltaAt(double s)
        {
            Calculate();
            return _solver.DerivativeX(System.Math.Log(s)) / s;
        }

        public double GammaAt(double s)
        {
            Calculate();
            return (_solver.DerivativeXX(System.Math.Log(s)) - _solver.DerivativeX(System.Math.Log(s))) / (s * s);
        }

        public double ThetaAt(double s)
        {
            // In the C++ code, this method is not const, implying it might trigger a calculation.
            // We ensure calculation is performed before accessing the solver.
            Calculate();
            return _solver.ThetaAt(System.Math.Log(s));
        }

        protected override void PerformCalculations()
        {
            var op = new FdmBlackScholesOp(
                _solverDesc.Mesher,
                _process.CurrentLink(),
                _strike,
                _localVol,
                _illegalLocalVolOverwrite,
                0, // direction, assuming 0 for now as it's not exposed in this constructor
                _quantoHelper.IsEmpty ? null : _quantoHelper.CurrentLink()
            );

            _solver = new Fdm1DimSolver(_solverDesc, _schemeDesc, op);
        }
    }
}