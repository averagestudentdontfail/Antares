// Quote.cs

using System;
using Antares.Pattern;
using Antares.Quote;

namespace Antares
{
    /// <summary>
    /// Purely abstract base class for market observables.
    /// In C#, this is represented as an interface.
    /// </summary>
    public interface IQuote : IObservable
    {
        /// <summary>
        /// Returns the current value of the quote.
        /// </summary>
        Real Value { get; }

        /// <summary>
        /// Returns true if the Quote holds a valid value.
        /// </summary>
        bool IsValid { get; }
    }

    /// <summary>
    /// Contains helper methods related to <see cref="IQuote"/>.
    /// </summary>
    public static class Quote
    {
        /// <summary>
        /// Creates a Handle&lt;IQuote&gt; from a variant input, which can be either a Real or an existing Handle&lt;IQuote&gt;.
        /// </summary>
        /// <param name="value">The input value, which must be of type Real (double) or Handle&lt;IQuote&gt;.</param>
        /// <returns>A valid Handle&lt;IQuote&gt;.</returns>
        /// <exception cref="ArgumentException">Thrown if the input object is not of a supported type.</exception>
        public static Handle<IQuote> HandleFromVariant(object value)
        {
            switch (value)
            {
                case Real realValue:
                    // Equivalent to QL's makeQuoteHandle(x) which creates a Handle to a new SimpleQuote.
                    return new Handle<IQuote>(new SimpleQuote(realValue));
                case Handle<IQuote> handleValue:
                    return handleValue;
                default:
                    throw new ArgumentException($"Input must be a {nameof(Real)} or a {nameof(Handle<IQuote>)}.", nameof(value));
            }
        }
    }
}