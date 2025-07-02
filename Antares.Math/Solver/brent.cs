// Brent.cs

using System;

namespace Antares.Math.Solver
{
    /// <summary>
    /// Brent's 1-D solver.
    /// </summary>
    /// <remarks>
    /// The implementation of the algorithm was inspired by
    /// Press, Teukolsky, Vetterling, and Flannery,
    /// "Numerical Recipes in C", 2nd edition, Cambridge University Press.
    /// </remarks>
    public class Brent : Solver1D<Brent>
    {
        public override double SolveImpl(Func<double, double> f, double xAccuracy)
        {
            double min1, min2;
            double froot, p, q, r, s, xAcc1, xMid;

            // We want to start with Root (which equals the guess) on one side
            // of the bracket and both XMin and XMax on the other.
            froot = f(Root);
            EvaluationNumber++;
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

            while (EvaluationNumber <= MaxEvaluations)
            {
                if ((froot > 0.0 && FxMax > 0.0) || (froot < 0.0 && FxMax < 0.0))
                {
                    // Rename XMin, Root, XMax and adjust bounds
                    XMax = XMin;
                    FxMax = FxMin;
                    e = d = Root - XMin;
                }

                if (System.Math.Abs(FxMax) < System.Math.Abs(froot))
                {
                    XMin = Root;     Root = XMax;     XMax = XMin;
                    FxMin = froot;   froot = FxMax;   FxMax = FxMin;
                }

                // Convergence check
                xAcc1 = 2.0 * QLDefines.EPSILON * System.Math.Abs(Root) + 0.5 * xAccuracy;
                xMid = (XMax - Root) / 2.0;

                if (System.Math.Abs(xMid) <= xAcc1 || Comparison.Close(froot, 0.0))
                {
                    f(Root); // Final evaluation
                    EvaluationNumber++;
                    return Root;
                }

                if (System.Math.Abs(e) >= xAcc1 && System.Math.Abs(FxMin) > System.Math.Abs(froot))
                {
                    // Attempt inverse quadratic interpolation
                    s = froot / FxMin;
                    if (Comparison.Close(XMin, XMax))
                    {
                        p = 2.0 * xMid * s;
                        q = 1.0 - s;
                    }
                    else
                    {
                        q = FxMin / FxMax;
                        r = froot / FxMax;
                        p = s * (2.0 * xMid * q * (q - r) - (Root - XMin) * (r - 1.0));
                        q = (q - 1.0) * (r - 1.0) * (s - 1.0);
                    }

                    if (p > 0.0) q = -q; // Check whether in bounds
                    p = System.Math.Abs(p);
                    min1 = 3.0 * xMid * q - System.Math.Abs(xAcc1 * q);
                    min2 = System.Math.Abs(e * q);

                    if (2.0 * p < System.Math.Min(min1, min2))
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

                XMin = Root;
                FxMin = froot;

                if (System.Math.Abs(d) > xAcc1)
                    Root += d;
                else
                    Root += System.Math.CopySign(xAcc1, xMid);

                froot = f(Root);
                EvaluationNumber++;
            }

            QL.Fail($"maximum number of function evaluations ({MaxEvaluations}) exceeded");
            return 0; // Unreachable
        }
    }
}