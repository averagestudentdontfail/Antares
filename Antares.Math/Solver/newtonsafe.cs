// Newtonsafe.cs

using System;

namespace Antares.Math.Solver
{
    /// <summary>
    /// Safe (bracketed) Newton 1-D solver.
    /// </summary>
    /// <remarks>
    /// This solver combines the speed of Newton's method with the safety of bisection.
    /// It requires that the function object passed to its Solve methods is an instance method
    /// of a class that implements <see cref="IFunctionWithDerivative"/>.
    /// <para>
    /// The implementation of the algorithm was inspired by
    /// Press, Teukolsky, Vetterling, and Flannery,
    /// "Numerical Recipes in C", 2nd edition, Cambridge University Press.
    /// </para>
    /// </remarks>
    public class NewtonSafe : Solver1D<NewtonSafe>
    {
        /// <summary>
        /// This method is required by the base class but is not the intended entry point for this solver.
        /// It will attempt to solve the root-finding problem, but requires the provided function
        /// to be an instance method of a class implementing <see cref="IFunctionWithDerivative"/>.
        /// </summary>
        public override double SolveImpl(Func<double, double> f, double xAccuracy)
        {
            if (!(f.Target is IFunctionWithDerivative fd))
            {
                throw new ArgumentException(
                    "The function passed to the NewtonSafe solver must be an instance method of a class " +
                    "that implements IFunctionWithDerivative. Consider using a different overload or solver.",
                    nameof(f));
            }

            // Orient the search so that f(xl) < 0
            double xh, xl;
            if (FxMin < 0.0)
            {
                xl = XMin;
                xh = XMax;
            }
            else
            {
                xh = XMin;
                xl = XMax;
            }

            // The "stepsize before last" and the last step
            double dxold = XMax - XMin;
            double dx = dxold;

            double froot = f(Root);
            double dfroot = fd.Derivative(Root);
            if (dfroot == 0.0)
                throw new InvalidOperationException("NewtonSafe requires a non-zero derivative.");
            
            EvaluationNumber++;

            while (EvaluationNumber <= MaxEvaluations)
            {
                // Bisect if (out of range || not decreasing fast enough)
                if ((((Root - xh) * dfroot - froot) * ((Root - xl) * dfroot - froot) > 0.0)
                    || (System.Math.Abs(2.0 * froot) > System.Math.Abs(dxold * dfroot)))
                {
                    dxold = dx;
                    dx = (xh - xl) / 2.0;
                    Root = xl + dx;
                }
                else
                {
                    dxold = dx;
                    dx = froot / dfroot;
                    Root -= dx;
                }

                // Convergence criterion
                if (System.Math.Abs(dx) < xAccuracy)
                {
                    f(Root); // Final evaluation
                    EvaluationNumber++;
                    return Root;
                }

                froot = f(Root);
                dfroot = fd.Derivative(Root);
                EvaluationNumber++;

                if (dfroot == 0.0)
                    throw new InvalidOperationException("NewtonSafe encountered a zero derivative during iteration.");

                // Maintain the bracket on the root
                if (froot < 0.0)
                    xl = Root;
                else
                    xh = Root;
            }

            QL.Fail($"maximum number of function evaluations ({MaxEvaluations}) exceeded");
            return 0; // Unreachable
        }
    }
}