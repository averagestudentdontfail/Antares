using System;
using Anderson.Root;
using Anderson.Root.Solvers;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    /// <summary>
    /// Implements the QD+ method to find an accurate initial guess for the American option exercise boundary.
    /// This engine is primarily used as a first step for the more accurate QdFpAmericanEngine.
    /// Reference: Li, M. (2009), "Analytical Approximations for the Critical Stock Prices of American Options".
    /// </summary>
    public class QdPlusAmericanEngine
    {
        public enum SolverType { Halley, Brent }

        private readonly ISolver1D _solver;
        private readonly int _interpolationPoints;
        private readonly double _epsilon;

        public QdPlusAmericanEngine(
            int interpolationPoints = 8,
            SolverType solverType = SolverType.Halley,
            double epsilon = 1e-9)
        {
            _interpolationPoints = interpolationPoints;
            _epsilon = epsilon;
            
            _solver = solverType switch
            {
                SolverType.Halley => new Halley { MaxEvaluations = 100 },
                SolverType.Brent => new Brent { MaxEvaluations = 100 },
                _ => throw new ArgumentOutOfRangeException(nameof(solverType))
            };
        }

        /// <summary>
        /// Calculates the short-maturity (t->0) exercise boundary for a put option.
        /// This provides an excellent initial guess for the root finder at longer maturities.
        /// Reference: Andersen & Lake (2021), Table 2, "American put option boundary asymptotes".
        /// </summary>
        public static double XMax(double K, double r, double q)
        {
            // For a put option, early exercise is considered if r > q.
            // If r <= q, the dividend yield advantage of holding the stock outweighs the interest rate gain
            // from exercising early, but early exercise can still be optimal due to volatility.

            if (r < 0 && q < r)
            {
                // This is the double-boundary case described by Andersen & Lake (2021).
                // The single-boundary engine is not applicable. Return 0 to signal this.
                return 0.0;
            }

            if (q > 0.0)
            {
                return K * Math.Max(1.0, r / q);
            }
            
            if (q == 0.0)
            {
                // With no dividends (q=0), it is never optimal to exercise an American put early if r>=0.
                // The boundary is effectively at S=0, so the continuation region covers all positive stock prices.
                // We return infinity to signal this.
                return (r >= 0.0) ? double.PositiveInfinity : K; // If r<0, q=0 => r/q is -inf, so max(1, r/q) is 1.
            }
            
            // Case q < 0 (negative dividends / cost of carry)
            // It is always better to hold the option, as holding the underlying has a cost.
            // Boundary stays at K.
            return K;
        }

        /// <summary>
        /// Computes the exercise boundary B(τ) as a Chebyshev interpolation of the transformed function H(z).
        /// H(z) = ln(B(τ(z)) / XMax)^2, where z is the transformed square-root time variable.
        /// </summary>
        public ChebyshevInterpolation GetPutExerciseBoundary(double S, double K, double r, double q, double vol, double T)
        {
            double xmax = XMax(K, r, q);
            
            // If boundary is not applicable or trivial, return a zero-function interpolator.
            if (xmax <= 0 || double.IsInfinity(xmax))
            {
                return new ChebyshevInterpolation(_interpolationPoints, z => 0.0);
            }
            
            // This is the function we interpolate: the transformed boundary value.
            Func<double, double> functionToInterpolate = z =>
            {
                // Map from canonical Chebyshev domain [-1, 1] to time-to-maturity τ
                double tau = 0.25 * T * Math.Pow(1.0 + z, 2);
                
                // Find the boundary B(τ) at this specific time
                double boundary = PutExerciseBoundaryAtTau(S, K, r, q, vol, T, tau);
                
                // Return the H-transformed value
                return Math.Pow(Math.Log(Math.Max(1e-12, boundary) / xmax), 2);
            };
            
            return new ChebyshevInterpolation(_interpolationPoints, functionToInterpolate);
        }

        private double PutExerciseBoundaryAtTau(double S, double K, double r, double q, double vol, double T, double tau)
        {
            if (tau < 1e-9) return XMax(K, r, q);
            
            var evaluator = new QdPlusBoundaryEvaluator(S, K, r, q, vol, tau);
            var solverFunction = new SolverFunction(evaluator);
            
            double initialGuess = XMax(K, r, q);
            // The solver needs a finite initial guess
            if (double.IsInfinity(initialGuess))
            {
                initialGuess = K; 
            }
            
            // Ensure guess is within valid bounds
            initialGuess = Math.Min(K - 1e-6, Math.Max(evaluator.XMin(), initialGuess));
            
            try
            {
                // Use the auto-bracketing method that finds its own bracket
                return _solver.Solve(solverFunction, _epsilon, initialGuess, K * 0.1);
            }
            catch (Exception ex)
            {
                // Re-throw with a more informative message
                throw new Exception($"QD+ solver failed to find exercise boundary for tau={tau:F4}. Initial guess was {initialGuess:F2}. Error: {ex.Message}");
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