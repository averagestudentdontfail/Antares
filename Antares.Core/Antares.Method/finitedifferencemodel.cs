// C# code for FiniteDifferenceModel.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace Antares.Method
{
    /// <summary>
    /// Interface for evolver traits that define the types used in finite difference methods
    /// </summary>
    /// <typeparam name="TOperator">The operator type</typeparam>
    /// <typeparam name="TArray">The array type</typeparam>
    /// <typeparam name="TBcSet">The boundary condition set type</typeparam>
    /// <typeparam name="TCondition">The condition type</typeparam>
    public interface IEvolverTraits<TOperator, TArray, TBcSet, TCondition>
    {
        // Marker interface for traits
    }

    /// <summary>
    /// Interface for evolvers used in finite difference models
    /// </summary>
    /// <typeparam name="TArray">The array type</typeparam>
    /// <typeparam name="TCondition">The condition type</typeparam>
    public interface IEvolver<TArray, TCondition>
    {
        /// <summary>
        /// Sets the time step for the evolver
        /// </summary>
        /// <param name="dt">The time step</param>
        void SetStep(Time dt);

        /// <summary>
        /// Performs one evolution step
        /// </summary>
        /// <param name="a">The array to evolve</param>
        /// <param name="t">The current time</param>
        void Step(TArray a, Time t);
    }

    /// <summary>
    /// Generic finite difference model
    /// </summary>
    /// <typeparam name="TEvolver">The evolver type</typeparam>
    /// <typeparam name="TOperator">The operator type</typeparam>
    /// <typeparam name="TArray">The array type</typeparam>
    /// <typeparam name="TBcSet">The boundary condition set type</typeparam>
    /// <typeparam name="TCondition">The condition type</typeparam>
    public class FiniteDifferenceModel<TEvolver, TOperator, TArray, TBcSet, TCondition>
        where TEvolver : IEvolver<TArray, TCondition>
        where TCondition : class
    {
        private readonly TEvolver _evolver;
        private readonly List<Time> _stoppingTimes;

        #region Constructors
        /// <summary>
        /// Constructs a finite difference model from an operator and boundary conditions
        /// </summary>
        /// <param name="operatorL">The differential operator</param>
        /// <param name="bcs">The boundary conditions</param>
        /// <param name="stoppingTimes">Optional stopping times</param>
        public FiniteDifferenceModel(TOperator operatorL, TBcSet bcs, IEnumerable<Time> stoppingTimes = null)
        {
            // This constructor would need a factory method to create the evolver
            // Since we can't instantiate TEvolver directly, this constructor signature
            // might need to be adjusted based on the actual evolver implementation
            throw new NotImplementedException("This constructor requires evolver factory implementation");
        }

        /// <summary>
        /// Constructs a finite difference model from an evolver
        /// </summary>
        /// <param name="evolver">The evolver</param>
        /// <param name="stoppingTimes">Optional stopping times</param>
        public FiniteDifferenceModel(TEvolver evolver, IEnumerable<Time> stoppingTimes = null)
        {
            _evolver = evolver;
            _stoppingTimes = ProcessStoppingTimes(stoppingTimes);
        }
        #endregion

        #region Properties
        /// <summary>
        /// Gets the evolver used by this model
        /// </summary>
        public TEvolver Evolver => _evolver;
        #endregion

        #region Public Methods
        /// <summary>
        /// Solves the problem between the given times.
        /// Warning: being this a rollback, 'from' must be a later time than 'to'.
        /// </summary>
        /// <param name="a">The array to evolve</param>
        /// <param name="from">The starting time</param>
        /// <param name="to">The ending time</param>
        /// <param name="steps">The number of steps</param>
        public void Rollback(TArray a, Time from, Time to, Size steps)
        {
            RollbackImpl(a, from, to, steps, null);
        }

        /// <summary>
        /// Solves the problem between the given times, applying a condition at every step.
        /// Warning: being this a rollback, 'from' must be a later time than 'to'.
        /// </summary>
        /// <param name="a">The array to evolve</param>
        /// <param name="from">The starting time</param>
        /// <param name="to">The ending time</param>
        /// <param name="steps">The number of steps</param>
        /// <param name="condition">The condition to apply at each step</param>
        public void Rollback(TArray a, Time from, Time to, Size steps, TCondition condition)
        {
            RollbackImpl(a, from, to, steps, condition);
        }
        #endregion

        #region Private Methods
        private static List<Time> ProcessStoppingTimes(IEnumerable<Time> stoppingTimes)
        {
            if (stoppingTimes == null)
                return new List<Time>();

            return stoppingTimes
                .OrderBy(t => t)
                .Distinct()
                .ToList();
        }

        private void RollbackImpl(TArray a, Time from, Time to, Size steps, TCondition condition)
        {
            QL.Require(from >= to, 
                $"trying to roll back from {from} to {to}");

            Time dt = (from - to) / steps;
            Time t = from;
            _evolver.SetStep(dt);

            if (_stoppingTimes.Count > 0 && Math.Abs(_stoppingTimes.Last() - from) < QLDefines.EPSILON)
            {
                condition?.ApplyTo(a, from);
            }

            for (Size i = 0; i < steps; ++i, t -= dt)
            {
                Time now = t;
                // Make sure last step ends exactly on "to" in order to not
                // miss a stopping time at "to" due to numerical issues
                Time next = (i < steps - 1) ? t - dt : to;

                if (Math.Abs(to - next) < Math.Sqrt(QLDefines.EPSILON))
                    next = to;

                bool hit = false;
                for (int j = _stoppingTimes.Count - 1; j >= 0; --j)
                {
                    if (next <= _stoppingTimes[j] && _stoppingTimes[j] < now)
                    {
                        // A stopping time was hit
                        hit = true;

                        // Perform a small step to stoppingTimes[j]...
                        _evolver.SetStep(now - _stoppingTimes[j]);
                        _evolver.Step(a, now);
                        condition?.ApplyTo(a, _stoppingTimes[j]);
                        
                        // ...and continue the cycle
                        now = _stoppingTimes[j];
                    }
                }

                // If we did hit...
                if (hit)
                {
                    // ...we might have to make a small step to complete the big one...
                    if (now > next)
                    {
                        _evolver.SetStep(now - next);
                        _evolver.Step(a, now);
                        condition?.ApplyTo(a, next);
                    }
                    
                    // ...and in any case, we have to reset the evolver to the default step.
                    _evolver.SetStep(dt);
                }
                else
                {
                    // If we didn't, the evolver is already set to the default step, which is ok for us.
                    _evolver.Step(a, now);
                    condition?.ApplyTo(a, next);
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Simplified finite difference model for common use cases
    /// </summary>
    /// <typeparam name="TEvolver">The evolver type</typeparam>
    public class FiniteDifferenceModel<TEvolver> : FiniteDifferenceModel<TEvolver, object, Array, object, IStepCondition>
        where TEvolver : IEvolver<Array, IStepCondition>
    {
        /// <summary>
        /// Constructs a finite difference model from an evolver
        /// </summary>
        /// <param name="evolver">The evolver</param>
        /// <param name="stoppingTimes">Optional stopping times</param>
        public FiniteDifferenceModel(TEvolver evolver, IEnumerable<Time> stoppingTimes = null)
            : base(evolver, stoppingTimes)
        {
        }
    }
}