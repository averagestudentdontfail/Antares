// Quote.cs

using System;

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
        Real value();

        /// <summary>
        /// Returns true if the Quote holds a valid value.
        /// </summary>
        bool isValid();
    }

    /// <summary>
    /// Simple concrete quote implementation.
    /// </summary>
    public class SimpleQuote : IQuote
    {
        private Real? _value;
        private readonly Observable _observable = new Observable();

        public SimpleQuote() : this(null) { }

        public SimpleQuote(Real? value)
        {
            _value = value;
        }

        public Real value()
        {
            if (!_value.HasValue)
                throw new InvalidOperationException("invalid quote");
            return _value.Value;
        }

        public bool isValid() => _value.HasValue;

        public void setValue(Real? value)
        {
            var oldValue = _value;
            _value = value;
            if (oldValue != _value)
                _observable.NotifyObservers();
        }

        public void reset() => setValue(null);

        // IObservable implementation
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
    }

    /// <summary>
    /// Contains helper methods related to <see cref="IQuote"/>.
    /// </summary>
    public static class QuoteExtensions
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

        /// <summary>
        /// Creates a handle to a SimpleQuote with the given value.
        /// </summary>
        public static Handle<IQuote> makeQuoteHandle(Real value)
        {
            return new Handle<IQuote>(new SimpleQuote(value));
        }
    }
}