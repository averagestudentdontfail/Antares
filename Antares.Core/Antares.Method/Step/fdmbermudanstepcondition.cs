// C# code for FdmBermudanStepCondition.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math;
using Antares.Method.Mesh;
using Antares.Method.Utilities;
using Antares.Time;

namespace Antares.Method.Step
{
    /// <summary>
    /// Step condition for applying a Bermudan exercise constraint in an FDM scheme.
    /// </summary>
    /// <remarks>
    /// This condition checks if the current time step corresponds to one of the
    /// specified exercise dates. If it does, it applies the American exercise
    /// constraint: V(t,x) = max(V_continuation(t,x), V_intrinsic(t,x)).
    /// </remarks>
    public class FdmBermudanStepCondition : IStepCondition<Array>
    {
        private readonly List<double> _exerciseTimes;
        private readonly FdmMesher _mesher;
        private readonly IFdmInnerValueCalculator _calculator;

        /// <summary>
        /// Initializes a new instance of the FdmBermudanStepCondition class.
        /// </summary>
        /// <param name="exerciseDates">The list of exercise dates.</param>
        /// <param name="referenceDate">The reference date for calculating time fractions.</param>
        /// <param name="dayCounter">The day counter for calculating time fractions.</param>
        /// <param name="mesher">The FDM mesher, which provides the grid layout.</param>
        /// <param name="calculator">The calculator for the option's intrinsic value.</param>
        public FdmBermudanStepCondition(
            IReadOnlyList<Date> exerciseDates,
            Date referenceDate,
            DayCounter dayCounter,
            FdmMesher mesher,
            IFdmInnerValueCalculator calculator)
        {
            _mesher = mesher;
            _calculator = calculator;
            
            _exerciseTimes = exerciseDates
                .Select(d => dayCounter.yearFraction(referenceDate, d))
                .ToList();
        }

        /// <summary>
        /// Gets the list of exercise times (in years) corresponding to the exercise dates.
        /// </summary>
        public IReadOnlyList<double> ExerciseTimes => _exerciseTimes;

        /// <summary>
        /// Applies the Bermudan exercise constraint to the solution array if the
        /// current time `t` is one of the exercise times.
        /// </summary>
        /// <param name="a">The solution array at the current time step.</param>
        /// <param name="t">The current time.</param>
        public void ApplyTo(Array a, double t)
        {
            // The C++ code uses std::find. For a small number of exercise dates, this is fine.
            // For a larger number, a HashSet would be more performant for the lookup.
            // Here, we use a simple Contains check, which is clear and sufficient.
            // To handle floating point inaccuracies, a tolerance check is better.
            bool isExerciseTime = false;
            foreach (double exerciseTime in _exerciseTimes)
            {
                if (System.Math.Abs(exerciseTime - t) < 1e-12)
                {
                    isExerciseTime = true;
                    break;
                }
            }

            if (isExerciseTime)
            {
                QL.Require(_mesher.Layout.Size == a.Count, "Inconsistent array dimensions.");

                // The loop iterates over all points in the multi-dimensional grid.
                foreach (var iter in (Operator.FdmLinearOpLayout)_mesher.Layout)
                {
                    double innerValue = _calculator.InnerValue(iter, t);
                    int index = iter.Index;

                    if (innerValue > a[index])
                    {
                        a[index] = innerValue;
                    }
                }
            }
        }
    }
}