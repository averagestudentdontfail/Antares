using System;
using Anderson.Root;
using Anderson.Root.Solvers;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    /// <summary>
    /// Provides the initial "first guess" for the American option exercise boundary
    /// using the QD+ analytical approximation method. The primary output is a
    /// ChebyshevInterpolation object representing the continuous boundary function.
    /// This version includes direct implementations for Halley and SuperHalley solvers.
    /// </summary>
    public class QdPlusAmericanEngine
    {
        /// <summary>
        /// Defines the type of 1D root-finding solver to use for the QD+ boundary calculation.
        /// </summary>
        public enum SolverType { Brent, Newton, Ridder, Halley, SuperHalley }

        private readonly ISolver1D _solver;
        private readonly SolverType _solverType;
        private readonly int _interpolationPoints;
        private readonly double _epsilon;

        /// <summary>
        /// Initializes a new instance of the QdPlusAmericanEngine.
        /// </summary>
        /// <param name="interpolationPoints">The number of nodes for Chebyshev interpolation.</param>
        /// <param name="solverType">The type of root-finder to use.</param>
        /// <param name="epsilon">The accuracy for the root-finding solver.</param>
        public QdPlusAmericanEngine(
            int interpolationPoints = 8,
            SolverType solverType = SolverType.Halley,
            double epsilon = 1e-8)
        {
            _interpolationPoints = interpolationPoints;
            _solverType = solverType;
            _epsilon = epsilon;

            // Instantiate the correct solver. Halley class is used for both Halley and SuperHalley
            // as the core logic is handled within this engine's PutExerciseBoundaryAtTau method.
            _solver = solverType switch
            {
                SolverType.Brent => new Brent(),
                SolverType.Newton => new Newton(),
                SolverType.Ridder => new Ridder(),
                SolverType.Halley => new Halley(),
                SolverType.SuperHalley => new Halley(),
                _ => throw new ArgumentOutOfRangeException(nameof(solverType), "Unknown solver type specified.")
            };
            _solver.MaxEvaluations = 100;
        }

        /// <summary>
        /// Calculates the limiting exercise boundary value for a put option as time to maturity approaches zero.
        /// </summary>
        public static double XMax(double K, double r, double q)
        {
            if (r > 0.0)
            {
                // For q > 0, the boundary is K*min(1, r/q). For q <= 0, it's K.
                // This simplifies to K if r/q >= 1, and K*r/q otherwise.
                // Since q can be zero or negative, we must handle that.
                if (q > 0.0)
                {
                    return K * Math.Min(1.0, r / q);
                }
                return K;
            }
            if (r == 0.0 && q < 0.0) return K;
            if (r < 0.0 && q < r) return K; // Double-boundary case
            return 0.0; // European case
        }

        /// <summary>
        /// Computes the put exercise boundary as a continuous function over the option's life.
        /// </summary>
        public ChebyshevInterpolation GetPutExerciseBoundary(double S, double K, double r, double q, double vol, double T)
        {
            double xmax = XMax(K, r, q);
            if (xmax <= 0)
            {
                // This is a European option, so the boundary is effectively zero.
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

        /// <summary>
        /// Calculates the put exercise boundary at a single point in time (tau).
        /// </summary>
        private double PutExerciseBoundaryAtTau(double S, double K, double r, double q, double vol, double T, double tau)
        {
            if (tau < 1e-9)
            {
                return XMax(K, r, q);
            }

            var evaluator = new QdPlusBoundaryEvaluator(S, K, r, q, vol, tau, T);

            // Special, direct handling for Halley-family solvers for performance.
            if (_solverType == SolverType.Halley || _solverType == SolverType.SuperHalley)
            {
                double x = evaluator.XMax(); // Start guess from the upper bound
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
                    
                    if (Math.Abs(fPrime) < 1e-12) break;

                    double lf = (fx * fPrime2) / (fPrime * fPrime);

                    // Switch between Halley and SuperHalley step formula
                    double step = _solverType == SolverType.Halley
                        ? (fx / fPrime) / (1.0 - 0.5 * lf)
                        : (fx / fPrime) * (1.0 + 0.5 * lf / (1.0 - lf));
                    
                    x = Math.Max(evaluator.XMin(), x - step);
                    evaluations++;
                }
                while (Math.Abs(x - xOld) > _epsilon && evaluations < maxIter);

                // If not converged, fall back to a safer solver for robustness
                if (Math.Abs(evaluator.Value(x)) > _epsilon * 10)
                {
                    return new Brent().Solve(evaluator, _epsilon, x, evaluator.XMin(), evaluator.XMax());
                }
                return x;
            }

            // For other specified solvers, use the standard ISolver1D interface
            return _solver.Solve(evaluator, _epsilon, K, evaluator.XMin(), evaluator.XMax());
        }
    }
}