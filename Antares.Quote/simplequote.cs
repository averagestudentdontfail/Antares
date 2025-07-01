// C# code for SimpleQuote.cs

using System;
using System.Collections.Generic;
using Antares.Pattern;

namespace Antares.Quote
{
    #region Supporting Infrastructure (Placeholders)
    // This infrastructure is included to make the file self-contained and compilable.
    // In a real project, these would be referenced via `using` statements.

    /// <summary>
    /// Placeholder for the IQuote interface.
    /// </summary>
    public interface IQuote : IObservable
    {
        Real Value { get; }
        bool IsValid { get; }
    }
    #endregion

    /// <summary>
    /// Market element returning a stored value.
    /// </summary>
    public class SimpleQuote : IQuote
    {
        private readonly Observable _observable = new Observable();
        private Real? _value;

        public SimpleQuote(Real? value = null)
        {
            _value = value;
        }

        #region Quote interface
        /// <summary>
        /// The current value of the quote.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the quote is not valid.</exception>
        public Real Value
        {
            get
            {
                QL.Ensure(IsValid, "invalid SimpleQuote");
                return _value.Value;
            }
        }

        /// <summary>
        /// Returns true if the quote holds a valid value.
        /// </summary>
        public bool IsValid => _value.HasValue;
        #endregion

        #region Modifiers
        /// <summary>
        /// Sets the value of the quote and notifies observers if it changes.
        /// </summary>
        /// <param name="value">The new value. Can be null to invalidate the quote.</param>
        /// <returns>The difference between the new value and the old value.</returns>
        public Real SetValue(Real? value = null)
        {
            Real diff = (value ?? 0.0) - (_value ?? 0.0);
            if (Math.Abs(diff) > 1e-15)
            {
                _value = value;
                _observable.NotifyObservers();
            }
            return diff;
        }

        /// <summary>
        /// Resets the quote to an invalid state.
        /// </summary>
        public void Reset()
        {
            SetValue(null);
        }
        #endregion

        #region IObservable implementation
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        #endregion
    }

    /// <summary>
    /// Provides factory methods for creating quote-related objects.
    /// </summary>
    public static class QuoteFactory
    {
        /// <summary>
        /// Creates a relinkable handle to a new SimpleQuote instance.
        /// </summary>
        /// <param name="value">The initial value for the quote.</param>
        /// <returns>A new RelinkableHandle pointing to a SimpleQuote.</returns>
        public static RelinkableHandle<IQuote> MakeQuoteHandle(Real value)
        {
            return new RelinkableHandle<IQuote>(new SimpleQuote(value));
        }
    }
}