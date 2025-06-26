using System;
using Anderson.Root;
using Anderson.Root.Solvers;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    /// <summary>
    /// QD+ American option pricing engine with robust root finding for exercise boundary calculation.
    /// This version uses a corrected bracketing strategy to ensure convergence to the correct
    /// exercise boundary, even for Deep ITM options.
    /// </summary>
    public class QdPlusAmericanEngine
    {
        public enum SolverType { Brent, Newton, Ridder, Halley, SuperHalley }

        private readonly ISolver1D _solver;
        private readonly SolverType _solverType;
        private readonly int _interpolationPoints;
        private readonly double _epsilon;

        public QdPlusAmericanEngine(
            int interpolationPoints = 8,
            SolverType solverType = SolverType.Halley,
            double epsilon = 1e-8)
        {
            _interpolationPoints = interpolationPoints;
            _solverType = solverType;
            _epsilon = epsilon;
            
            _solver = solverType switch
            {
                SolverType.Brent => new Brent(),
                SolverType.Newton => new Newton(),
                SolverType.Ridder => new Ridder(),
                SolverType.Halley => new Halley(),
                SolverType.SuperHalley => new Halley(),
                _ => throw new ArgumentOutOfRangeException(nameof(solverType))
            };
            _solver.MaxEvaluations = 100;
        }

        /// <summary>
        /// Calculates the short-maturity (T->0) exercise boundary for a put option.
        /// This provides an excellent initial guess for the root finder.
        /// Reference: Andersen & Lake (2021), Table 2 for American Puts.
        /// </summary>
        public static double XMax(double K, double r, double q)
        {
            if (r > 0.0)
            {
                // For a put, early exercise is considered if q > r.
                return K * Math.Max(1.0, q / r);
            }
            if (r == 0.0 && q > 0.0) return double.PositiveInfinity; // Theoretical limit
            if (r < 0.0 && q > 0.0) return double.PositiveInfinity; // Theoretical limit

            // Otherwise, early exercise is not optimal, and it behaves like a European option.
            return 0.0;
        }

        public ChebyshevInterpolation GetPutExerciseBoundary(double S, double K, double r, double q, double vol, double T)
        {
            double xmax = XMax(K, r, q);
            if (xmax <= 0 || double.IsInfinity(xmax))
            {
                return new ChebyshevInterpolation(_interpolationPoints, z => 0.0);
            }
            
            Func<double, double> functionToInterpolate = z =>
            {
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
            
            // Use the asymptotic value as the smart initial guess.
            double initialGuess = XMax(K, r, q);
            // Clamp the guess to be within the guaranteed bracket.
            initialGuess = Math.Max(solverFunction.XMin(), Math.Min(initialGuess, solverFunction.XMax()));

            // Always call the solver with the explicit, robust bracket [xMin, xMax].
            return _solver.Solve(solverFunction, _epsilon, initialGuess, solverFunction.XMin(), solverFunction.XMax());
        }

        /// <summary>
        /// Private adapter class to present the function g(B) = QD+(B) - B to the generic solvers.
        /// </summary>
        private class SolverFunction : IObjectiveFunctionWithSecondDerivative
        {
            private readonly QdPlusBoundaryEvaluator _eval;
            public SolverFunction(QdPlusBoundaryEvaluator evaluator) { _eval = evaluator; }
            public double Value(double x) => _eval.Value(x) - x;
            public double Derivative(double x) => _eval.Derivative(x) - 1.0;
            public double SecondDerivative(double x) => _eval.SecondDerivative(x);
            public double XMin() => _eval.XMin();
            public double XMax() => _eval.XMax();
        }
    }
}