// Observablevalue.cs

using System.Collections.Generic;
using Antares.Pattern;

namespace Antares.Utility
{
    /// <summary>
    /// An observable and assignable proxy to a concrete value.
    /// </summary>
    /// <remarks>
    /// Observers can be registered with instances of this class so that they are
    /// notified when a different value is assigned. Client code can access the
    /// contained value through the Value property or via implicit conversion.
    /// </remarks>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    public class ObservableValue<T> : IObservable
    {
        private T _value;
        private readonly Observable _observable = new Observable();

        #region Constructors
        /// <summary>
        /// Creates a new instance with the default value for type T.
        /// </summary>
        public ObservableValue()
        {
            _value = default;
        }

        /// <summary>
        /// Creates a new instance with the given initial value.
        /// </summary>
        /// <param name="value">The initial value.</param>
        public ObservableValue(T value)
        {
            _value = value;
        }
        #endregion

        /// <summary>
        /// Gets or sets the underlying value.
        /// Setting a new value will notify all registered observers.
        /// </summary>
        public T Value
        {
            get => _value;
            set
            {
                // To avoid redundant notifications, only notify if the value actually changes.
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    _observable.NotifyObservers();
                }
            }
        }

        #region Implicit Conversion
        /// <summary>
        /// Allows implicit conversion from an ObservableValue<T> to its underlying type T.
        /// This enables read-only access where a T is expected.
        /// </summary>
        public static implicit operator T(ObservableValue<T> ov)
        {
            return ov.Value;
        }
        #endregion

        #region IObservable Implementation
        /// <summary>
        /// Registers an observer to be notified of value changes.
        /// </summary>
        /// <param name="observer">The observer to register.</param>
        public void RegisterWith(IObserver observer)
        {
            _observable.RegisterWith(observer);
        }

        /// <summary>
        /// Unregisters an observer.
        /// </summary>
        /// <param name="observer">The observer to unregister.</param>
        public void UnregisterWith(IObserver observer)
        {
            _observable.UnregisterWith(observer);
        }
        #endregion
    }
}