// Exercise.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace Antares
{
    /// <summary>
    /// Base interface for instrument exercise schedules.
    /// </summary>
    public interface IExercise
    {
        Exercise.ExerciseType Type { get; }
        IReadOnlyList<Date> Dates { get; }
        Date LastDate { get; }
        Date Date(int index);
    }

    /// <summary>
    /// Base exercise class.
    /// </summary>
    public abstract class Exercise : IExercise
    {
        public enum ExerciseType
        {
            American,
            Bermudan,
            European
        }

        protected List<Date> _dates;

        protected Exercise(ExerciseType type)
        {
            Type = type;
        }

        // Inspectors
        public ExerciseType Type { get; }
        public IReadOnlyList<Date> Dates => _dates;

        public Date LastDate
        {
            get
            {
                if (_dates == null || _dates.Count == 0)
                    throw new InvalidOperationException("No exercise date given.");
                return _dates.Last();
            }
        }

        public Date Date(int index) => _dates[index];
    }

    /// <summary>
    /// Early-exercise base class.
    /// </summary>
    /// <remarks>The payoff can be at exercise (the default) or at expiry.</remarks>
    public abstract class EarlyExercise : Exercise
    {
        protected EarlyExercise(ExerciseType type, bool payoffAtExpiry = false)
            : base(type)
        {
            PayoffAtExpiry = payoffAtExpiry;
        }

        public bool PayoffAtExpiry { get; }
    }

    /// <summary>
    /// American exercise.
    /// </summary>
    /// <remarks>
    /// An American option can be exercised at any time between two
    /// predefined dates; the first date might be omitted, in which
    /// case the option can be exercised at any time before the expiry.
    /// </remarks>
    public sealed class AmericanExercise : EarlyExercise
    {
        public AmericanExercise(Date earliestDate, Date latestDate, bool payoffAtExpiry = false)
            : base(ExerciseType.American, payoffAtExpiry)
        {
            if (earliestDate > latestDate)
                throw new ArgumentException("earliest > latest exercise date");

            _dates = new List<Date> { earliestDate, latestDate };
        }

        public AmericanExercise(Date latestDate, bool payoffAtExpiry = false)
            : this(Date.minDate(), latestDate, payoffAtExpiry)
        {
        }
    }

    /// <summary>
    /// Bermudan exercise.
    /// </summary>
    /// <remarks>A Bermudan option can only be exercised at a set of fixed dates.</remarks>
    public sealed class BermudanExercise : EarlyExercise
    {
        public BermudanExercise(IEnumerable<Date> dates, bool payoffAtExpiry = false)
            : base(ExerciseType.Bermudan, payoffAtExpiry)
        {
            if (dates == null || !dates.Any())
                throw new ArgumentException("No exercise dates given.", nameof(dates));

            _dates = dates.ToList();
            _dates.Sort();
        }
    }

    /// <summary>
    /// European exercise.
    /// </summary>
    /// <remarks>A European option can only be exercised at one (expiry) date.</remarks>
    public sealed class EuropeanExercise : Exercise
    {
        public EuropeanExercise(Date date)
            : base(ExerciseType.European)
        {
            _dates = new List<Date> { date };
        }
    }
}