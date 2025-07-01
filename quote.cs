// Quote.cs

using System;
using System.Collections.Generic;
using QLNet;

namespace Antares
{
    #region Supporting Infrastructure (Normally in separate files)
    // This infrastructure is included to make the file self-contained and compilable.
    // In a real project, these would be in their own files.

    /// <summary>
    /// Observer interface for the observer pattern.
    /// </summary>
    public interface IObserver
    {
        void Update();
    }

    /// <summary>
    /// Observable interface for the observer pattern.
    /// </summary>
    public interface IObservable
    {
        void RegisterWith(IObserver observer);
        void UnregisterWith(IObserver observer);
    }

    /// <summary>
    /// Concrete implementation of IObservable to be used via composition.
    /// </summary>
    public class Observable : IObservable
    {
        private readonly List<IObserver> _observers = new List<IObserver>();

        public void RegisterWith(IObserver observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        public void UnregisterWith(IObserver observer) => _observers.Remove(observer);

        public void NotifyObservers()
        {
            var observersCopy = new List<IObserver>(_observers);
            foreach (var observer in observersCopy)
                observer.Update();
        }
    }
    #endregion

    #region Placeholders for future ports

    /// <summary>
    /// Placeholder for the SimpleQuote class. This will be replaced by a full port later.
    /// It provides a basic implementation of IQuote around a single value.
    /// </summary>
    public class SimpleQuote : IQuote
    {
        private readonly Observable _observable = new Observable();
        private Real? _value;

        public SimpleQuote(Real? value = null)
        {
            _value = value;
        }

        public Real Value
        {
            get
            {
                if (!IsValid)
                    throw new InvalidOperationException("Invalid quote");
                return _value.Value;
            }
        }

        public bool IsValid => _value.HasValue;

        public void SetValue(Real value)
        {
            if (_value != value)
            {
                _value = value;
                _observable.NotifyObservers();
            }
        }

        public void Reset()
        {
            _value = null;
            _observable.NotifyObservers();
        }

        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
    }

    #endregion

    /// <summary>
    /// Purely virtual base class for market observables.
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
    /// Contains helper methods related to IQuote.
    /// </summary>
    public static class Quote
    {
        /// <summary>
        /// Creates a Handle<IQuote> from a variant input, which can be either a Real or an existing Handle<IQuote>.
        /// </summary>
        /// <param name="value">The input value, which must be of type Real (double) or Handle<IQuote>.</param>
        /// <returns>A valid Handle<IQuote>.</returns>
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