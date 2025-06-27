using System;

namespace Antares.Root.Solvers
{
    /// <summary>
    /// Implements Halley's method for 1-dimensional root finding.
    /// This is a third-order solver that requires the function's first and second derivatives.
    /// It is very fast but should only be used for well-behaved functions.
    /// </summary>
    public class Halley : Solver1D
    {
        /// <summary>
        /// The core implementation of Halley's solver.
        /// </summary>
        protected override double SolveImpl(IObjectiveFunction f, double xAccuracy)
        {
            // The function must provide first and second derivatives.
            if (!(f is IObjectiveFunctionWithSecondDerivative fds))
            {
                throw new ArgumentException("The objective function must implement IObjectiveFunctionWithSecondDerivative for the Halley solver.");
            }

            while (Evaluations <= MaxEvaluations)
            {
                double froot = fds.Value(Root);
                double dfroot = fds.Derivative(Root);
                double d2froot = fds.SecondDerivative(Root);
                Evaluations += 2; // Count derivative evaluations

                // Check for convergence
                if (Math.Abs(froot) < xAccuracy)
                {
                    return Root;
                }

                // Denominator of the standard Halley update step
                double denominator = 2.0 * dfroot * dfroot - froot * d2froot;

                if (Math.Abs(denominator) < 1e-15)
                {
                    // Denominator is zero, fall back to a safer method
                    var fallbackSolver = new Brent { MaxEvaluations = MaxEvaluations - Evaluations };
                    return fallbackSolver.Solve(f, xAccuracy, Root, XMin, XMax);
                }

                // Halley's update step
                double dx = (2.0 * froot * dfroot) / denominator;
                Root -= dx;

                // Check if we have jumped out of the bracket
                if ((XMin - Root) * (Root - XMax) < 0.0)
                {
                    // We are out of bounds, fall back to the safer Brent solver
                    var fallbackSolver = new Brent { MaxEvaluations = MaxEvaluations - Evaluations };
                    // Use previous root as guess for the fallback
                    return fallbackSolver.Solve(f, xAccuracy, Root + dx, XMin, XMax);
                }

                // Check for convergence on the step size
                if (Math.Abs(dx) < xAccuracy)
                {
                    return Root;
                }
            }

            throw new Exception($"Halley's method failed to converge in {MaxEvaluations} evaluations.");
        }
    }
}