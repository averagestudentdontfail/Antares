using System;
using Anderson.Root;
using Anderson.Root.Solvers;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    /// <summary>
    /// QD+ American option pricing engine with robust root finding for exercise boundary calculation.
    /// Uses asymptotic approximation as initial guess and guarantees convergence with proper bracketing.
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
        /// Calculate the asymptotic exercise boundary for infinite time to maturity.
        /// This provides an excellent initial guess for the root finder.
        /// </summary>
        public static double XMax(double K, double r, double q)
        {
            if (r > 0.0)
            {
                if (q > 0.0) return K * Math.Min(1.0, r / q);
                return K;
            }
            if (r == 0.0 && q < 0.0) return K;
            if (r < 0.0 && q < r) return K;
            return 0.0;
        }

        public ChebyshevInterpolation GetPutExerciseBoundary(double S, double K, double r, double q, double vol, double T)
        {
            double xmax = XMax(K, r, q);
            if (xmax <= 0)
            {
                return new ChebyshevInterpolation(_interpolationPoints, z => 0.0);
            }
            
            Func<double, double> functionToInterpolate = z =>
            {
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
            
            double initialGuess = XMax(K, r, q);

            initialGuess = Math.Max(evaluator.XMin(), Math.Min(initialGuess, evaluator.XMax()));

            if (_solverType == SolverType.Halley || _solverType == SolverType.SuperHalley)
            {
                double x = initialGuess; // Use the smart guess
                double xOld;
                int evaluations = 0;
                const int maxIter = 100;

                do
                {
                    xOld = x;
                    double fx = evaluator.Value(x);
                    if (Math.Abs(fx) < _epsilon) return x;
                    
                    double fPrime = evaluator.Derivative(x);
                    double fPrime2 = evaluator.SecondDerivative(x);
                    
                    if (Math.Abs(fPrime) < 1e-12) break; // Avoid division by zero
                    
                    double lf = (fx * fPrime2) / (fPrime * fPrime);
                    double step = _solverType == SolverType.Halley
                        ? (fx / fPrime) / (1.0 - 0.5 * lf)
                        : (fx / fPrime) * (1.0 + 0.5 * lf / (1.0 - lf));
                    
                    // Clamp the step to stay within the robust bounds
                    x = Math.Max(evaluator.XMin(), x - step);
                    x = Math.Min(evaluator.XMax(), x);
                    evaluations++;
                }
                while (Math.Abs(x - xOld) > _epsilon && evaluations < maxIter);

                // If Halley didn't converge properly, fall back to Brent
                if (Math.Abs(evaluator.Value(x)) > _epsilon * 10)
                {
                    return new Brent().Solve(evaluator, _epsilon, x, evaluator.XMin(), evaluator.XMax());
                }
                return x;
            }

            // Use the smart guess for all other solvers too
            return _solver.Solve(evaluator, _epsilon, initialGuess, evaluator.XMin(), evaluator.XMax());
        }
    }
}