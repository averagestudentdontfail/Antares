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
            
            // The solver now gets the adapter, not the raw evaluator
            _solver = solverType switch
            {
                SolverType.Brent => new Brent(),
                SolverType.Newton => new Newton(),
                SolverType.Ridder => new Ridder(),
                SolverType.Halley => new Halley(),
                SolverType.SuperHalley => new Halley(), // Halley solver can be adapted for SuperHalley logic
                _ => throw new ArgumentOutOfRangeException(nameof(solverType))
            };
            _solver.MaxEvaluations = 100;
        }

        public static double XMax(double K, double r, double q)
        {
            if (r > 0.0) { if (q > 0.0) return K * Math.Min(1.0, r / q); return K; }
            if (r == 0.0 && q < 0.0) return K;
            if (r < 0.0 && q < r) return K;
            return 0.0;
        }

        public ChebyshevInterpolation GetPutExerciseBoundary(double S, double K, double r, double q, double vol, double T)
        {
            double xmax = XMax(K, r, q);
            if (xmax <= 0) return new ChebyshevInterpolation(_interpolationPoints, z => 0.0);
            
            Func<double, double> functionToInterpolate = z => {
                double tau = 0.25 * T * Math.Pow(1.0 + z, 2);
                double boundary = PutExerciseBoundaryAtTau(S, K, r, q, vol, T, tau);
                return Math.Pow(Math.Log(boundary / xmax), 2);
            };
            
            return new ChebyshevInterpolation(_interpolationPoints, functionToInterpolate);
        }

        private double PutExerciseBoundaryAtTau(double S, double K, double r, double q, double vol, double T, double tau)
        {
            if (tau < 1e-9) return XMax(K, r, q);
            
            var evaluator = new QdPlusBoundaryEvaluator(S, K, r, q, vol, tau);
            var solverFunction = new SolverFunction(evaluator);
            
            double initialGuess = XMax(K, r, q);
            initialGuess = Math.Max(solverFunction.XMin(), Math.Min(initialGuess, solverFunction.XMax()));

            return _solver.Solve(solverFunction, _epsilon, initialGuess, solverFunction.XMin(), solverFunction.XMax());
        }

        /// <summary>
        /// Private adapter class to present the function g(B) = QD+(B) - B to the generic solvers.
        /// This class correctly applies the subtractions for the root-finding problem.
        /// </summary>
        private class SolverFunction : IObjectiveFunctionWithSecondDerivative
        {
            private readonly QdPlusBoundaryEvaluator _eval;
            public SolverFunction(QdPlusBoundaryEvaluator evaluator) { _eval = evaluator; }
            
            public double Value(double x) => _eval.Value(x) - x;
            public double Derivative(double x) => _eval.Derivative(x) - 1.0;
            public double SecondDerivative(double x) => _eval.SecondDerivative(x); // d/dx(-x) = 0, d/dx(-1) = 0
            
            public double XMin() => _eval.XMin();
            public double XMax() => _eval.XMax();
        }
    }
}