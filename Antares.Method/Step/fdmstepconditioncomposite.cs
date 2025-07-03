// C# code for FdmStepConditionComposite.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math;
using Antares.Method.Mesh;
using Antares.Method.Utilities;
using Antares.Time;

namespace Antares.Method.Utilities
{
    /// <summary>
    /// Handles dividend adjustments in finite difference methods.
    /// </summary>
    public class FdmDividendHandler : IStepCondition<Array>
    {
        private readonly List<double> _dividendTimes;
        private readonly List<double> _dividendAmounts;
        private readonly FdmMesher _mesher;

        public FdmDividendHandler(IReadOnlyList<Dividend> cashFlow, 
                                 FdmMesher mesher, 
                                 Date refDate, 
                                 DayCounter dayCounter, 
                                 int assetDirection = 0)
        {
            _mesher = mesher ?? throw new ArgumentNullException(nameof(mesher));
            _dividendTimes = new List<double>();
            _dividendAmounts = new List<double>();
            
            foreach (var dividend in cashFlow)
            {
                double time = dayCounter.yearFraction(refDate, dividend.date());
                _dividendTimes.Add(time);
                _dividendAmounts.Add(dividend.amount());
            }
        }

        public IReadOnlyList<double> DividendTimes() => _dividendTimes;

        public void ApplyTo(Array a, double t)
        {
            // Find if there's a dividend at this time
            for (int i = 0; i < _dividendTimes.Count; i++)
            {
                if (Math.Abs(_dividendTimes[i] - t) < 1e-8) // Close enough to dividend time
                {
                    ApplyDividendAdjustment(a, _dividendAmounts[i]);
                    break;
                }
            }
        }

        private void ApplyDividendAdjustment(Array a, double dividendAmount)
        {
            // Apply dividend adjustment logic
            // This would typically involve adjusting the underlying asset price grid
            // and interpolating the option values accordingly
            foreach (var iter in (FdmLinearOpLayout)_mesher.Layout)
            {
                // Simplified dividend adjustment - in practice this would be more complex
                // involving proper interpolation and grid adjustments
                int index = (int)iter.Index;
                // The adjustment logic would depend on the specific dividend model
            }
        }
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
        /// <param name="cashFlow">The dividend schedule.</param>
        /// <param name="exercise">The exercise information.</param>
        /// <param name="mesher">The finite difference mesher.</param>
        /// <param name="calculator">The inner value calculator.</param>
        /// <param name="refDate">The reference date for time calculations.</param>
        /// <param name="dayCounter">The day counter for time calculations.</param>
        /// <returns>A composite step condition containing appropriate conditions for the option.</returns>
        public static FdmStepConditionComposite VanillaComposite(
            IReadOnlyList<Dividend> cashFlow,
            Exercise exercise,
            FdmMesher mesher,
            FdmInnerValueCalculator calculator,
            Date refDate,
            DayCounter dayCounter)
        {
            var stoppingTimesList = new List<IEnumerable<double>>();
            var conditions = new List<IStepCondition<Array>>();

            // Add dividend condition if there are dividends
            if (cashFlow != null && cashFlow.Count > 0)
            {
                var dividendHandler = new FdmDividendHandler(cashFlow, mesher, refDate, dayCounter);
                conditions.Add(dividendHandler);
                stoppingTimesList.Add(dividendHandler.DividendTimes());
            }

            // Add American exercise condition if applicable
            if (exercise != null && exercise.type() != Exercise.Type.European)
            {
                var americanCondition = new FdmAmericanStepCondition(mesher, calculator);
                conditions.Add(americanCondition);
                
                // Add exercise dates as stopping times
                var exerciseTimes = exercise.dates()
                    .Select(date => dayCounter.yearFraction(refDate, date))
                    .ToList();
                stoppingTimesList.Add(exerciseTimes);
            }

            return new FdmStepConditionComposite(stoppingTimesList, conditions);
        }
    }

    /// <summary>
    /// A step condition that takes a snapshot of the solution at a specific time.
    /// </summary>
    public class FdmSnapshotCondition : IStepCondition<Array>
    {
        public double Time { get; }
        private Array _values;

        public FdmSnapshotCondition(double time)
        {
            Time = time;
        }

        public void ApplyTo(Array a, double t)
        {
            if (Math.Abs(t - Time) < 1e-8) // Close enough to snapshot time
            {
                _values = new Array(a); // Take a copy
            }
        }

        public Array GetValues() => _values;
    }
}