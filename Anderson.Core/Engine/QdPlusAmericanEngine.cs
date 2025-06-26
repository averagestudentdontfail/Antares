using System;
using Anderson.Root;
using Anderson.Root.Solvers;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    /// <summary>
    /// QD+ American option pricing engine with robust root finding for exercise boundary calculation.
    /// This version uses the auto-bracketing Solve method to handle cases where we don't have
    /// a pre-defined finite bracket.
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
            // From Anderson & Lake (2021), Table 2: American Put
            // The short-maturity exercise boundary B(0) = X_max
            
            if (r >= 0.0)
            {
                // Standard case: always return K * max(1, r/q)
                // This ensures we never return 0, maintaining American optionality
                if (q > 0.0)
                {
                    return K * Math.Max(1.0, r / q);
                }
                else if (q == 0.0)
                {
                    // No dividends: boundary approaches infinity as r/0
                    return r > 0.0 ? double.PositiveInfinity : K;
                }
                else // q < 0 (negative dividends/storage costs)
                {
                    // With negative dividends, early exercise is always suboptimal
                    // since holding the option gives positive carry
                    return K; // Minimum boundary
                }
            }
            else // r < 0 (negative interest rates)
            {
                if (q < r)
                {
                    // Double boundary case - not handled by this implementation
                    return 0.0;
                }
                else
                {
                    // Single boundary case with negative rates
                    return K * Math.Max(1.0, r / q);
                }
            }
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
            
            // Use the asymptotic value as the initial guess
            double initialGuess = XMax(K, r, q);
            
            // Handle the case where XMax might be infinite
            if (double.IsInfinity(initialGuess))
            {
                initialGuess = K * 2.0; // Use 2*K as a reasonable starting point
            }
            
            // Clamp the guess to be above the minimum bound
            initialGuess = Math.Max(evaluator.XMin(), initialGuess);
            
            // Use the auto-bracketing solve method with an initial step
            // The step size should be proportional to the scale of the problem
            double step = K * 0.1; // 10% of strike price as initial step
            
            // Set bounds on the solver to prevent it from exploring negative values
            _solver.LowerBound = evaluator.XMin();
            _solver.UpperBound = null; // No upper bound restriction
            
            try
            {
                // Use the auto-bracketing method that finds its own bracket
                return _solver.Solve(solverFunction, _epsilon, initialGuess, step);
            }
            catch (Exception ex)
            {
                // If auto-bracketing fails, try with a different initial guess
                // Use a more conservative guess closer to the strike
                initialGuess = K;
                step = K * 0.05; // Smaller step
                
                try
                {
                    return _solver.Solve(solverFunction, _epsilon, initialGuess, step);
                }
                catch
                {
                    // If still failing, fall back to a simple search near the strike
                    // This is a last resort
                    double[] testPoints = { K * 0.8, K * 0.9, K, K * 1.1, K * 1.2, K * 1.5 };
                    foreach (var testPoint in testPoints)
                    {
                        try
                        {
                            return _solver.Solve(solverFunction, _epsilon, testPoint, K * 0.1);
                        }
                        catch
                        {
                            continue;
                        }
                    }
                    
                    // If all else fails, throw the original exception
                    throw new Exception($"Failed to find exercise boundary: {ex.Message}");
                }
            }
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