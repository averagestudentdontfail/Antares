// C# code for Solver1D.cs

using System;

namespace Antares.Math
{
    #region Supporting Infrastructure
    // Placeholders for dependencies. In a real project, these would be in their own files.
    
    public static class Comparison
    {
        public static bool Close(double x, double y) => System.Math.Abs(x - y) < 1e-12;
    }

    public static class QLDefines
    {
        public const double EPSILON = 2.2204460492503131e-016;
    }
    #endregion

    /// <summary>
    /// Base class for 1-D solvers.
    /// </summary>
    /// <remarks>
    /// The implementation of this class uses the Curiously Recurring Template Pattern (CRTP).
    /// Concrete solvers will be declared as:
    /// <code>
    /// public class MySolver : Solver1D<MySolver>
    /// {
    ///     public double SolveImpl(Func<double, double> f, double accuracy)
    ///     {
    ///         // ... implementation ...
    ///     }
    /// }
    /// </code>
    /// Before calling `SolveImpl`, the base class will set its protected data members
    /// so that `XMin` and `XMax` form a valid bracket, `FxMin` and `FxMax` contain
    /// the function values, and `Root` is a valid initial guess.
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
        public double Solve(Func<double, double> f, double accuracy, double guess, double step)
        {
            QL.Require(accuracy > 0.0, $"accuracy ({accuracy}) must be positive");
            accuracy = System.Math.Max(accuracy, QLDefines.EPSILON);

            const double growthFactor = 1.6;
            int flipflop = -1;

            Root = guess;
            FxMax = f(Root);

            if (Comparison.Close(FxMax, 0.0))
                return Root;
            
            if (FxMax > 0.0)
            {
                XMin = EnforceBounds(Root - step);
                FxMin = f(XMin);
                XMax = Root;
            }
            else
            {
                XMin = Root;
                FxMin = FxMax;
                XMax = EnforceBounds(Root + step);
                FxMax = f(XMax);
            }

            EvaluationNumber = 2;
            while (EvaluationNumber <= MaxEvaluations)
            {
                if (FxMin * FxMax <= 0.0)
                {
                    if (Comparison.Close(FxMin, 0.0)) return XMin;
                    if (Comparison.Close(FxMax, 0.0)) return XMax;
                    Root = (XMax + XMin) / 2.0;
                    return ((TImpl)this).SolveImpl(f, accuracy);
                }

                if (System.Math.Abs(FxMin) < System.Math.Abs(FxMax))
                {
                    XMin = EnforceBounds(XMin + growthFactor * (XMin - XMax));
                    FxMin = f(XMin);
                }
                else if (System.Math.Abs(FxMin) > System.Math.Abs(FxMax))
                {
                    XMax = EnforceBounds(XMax + growthFactor * (XMax - XMin));
                    FxMax = f(XMax);
                }
                else if (flipflop == -1)
                {
                    XMin = EnforceBounds(XMin + growthFactor * (XMin - XMax));
                    FxMin = f(XMin);
                    EvaluationNumber++;
                    flipflop = 1;
                }
                else if (flipflop == 1)
                {
                    XMax = EnforceBounds(XMax + growthFactor * (XMax - XMin));
                    FxMax = f(XMax);
                    flipflop = -1;
                }
                EvaluationNumber++;
            }

            QL.Fail($"unable to bracket root in {MaxEvaluations} function evaluations (last bracket attempt: " +
                    $"f[{XMin},{XMax}] -> [{FxMin},{FxMax}])");
            return 0; // Unreachable
        }

        /// <summary>
        /// Solves for the root of a function given a bracketed range.
        /// </summary>
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