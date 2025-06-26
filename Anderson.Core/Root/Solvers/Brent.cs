using System;

namespace Anderson.Root.Solvers
{
    /// <summary>
    /// Implements the Brent method for 1-dimensional root finding.
    /// This is a safe, bracketing solver that combines bisection, secant, and inverse quadratic interpolation.
    /// It does not require the function's derivative.
    /// </summary>
    public class Brent : Solver1D
    {
        /// <summary>
        /// The core implementation of the Brent solver.
        /// </summary>
        protected override double SolveImpl(IObjectiveFunction f, double xAccuracy)
        {
            /*
             * This implementation is a C# port of the algorithm from QuantLib,
             * which was inspired by "Numerical Recipes in C", 2nd ed., Cambridge University Press.
             */

            double min1, min2;
            double froot, p, q, r, s, xAcc1, xMid;

            // Start with the guess (Root) on one side and the bracket on the other.
            // The base class ensures FxMin and FxMax have opposite signs.
            froot = f.Value(Root);
            Evaluations++;

            // Orient the bracket so that f(xMin) and f(root) have opposite signs
            if (froot * FxMin < 0.0)
            {
                XMax = XMin;
                FxMax = FxMin;
            }
            else
            {
                XMin = XMax;
                FxMin = FxMax;
            }

            double d = Root - XMax;
            double e = d;

            while (Evaluations <= MaxEvaluations)
            {
                // Ensure the root remains between xMax and Root
                if ((froot > 0.0 && FxMax > 0.0) || (froot < 0.0 && FxMax < 0.0))
                {
                    XMax = XMin;
                    FxMax = FxMin;
                    d = Root - XMin;
                    e = d;
                }

                // Ensure f(Root) is the best current approximation
                if (Math.Abs(FxMax) < Math.Abs(froot))
                {
                    XMin = Root;    Root = XMax;    XMax = XMin;
                    FxMin = froot;  froot = FxMax;  FxMax = FxMin;
                }

                // Convergence check
                xAcc1 = 2.0 * 1e-15 * Math.Abs(Root) + 0.5 * xAccuracy;
                xMid = (XMax - Root) / 2.0;

                if (Math.Abs(xMid) <= xAcc1 || Math.Abs(froot) < 1e-15)
                {
                    return Root;
                }

                // Attempt interpolation
                if (Math.Abs(e) >= xAcc1 && Math.Abs(FxMin) > Math.Abs(froot))
                {
                    s = froot / FxMin;
                    if (Math.Abs(XMin - XMax) < 1e-15) // Are XMin and XMax the same?
                    {
                        p = 2.0 * xMid * s;
                        q = 1.0 - s;
                    }
                    else
                    {
                        // Inverse quadratic interpolation
                        q = FxMin / FxMax;
                        r = froot / FxMax;
                        p = s * (2.0 * xMid * q * (q - r) - (Root - XMin) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }

                    if (p > 0.0)
                    {
                        q = -q;
                    }
                    else
                    {
                        p = -p;
                    }

                    // Check if interpolation is acceptable
                    min1 = 3.0 * xMid * q - Math.Abs(xAcc1 * q);
                    min2 = Math.Abs(e * q);
                    if (2.0 * p < Math.Min(min1, min2))
                    {
                        e = d; // Accept interpolation
                        d = p / q;
                    }
                    else
                    {
                        d = xMid; // Interpolation failed, use bisection
                        e = d;
                    }
                }
                else
                {
                    // Bounds decreasing too slowly, use bisection
                    d = xMid;
                    e = d;
                }

                // Move to next guess
                XMin = Root;
                FxMin = froot;

                if (Math.Abs(d) > xAcc1)
                {
                    Root += d;
                }
                else
                {
                    Root += Math.Abs(xAcc1) * Math.Sign(xMid);
                }

                froot = f.Value(Root);
                Evaluations++;
            }

            throw new Exception($"Brent method failed to converge in {MaxEvaluations} evaluations.");
        }
    }
}