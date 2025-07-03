// C# code for Solver1D.cs

using System;

namespace Antares.Math
{
    /// <summary>
    /// Base class for 1-D solvers.
    /// </summary>
    /// <remarks>
    /// The implementation of this class uses the Curiously Recurring Template Pattern (CRTP).
    /// Concrete solvers will be declared as:
    ///
    ///     public class MySolver : Solver1D&lt;MySolver&gt;
    ///     {
    ///         public double SolveImpl(Func&lt;double, double&gt; f, double accuracy)
    ///         {
    ///             // ... implementation ...
    ///         }
    ///     }
    ///
    /// Before calling SolveImpl, the base class will set its protected data members
    /// so that XMin and XMax form a valid bracket, FxMin and FxMax contain
    /// the function values, and Root is a valid initial guess.
    /// </remarks>
    /// <typeparam name="TImpl">The concrete solver class inheriting from this base.</typeparam>
    public abstract class Solver1D<TImpl> where TImpl : Solver1D<TImpl>
    {
        protected double Root, XMin, XMax, FxMin, FxMax;
        protected int MaxEvaluations = 100;
        protected int EvaluationNumber;

        private double _lowerBound, _upperBound;
        private bool _lowerBoundEnforced, _upperBoundEnforced;
        
        /// <summary>
        /// Solves for the root of a function by first bracketing it and then calling the specific solver implementation.
        /// </summary>
        /// <param name="f">The function whose root is to be found.</param>
        /// <param name="accuracy">The required accuracy of the solution.</param>
        /// <param name="guess">The initial guess for the root.</param>
        /// <param name="xMin">The minimum value of the search interval.</param>
        /// <param name="xMax">The maximum value of the search interval.</param>
        /// <returns>The root of the function.</returns>
        public double Solve(Func<double, double> f, double accuracy, double guess, double xMin, double xMax)
        {
            QL.Require(accuracy > 0.0, $"accuracy ({accuracy}) must be positive");
            accuracy = System.Math.Max(accuracy, QLDefines.EPSILON);

            XMin = xMin;
            XMax = xMax;

            QL.Require(XMin < XMax, $"invalid range: xMin ({XMin}) >= xMax ({XMax})");
            QL.Require(!_lowerBoundEnforced || XMin >= _lowerBound, $"xMin ({XMin}) < enforced low bound ({_lowerBound})");
            QL.Require(!_upperBoundEnforced || XMax <= _upperBound, $"xMax ({XMax}) > enforced hi bound ({_upperBound})");

            FxMin = f(XMin);
            if (Comparison.Close(FxMin, 0.0)) return XMin;

            FxMax = f(XMax);
            if (Comparison.Close(FxMax, 0.0)) return XMax;

            EvaluationNumber = 2;

            QL.Require(FxMin * FxMax < 0.0, $"root not bracketed: f[{XMin},{XMax}] -> [{FxMin:E},{FxMax:E}]");

            QL.Require(guess > XMin, $"guess ({guess}) < xMin ({XMin})");
            QL.Require(guess < XMax, $"guess ({guess}) > xMax ({XMax})");

            Root = guess;

            return ((TImpl)this).SolveImpl(f, accuracy);
        }

        /// <summary>
        /// This method must be implemented by the concrete solver.
        /// </summary>
        /// <param name="f">The function to solve.</param>
        /// <param name="xAccuracy">The required accuracy.</param>
        /// <returns>The root of the function.</returns>
        public abstract double SolveImpl(Func<double, double> f, double xAccuracy);

        /// <summary>
        /// Sets the maximum number of function evaluations for the bracketing routine.
        /// </summary>
        public void SetMaxEvaluations(int evaluations)
        {
            MaxEvaluations = evaluations;
        }

        /// <summary>
        /// Sets the lower bound for the function domain.
        /// </summary>
        public void SetLowerBound(double lowerBound)
        {
            _lowerBound = lowerBound;
            _lowerBoundEnforced = true;
        }

        /// <summary>
        /// Sets the upper bound for the function domain.
        /// </summary>
        public void SetUpperBound(double upperBound)
        {
            _upperBound = upperBound;
            _upperBoundEnforced = true;
        }

        private double EnforceBounds(double x)
        {
            if (_lowerBoundEnforced && x < _lowerBound) return _lowerBound;
            if (_upperBoundEnforced && x > _upperBound) return _upperBound;
            return x;
        }
    }
}