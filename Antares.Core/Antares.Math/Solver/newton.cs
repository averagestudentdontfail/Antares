// Newton.cs

using System;

namespace Antares.Math.Solver
{
    /// <summary>
    /// Defines a function that provides both its value and its first derivative.
    /// This interface is required by solvers that use derivative information, such as Newton's method.
    /// </summary>
    public interface IFunctionWithDerivative
    {
        /// <summary>
        /// Calculates the function's value at a given point.
        /// </summary>
        /// <param name="x">The point at which to evaluate the function.</param>
        /// <returns>The value of the function.</returns>
        double Value(double x);

        /// <summary>
        /// Calculates the function's first derivative at a given point.
        /// </summary>
        /// <param name="x">The point at which to evaluate the derivative.</param>
        /// <returns>The value of the first derivative.</returns>
        double Derivative(double x);
    }

    /// <summary>
    /// A convenient wrapper class that implements IFunctionWithDerivative using delegates.
    /// This allows you to use Newton solvers without implementing the interface yourself.
    /// </summary>
    public class FunctionWithDerivative : IFunctionWithDerivative
    {
        private readonly Func<double, double> _function;
        private readonly Func<double, double> _derivative;

        /// <summary>
        /// Initializes a new instance of the FunctionWithDerivative class.
        /// </summary>
        /// <param name="function">The function delegate.</param>
        /// <param name="derivative">The derivative delegate.</param>
        public FunctionWithDerivative(Func<double, double> function, Func<double, double> derivative)
        {
            _function = function ?? throw new ArgumentNullException(nameof(function));
            _derivative = derivative ?? throw new ArgumentNullException(nameof(derivative));
        }

        /// <summary>
        /// Calculates the function's value at a given point.
        /// </summary>
        public double Value(double x) => _function(x);

        /// <summary>
        /// Calculates the function's first derivative at a given point.
        /// </summary>
        public double Derivative(double x) => _derivative(x);
    }

    /// <summary>
    /// Newton 1-D solver.
    /// </summary>
    /// <remarks>
    /// This solver requires that the function provides both value and derivative information.
    /// You can either implement IFunctionWithDerivative directly or use the FunctionWithDerivative wrapper class.
    /// <para>
    /// The implementation of the algorithm was inspired by
    /// Press, Teukolsky, Vetterling, and Flannery,
    /// "Numerical Recipes in C", 2nd edition, Cambridge University Press.
    /// </para>
    /// </remarks>
    public class Newton : Solver1D<Newton>
    {
        /// <summary>
        /// Solves for the root using Newton's method with function and derivative delegates.
        /// </summary>
        /// <param name="function">The function to find the root of.</param>
        /// <param name="derivative">The derivative of the function.</param>
        /// <param name="accuracy">The required accuracy.</param>
        /// <param name="guess">The initial guess.</param>
        /// <param name="step">The initial step size for bracketing (if needed).</param>
        /// <returns>The root of the function.</returns>
        public double Solve(Func<double, double> function, Func<double, double> derivative, 
                          double accuracy, double guess, double step)
        {
            var wrapper = new FunctionWithDerivative(function, derivative);
            return Solve(wrapper.Value, accuracy, guess, step);
        }

        /// <summary>
        /// Solves for the root using Newton's method with function and derivative delegates.
        /// </summary>
        /// <param name="function">The function to find the root of.</param>
        /// <param name="derivative">The derivative of the function.</param>
        /// <param name="accuracy">The required accuracy.</param>
        /// <param name="guess">The initial guess.</param>
        /// <param name="xMin">The lower bound.</param>
        /// <param name="xMax">The upper bound.</param>
        /// <returns>The root of the function.</returns>
        public double Solve(Func<double, double> function, Func<double, double> derivative,
                          double accuracy, double guess, double xMin, double xMax)
        {
            var wrapper = new FunctionWithDerivative(function, derivative);
            return Solve(wrapper.Value, accuracy, guess, xMin, xMax);
        }

        /// <summary>
        /// Solves for the root using an object that implements IFunctionWithDerivative.
        /// </summary>
        /// <param name="functionWithDerivative">The function object that provides both value and derivative.</param>
        /// <param name="accuracy">The required accuracy.</param>
        /// <param name="guess">The initial guess.</param>
        /// <param name="step">The initial step size for bracketing (if needed).</param>
        /// <returns>The root of the function.</returns>
        public double Solve(IFunctionWithDerivative functionWithDerivative, double accuracy, double guess, double step)
        {
            return Solve(functionWithDerivative.Value, accuracy, guess, step);
        }

        /// <summary>
        /// Solves for the root using an object that implements IFunctionWithDerivative.
        /// </summary>
        /// <param name="functionWithDerivative">The function object that provides both value and derivative.</param>
        /// <param name="accuracy">The required accuracy.</param>
        /// <param name="guess">The initial guess.</param>
        /// <param name="xMin">The lower bound.</param>
        /// <param name="xMax">The upper bound.</param>
        /// <returns>The root of the function.</returns>
        public double Solve(IFunctionWithDerivative functionWithDerivative, double accuracy, double guess, double xMin, double xMax)
        {
            return Solve(functionWithDerivative.Value, accuracy, guess, xMin, xMax);
        }

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
                    "that implements IFunctionWithDerivative. Consider using the overloads that take " +
                    "separate function and derivative delegates, or use a different solver.",
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

            Antares.QL.Fail($"maximum number of function evaluations ({MaxEvaluations}) exceeded");
            return 0; // Unreachable
        }
    }
}