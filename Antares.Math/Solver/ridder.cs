// Ridder.cs

using System;

namespace Antares.Math.Solver
{
    /// <summary>
    /// Ridder's 1-D solver.
    /// </summary>
    /// <remarks>
    /// The implementation of the algorithm was inspired by
    /// Press, Teukolsky, Vetterling, and Flannery,
    /// "Numerical Recipes in C", 2nd edition, Cambridge University Press.
    /// </remarks>
    public class Ridder : Solver1D<Ridder>
    {
        public override double SolveImpl(Func<double, double> f, double xAcc)
        {
            // Test on Black-Scholes implied volatility show that
            // Ridder solver algorithm actually provides an
            // accuracy 100 times below promised.
            double xAccuracy = xAcc / 100.0;

            // Any highly unlikely value, to simplify logic below
            Root = double.MinValue;

            while (EvaluationNumber <= MaxEvaluations)
            {
                double xMid = 0.5 * (XMin + XMax);
                // First of two function evaluations per iteration
                double fxMid = f(xMid);
                EvaluationNumber++;

                double s = System.Math.Sqrt(fxMid * fxMid - FxMin * FxMax);
                if (Comparison.Close(s, 0.0))
                {
                    f(Root); // Final evaluation
                    EvaluationNumber++;
                    return Root;
                }

                // Updating formula
                double nextRoot = xMid + (xMid - XMin) *
                    ((FxMin >= FxMax ? 1.0 : -1.0) * fxMid / s);

                if (System.Math.Abs(nextRoot - Root) <= xAccuracy)
                {
                    f(Root); // Final evaluation
                    EvaluationNumber++;
                    return Root;
                }

                Root = nextRoot;
                // Second of two function evaluations per iteration
                double froot = f(Root);
                EvaluationNumber++;

                if (Comparison.Close(froot, 0.0))
                    return Root;

                // Bookkeeping to keep the root bracketed on next iteration
                if (System.Math.Sign(fxMid) != System.Math.Sign(froot))
                {
                    XMin = xMid;
                    FxMin = fxMid;
                    XMax = Root;
                    FxMax = froot;
                }
                else if (System.Math.Sign(FxMin) != System.Math.Sign(froot))
                {
                    XMax = Root;
                    FxMax = froot;
                }
                else if (System.Math.Sign(FxMax) != System.Math.Sign(froot))
                {
                    XMin = Root;
                    FxMin = froot;
                }
                else
                {
                    QL.Fail("never get here.");
                }

                if (System.Math.Abs(XMax - XMin) <= xAccuracy)
                {
                    f(Root); // Final evaluation
                    EvaluationNumber++;
                    return Root;
                }
            }

            QL.Fail($"maximum number of function evaluations ({MaxEvaluations}) exceeded");
            return 0; // Unreachable
        }
    }
}