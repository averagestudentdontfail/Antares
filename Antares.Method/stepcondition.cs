// C# code for StepCondition.cs

namespace Antares.Method
{
    /// <summary>
    /// Interface for conditions to be applied at every time step
    /// </summary>
    /// <typeparam name="TArray">The array type</typeparam>
    public interface IStepCondition<in TArray>
    {
        /// <summary>
        /// Applies the condition to the given array at the specified time
        /// </summary>
        /// <param name="a">The array to apply the condition to</param>
        /// <param name="t">The current time</param>
        void ApplyTo(TArray a, Time t);
    }

    /// <summary>
    /// Abstract base class for step conditions
    /// </summary>
    /// <typeparam name="TArray">The array type</typeparam>
    public abstract class StepCondition<TArray> : IStepCondition<TArray>
    {
        /// <summary>
        /// Applies the condition to the given array at the specified time
        /// </summary>
        /// <param name="a">The array to apply the condition to</param>
        /// <param name="t">The current time</param>
        public abstract void ApplyTo(TArray a, Time t);
    }

    /// <summary>
    /// Null step condition that performs no operation
    /// </summary>
    /// <typeparam name="TArray">The array type</typeparam>
    public class NullCondition<TArray> : StepCondition<TArray>
    {
        /// <summary>
        /// Does nothing - this is a null implementation
        /// </summary>
        /// <param name="a">The array (unused)</param>
        /// <param name="t">The time (unused)</param>
        public override void ApplyTo(TArray a, Time t)
        {
            // Intentionally empty - null condition does nothing
        }
    }

    /// <summary>
    /// Specialized step condition for Array type
    /// </summary>
    public abstract class StepCondition : StepCondition<Array>
    {
        // This provides a non-generic base class for Array-specific step conditions
    }

    /// <summary>
    /// Null condition specialized for Array type
    /// </summary>
    public class NullCondition : NullCondition<Array>
    {
        /// <summary>
        /// Singleton instance for convenience
        /// </summary>
        public static readonly NullCondition Instance = new NullCondition();
    }
}