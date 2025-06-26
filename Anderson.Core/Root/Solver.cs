using System;

namespace Anderson.Root
{
    /// <summary>
    /// Interface for a 1-dimensional root-finding algorithm.
    /// </summary>
    public interface ISolver1D
    {
        /// <summary>
        /// Gets or sets the maximum number of evaluations for the solver.
        /// </summary>
        int MaxEvaluations { get; set; }

        /// <summary>
        /// Gets or sets the lower bound of the function domain.
        /// If set, the solver will not evaluate points below this value.
        /// </summary>
        double? LowerBound { get; set; }

        /// <summary>
        /// Gets or sets the upper bound of the function domain.
        /// If set, the solver will not evaluate points above this value.
        /// </summary>
        double? UpperBound { get; set; }

        /// <summary>
        /// Finds a root of the function within a specified bracket.
        /// </summary>
        /// <param name="f">The objective function, encapsulated in an interface.</param>
        /// <param name="accuracy">The desired accuracy of the root.</param>
        /// <param name="guess">An initial guess for the root.</param>
        /// <param name="xMin">The lower bound of the bracket.</param>
        /// <param name="xMax">The upper bound of the bracket.</param>
        /// <returns>The root of the function.</returns>
        /// <exception cref="ArgumentException">Thrown if the root is not bracketed.</exception>
        double Solve(IObjectiveFunction f, double accuracy, double guess, double xMin, double xMax);

        /// <summary>
        /// Finds a root of the function by first searching for a bracket.
        /// </summary>
        /// <param name="f">The objective function, encapsulated in an interface.</param>
        /// <param name="accuracy">The desired accuracy of the root.</param>
        /// <param name="guess">An initial guess for the root.</param>
        /// <param name="step">An initial step used to search for the bracket.</param>
        /// <returns>The root of the function.</returns>
        /// <exception cref="Exception">Thrown if a bracket cannot be found within MaxEvaluations.</exception>
        double Solve(IObjectiveFunction f, double accuracy, double guess, double step);
    }

    /// <summary>
    /// Abstract base class for 1D solvers, providing common bracketing and validation logic.
    /// This mirrors the design of QuantLib's Solver1D base class.
    /// </summary>
    public abstract class Solver1D : ISolver1D
    {
        public int MaxEvaluations { get; set; } = 100;
        public double? LowerBound { get; set; }
        public double? UpperBound { get; set; }

        protected int Evaluations;
        protected double Root, XMin, XMax, FxMin, FxMax;

        /// <summary>
        /// The core solving logic to be implemented by concrete solver classes.
        /// This method assumes that a bracket (xMin, xMax) has been found.
        /// </summary>
        protected abstract double SolveImpl(IObjectiveFunction f, double xAccuracy);

        /// <summary>
        /// Bracketed solve method.
        /// </summary>
        public double Solve(IObjectiveFunction f, double accuracy, double guess, double xMin, double xMax)
        {
            if (accuracy <= 0.0)
                throw new ArgumentException("Accuracy must be positive.");

            accuracy = Math.Max(accuracy, 1e-15); // Epsilon

            XMin = xMin;
            XMax = xMax;

            if (XMin >= XMax)
                throw new ArgumentException($"Invalid range: xMin ({XMin}) must be less than xMax ({XMax}).");
            if (LowerBound.HasValue && XMin < LowerBound.Value)
                throw new ArgumentException($"xMin ({XMin}) is less than the enforced lower bound ({LowerBound.Value}).");
            if (UpperBound.HasValue && XMax > UpperBound.Value)
                throw new ArgumentException($"xMax ({XMax}) is greater than the enforced upper bound ({UpperBound.Value}).");

            FxMin = f.Value(XMin);
            if (Math.Abs(FxMin) < 1e-15) return XMin;

            FxMax = f.Value(XMax);
            if (Math.Abs(FxMax) < 1e-15) return XMax;

            Evaluations = 2;

            if (FxMin * FxMax >= 0.0)
                throw new ArgumentException($"Root not bracketed: f({XMin}) -> {FxMin}, f({XMax}) -> {FxMax}.");

            if (guess <= XMin)
                Root = XMin + accuracy;
            else if (guess >= XMax)
                Root = XMax - accuracy;
            else
                Root = guess;

            return SolveImpl(f, accuracy);
        }

        /// <summary>
        /// Bracketing solve method.
        /// </summary>
        public double Solve(IObjectiveFunction f, double accuracy, double guess, double step)
        {
            if (accuracy <= 0.0)
                throw new ArgumentException("Accuracy must be positive.");

            accuracy = Math.Max(accuracy, 1e-15); // Epsilon

            const double growthFactor = 1.6;
            int flipflop = -1;

            Root = guess;
            FxMax = f.Value(Root);
            Evaluations = 1;

            if (Math.Abs(FxMax) < 1e-15) return Root;

            if (FxMax > 0.0)
            {
                XMin = EnforceBounds(Root - step);
                FxMin = f.Value(XMin);
                XMax = Root;
            }
            else
            {
                XMin = Root;
                FxMin = FxMax;
                XMax = EnforceBounds(Root + step);
                FxMax = f.Value(XMax);
            }
            Evaluations++;

            while (Evaluations <= MaxEvaluations)
            {
                if (FxMin * FxMax <= 0.0)
                {
                    if (Math.Abs(FxMin) < 1e-15) return XMin;
                    if (Math.Abs(FxMax) < 1e-15) return XMax;
                    return Solve(f, accuracy, guess, XMin, XMax);
                }

                if (Math.Abs(FxMin) < Math.Abs(FxMax))
                {
                    XMin = EnforceBounds(XMin + growthFactor * (XMin - XMax));
                    FxMin = f.Value(XMin);
                }
                else if (Math.Abs(FxMin) > Math.Abs(FxMax))
                {
                    XMax = EnforceBounds(XMax + growthFactor * (XMax - XMin));
                    FxMax = f.Value(XMax);
                }
                else if (flipflop == -1)
                {
                    XMin = EnforceBounds(XMin + growthFactor * (XMin - XMax));
                    FxMin = f.Value(XMin);
                    flipflop = 1;
                }
                else // flipflop == 1
                {
                    XMax = EnforceBounds(XMax + growthFactor * (XMax - XMin));
                    FxMax = f.Value(XMax);
                    flipflop = -1;
                }
                Evaluations++;
            }

            throw new Exception($"Unable to bracket root in {MaxEvaluations} function evaluations. Last bracket attempt: f[{XMin},{XMax}] -> [{FxMin},{FxMax}].");
        }

        private double EnforceBounds(double x)
        {
            if (LowerBound.HasValue && x < LowerBound.Value) return LowerBound.Value;
            if (UpperBound.HasValue && x > UpperBound.Value) return UpperBound.Value;
            return x;
        }
    }
}