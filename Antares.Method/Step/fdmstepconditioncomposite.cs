// C# code for FdmStepConditionComposite.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math;
using Antares.Method.Mesh;
using Antares.Method.Utilities;
using QLNet; // Using QLNet for Date, DayCounter, Exercise, and Dividend types

// Placeholders for dependent types. In a full project, these would be in their own files.
namespace Antares.Method.Utilities
{
    // A placeholder for the FdmDividendHandler
    public class FdmDividendHandler : IStepCondition<Array>
    {
        private readonly List<double> _dividendTimes;
        public FdmDividendHandler(IReadOnlyList<Dividend> cashFlow, FdmMesher mesher, Date refDate, DayCounter dayCounter, int i)
        {
            _dividendTimes = cashFlow.Select(d => dayCounter.yearFraction(refDate, d.date())).ToList();
        }
        public IReadOnlyList<double> DividendTimes() => _dividendTimes;
        public void ApplyTo(Array a, double t) { /* Dividend logic would go here */ }
    }
}


namespace Antares.Method.Step
{
    /// <summary>
    /// A composite of multiple FDM step conditions.
    /// </summary>
    /// <remarks>
    /// This class manages a collection of step conditions and applies them sequentially.
    /// It also aggregates all stopping times from the individual conditions into a
    /// single, sorted list of unique times.
    /// </remarks>
    public class FdmStepConditionComposite : IStepCondition<Array>
    {
        private readonly List<double> _stoppingTimes;
        private readonly IReadOnlyList<IStepCondition<Array>> _conditions;

        /// <summary>
        /// Initializes a new instance of the FdmStepConditionComposite class.
        /// </summary>
        /// <param name="stoppingTimes">A list of collections, where each collection contains stopping times from a single condition.</param>
        /// <param name="conditions">The collection of step conditions to be managed.</param>
        public FdmStepConditionComposite(
            IEnumerable<IEnumerable<double>> stoppingTimes,
            IReadOnlyList<IStepCondition<Array>> conditions)
        {
            _conditions = conditions ?? throw new ArgumentNullException(nameof(conditions));

            // Aggregate all stopping times into a single sorted list of unique values.
            var allStoppingTimes = new HashSet<double>();
            foreach (var timeList in stoppingTimes)
            {
                foreach (var time in timeList)
                {
                    allStoppingTimes.Add(time);
                }
            }
            _stoppingTimes = allStoppingTimes.ToList();
            _stoppingTimes.Sort();
        }

        /// <summary>
        /// Gets the collection of managed step conditions.
        /// </summary>
        public IReadOnlyList<IStepCondition<Array>> Conditions() => _conditions;

        /// <summary>
        /// Gets a sorted list of unique stopping times aggregated from all managed conditions.
        /// </summary>
        public IReadOnlyList<double> StoppingTimes() => _stoppingTimes;

        /// <summary>
        /// Applies all managed step conditions to the solution array.
        /// </summary>
        /// <param name="a">The solution array at the current time step.</param>
        /// <param name="t">The current time.</param>
        public void ApplyTo(Array a, double t)
        {
            foreach (var condition in _conditions)
            {
                condition.ApplyTo(a, t);
            }
        }

        /// <summary>
        /// A factory method to join a snapshot condition with an existing composite.
        /// </summary>
        public static FdmStepConditionComposite JoinConditions(
            FdmSnapshotCondition c1,
            FdmStepConditionComposite c2)
        {
            var stoppingTimes = new List<IEnumerable<double>>
            {
                c2.StoppingTimes(),
                new[] { c1.Time }
            };

            var conditions = new List<IStepCondition<Array>>(c2.Conditions()) { c1 };

            return new FdmStepConditionComposite(stoppingTimes, conditions);
        }

        /// <summary>
        /// A factory method to create a composite of standard conditions for a vanilla option.
        /// </summary>
        public static FdmStepConditionComposite VanillaComposite(
            IReadOnlyList<Dividend> cashFlow,
            Exercise exercise,
            FdmMesher mesher,
            IFdmInnerValueCalculator calculator,
            Date refDate,
            DayCounter dayCounter)
        {
            var stoppingTimes = new List<IEnumerable<double>>();
            var stepConditions = new List<IStepCondition<Array>>();

            if (cashFlow != null && cashFlow.Any())
            {
                var dividendCondition = new FdmDividendHandler(cashFlow, mesher, refDate, dayCounter, 0);
                stepConditions.Add(dividendCondition);

                double maturityTime = dayCounter.yearFraction(refDate, exercise.lastDate());
                
                // Add dividend times, capped at maturity
                var dividendTimes = dividendCondition.DividendTimes()
                    .Select(t => System.Math.Min(maturityTime, t)).ToList();
                stoppingTimes.Add(dividendTimes);

                // Add slightly perturbed times for smoother convergence
                var perturbedTimes = dividendTimes
                    .Select(t => System.Math.Min(maturityTime, t + 1e-5)).ToList();
                stoppingTimes.Add(perturbedTimes);
            }

            switch (exercise.type())
            {
                case Exercise.Type.American:
                    stepConditions.Add(new FdmAmericanStepCondition(mesher, calculator));
                    break;
                case Exercise.Type.Bermudan:
                    var bermudanCondition = new FdmBermudanStepCondition(exercise.dates(), refDate, dayCounter, mesher, calculator);
                    stepConditions.Add(bermudanCondition);
                    stoppingTimes.Add(bermudanCondition.ExerciseTimes);
                    break;
                case Exercise.Type.European:
                    // No exercise condition needed before maturity
                    break;
                default:
                    QL.Fail("Exercise type is not supported.");
                    break;
            }

            return new FdmStepConditionComposite(stoppingTimes, stepConditions);
        }
    }
}