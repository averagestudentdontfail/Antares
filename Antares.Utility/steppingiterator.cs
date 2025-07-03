// Steppingiterator.cs

using System;
using System.Collections.Generic;

namespace Antares.Utility
{
    /// <summary>
    /// Provides utility methods for iterating over collections with a constant step.
    /// This class provides an idiomatic C# replacement for the C++ step_iterator.
    /// </summary>
    public static class SteppingUtility
    {
        /// <summary>
        /// Creates an iterator that advances over a collection in constant steps.
        /// </summary>
        /// <remarks>
        /// This method is the idiomatic C# equivalent of QuantLib's step_iterator.
        /// It returns an IEnumerable&lt;T&gt; that can be used in a foreach loop.
        /// The underlying collection must implement IList&lt;T&gt; to guarantee efficient random access, matching the constraint of the original C++ std::random_access_iterator.
        /// </remarks>
        /// <example>
        /// <code>
        /// var numbers = new List&lt;int&gt; { 0, 1, 2, 3, 4, 5, 6 };
        /// // This will yield 0, 3, 6
        /// foreach (var number in numbers.Step(3))
        /// {
        ///     Console.WriteLine(number);
        /// }
        /// </code>
        /// </example>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="source">The source collection to iterate over. Must support random access.</param>
        /// <param name="step">The number of elements to advance for each step. Must be a positive integer.</param>
        /// <returns>An IEnumerable&lt;T&gt; that yields elements from the source collection at each step.</returns>
        public static IEnumerable<T> Step<T>(this IList<T> source, int step)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (step <= 0)
                throw new ArgumentOutOfRangeException(nameof(step), "Step must be a positive integer.");

            for (int i = 0; i < source.Count; i += step)
            {
                yield return source[i];
            }
        }
    }
}