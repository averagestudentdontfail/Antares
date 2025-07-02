// Newton.cs

using System;

namespace Antares.Math.Solver
{
    /// <summary>
    /// Newton 1-D solver.
    /// </summary>
    /// <remarks>
    /// This solver requires that the function object passed to its Solve methods
    /// is an instance method of a class that implements <see cref="IFunctionWithDerivative"/>.
    /// <para>
    /// The implementation of the algorithm was inspired by
    /// Press, Teukolsky, Vetterling, and Flannery,
    /// "Numerical Recipes in C", 2nd edition, Cambridge University Press.
    /// </para>
    /// </remarks>
    public class Newton : Solver1D<Newton>
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
                    "The function passed to the Newton solver must be an instance method of a class " +
                    "that implements IFunctionWithDerivative. Consider using a different overload or solver.",
                    nameof(f));
            }
            
            double froot = f(Root);
            double dfroot = fd.Derivative(Root);
            if (dfroot == 0.0)
                throw new InvalidOperationException("Newton solver requires a non-zero derivative.");

            EvaluationNumber++;

            while (EvaluationNumber <= MaxEvaluations)
            {
                double dx = froot / dfroot;
                Root -= dx;

                // Jumped out of brackets, switch to NewtonSafe
                if ((XMin - Root) * (Root - XMax) < 0.0)
                {
                    var s = new NewtonSafe();
                    s.SetMaxEvaluations(MaxEvaluations - EvaluationNumber);
                    // The 'f' delegate is passed along, which still has the IFunctionWithDerivative target.
                    return s.Solve(f, xAccuracy, Root + dx, XMin, XMax);
                }

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
                    throw new InvalidOperationException("Newton solver encountered a zero derivative during iteration.");
            }

            QL.Fail($"maximum number of function evaluations ({MaxEvaluations}) exceeded");
            return 0; // Unreachable
        }
    }
}