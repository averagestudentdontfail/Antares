using System;

namespace Antares.Root.Solvers
{
    /// <summary>
    /// Implements the Newton-Raphson method for 1-dimensional root finding.
    /// This solver requires the function's first derivative. It includes a safety
    /// mechanism to fall back to a bracketing solver if a step goes out of bounds.
    /// </summary>
    public class Newton : Solver1D
    {
        /// <summary>
        /// The core implementation of the Newton-Raphson solver.
        /// </summary>
        protected override double SolveImpl(IObjectiveFunction f, double xAccuracy)
        {
            // The function must provide a derivative.
            if (!(f is IObjectiveFunctionWithDerivative fd))
            {
                throw new ArgumentException("The objective function must implement IObjectiveFunctionWithDerivative for the Newton solver.");
            }

            double froot = f.Value(Root);
            double dfroot = fd.Derivative(Root);
            Evaluations++; // Derivative counts as an evaluation

            while (Evaluations <= MaxEvaluations)
            {
                if (Math.Abs(dfroot) < 1e-15)
                {
                    // Derivative is zero, fall back to a safer method
                    // as the next step would be infinite.
                    var fallbackSolver = new Brent { MaxEvaluations = MaxEvaluations - Evaluations };
                    return fallbackSolver.Solve(f, xAccuracy, Root, XMin, XMax);
                }

                double dx = froot / dfroot;
                Root -= dx;

                // Check if we have jumped out of the bracket
                if ((XMin - Root) * (Root - XMax) < 0.0)
                {
                    // We are out of bounds, fall back to the safer Brent solver
                    var fallbackSolver = new Brent { MaxEvaluations = MaxEvaluations - Evaluations };
                    return fallbackSolver.Solve(f, xAccuracy, Root + dx, XMin, XMax);
                }

                // Convergence check
                if (Math.Abs(dx) < xAccuracy)
                {
                    return Root;
                }

                froot = f.Value(Root);
                dfroot = fd.Derivative(Root);
                Evaluations++;
            }

            throw new Exception($"Newton's method failed to converge in {MaxEvaluations} evaluations.");
        }
    }
}