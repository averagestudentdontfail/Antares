using System;

namespace Anderson.Root.Solvers
{
    /// <summary>
    /// Implements Ridder's method for 1-dimensional root finding.
    /// This is a robust method that combines the reliability of the bisection method 
    /// with the speed of the secant method. It does not require derivatives.
    /// </summary>
    public class Ridder : Solver1D
    {
        /// <summary>
        /// The core implementation of Ridder's solver.
        /// </summary>
        protected override double SolveImpl(IObjectiveFunction f, double xAccuracy)
        {
            // Use the bracket set up by the base class
            double xMin = XMin;
            double xMax = XMax;
            double fxMin = FxMin;
            double fxMax = FxMax;

            // Check if root is at the boundaries (already checked in base class, but for completeness)
            if (Math.Abs(fxMin) < xAccuracy) return xMin;
            if (Math.Abs(fxMax) < xAccuracy) return xMax;

            // Ridder's method iteration
            while (Evaluations <= MaxEvaluations)
            {
                double xMid = 0.5 * (xMin + xMax);
                double fxMid = f.Value(xMid);
                Evaluations++;

                if (Math.Abs(fxMid) < xAccuracy) return xMid;

                // Calculate the new estimate using Ridder's formula
                double s = Math.Sqrt(fxMid * fxMid - fxMin * fxMax);
                if (s == 0.0) return xMid; // Avoid division by zero

                double dx = (xMid - xMin) * fxMid / s;
                if ((fxMin - fxMax) < 0) dx = -dx;

                double xNew = xMid + dx;
                
                // Ensure xNew is within the current bracket
                if (xNew < Math.Min(xMin, xMax) || xNew > Math.Max(xMin, xMax))
                {
                    // Ridder's step went outside bracket, fall back to bisection for this step
                    xNew = xMid;
                }

                double fxNew = f.Value(xNew);
                Evaluations++;

                if (Math.Abs(fxNew) < xAccuracy) return xNew;

                // Update the bracketing interval
                if (fxMid * fxNew < 0)
                {
                    // Root is between xMid and xNew
                    xMin = xMid;
                    fxMin = fxMid;
                    xMax = xNew;
                    fxMax = fxNew;
                }
                else if (fxMin * fxNew < 0)
                {
                    // Root is between xMin and xNew
                    xMax = xNew;
                    fxMax = fxNew;
                }
                else
                {
                    // Root is between xNew and xMax
                    xMin = xNew;
                    fxMin = fxNew;
                }

                // Check for convergence on bracket size
                if (Math.Abs(xMax - xMin) < xAccuracy)
                {
                    return 0.5 * (xMin + xMax);
                }
            }

            throw new Exception($"Ridder's method failed to converge in {MaxEvaluations} evaluations.");
        }
    }
}