using System;
using Anderson.Root;
using Anderson.Root.Solvers;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    public class QdPlusAmericanEngine
    {
        public enum SolverType { Brent, Newton, Ridder, Halley, SuperHalley }

        private readonly ISolver1D _solver;
        private readonly int _interpolationPoints;
        private readonly double _epsilon;

        public QdPlusAmericanEngine(int interpolationPoints = 8, SolverType solverType = SolverType.Halley, double epsilon = 1e-8)
        {
            _interpolationPoints = interpolationPoints;
            _epsilon = epsilon;
            _solver = solverType switch {
                SolverType.Brent => new Brent(), SolverType.Newton => new Newton(),
                SolverType.Ridder => new Ridder(), SolverType.Halley => new Halley(),
                SolverType.SuperHalley => new Halley(), _ => throw new ArgumentOutOfRangeException(nameof(solverType))
            };
            _solver.MaxEvaluations = 100;
        }

        public static double XMax(double K, double r, double q)
        {
            if (r > 0.0) return K * Math.Max(1.0, q / r);
            if (r == 0.0 && q > 0.0) return double.PositiveInfinity;
            if (r < 0.0 && q > 0.0) return double.PositiveInfinity;
            return 0.0;
        }

        public ChebyshevInterpolation GetPutExerciseBoundary(double S, double K, double r, double q, double vol, double T)
        {
            double xmax = XMax(K, r, q);
            if (xmax <= 0 || double.IsInfinity(xmax))
            {
                return new ChebyshevInterpolation(_interpolationPoints, z => 0.0);
            }
            
            Func<double, double> functionToInterpolate = z => {
                double tau = 0.25 * T * Math.Pow(1.0 + z, 2);
                double boundary = PutExerciseBoundaryAtTau(S, K, r, q, vol, T, tau);
                return Math.Pow(Math.Log(Math.Max(1e-12, boundary) / xmax), 2);
            };
            
            return new ChebyshevInterpolation(_interpolationPoints, functionToInterpolate);
        }

        private double PutExerciseBoundaryAtTau(double S, double K, double r, double q, double vol, double T, double tau)
        {
            if (tau < 1e-9) return XMax(K, r, q);
            
            var evaluator = new QdPlusBoundaryEvaluator(S, K, r, q, vol, tau);
            var solverFunction = new SolverFunction(evaluator);
            
            // Provide the best possible starting point
            double initialGuess = XMax(K, r, q);
            if (double.IsInfinity(initialGuess))
            {
                initialGuess = K * 2.0; // A reasonable guess when the asymptotic value is infinite
            }

            // Set the absolute lower bound for the solver's search
            _solver.LowerBound = solverFunction.XMin();
            _solver.UpperBound = null; // Ensure there is no artificial upper bound

            // --- THE DEFINITIVE FIX ---
            // Call the auto-bracketing solver with a sensible step size.
            // This empowers the solver to find the correct bracket dynamically.
            double step = K * 0.1; // A reasonable initial step for the search
            return _solver.Solve(solverFunction, _epsilon, initialGuess, step);
        }

        private class SolverFunction : IObjectiveFunctionWithSecondDerivative
        {
            private readonly QdPlusBoundaryEvaluator _eval;
            public SolverFunction(QdPlusBoundaryEvaluator evaluator) { _eval = evaluator; }
            public double Value(double x) => _eval.Value(x) - x;
            public double Derivative(double x) => _eval.Derivative(x) - 1.0;
            public double SecondDerivative(double x) => _eval.SecondDerivative(x);
        }
    }
}