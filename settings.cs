// Settings.cs

using System;
using System.Collections.Generic;

namespace Antares
{
    #region Missing Type Definitions

    /// <summary>
    /// An observable and assignable proxy to a concrete value.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    public class ObservableValue<T> : IObservable
    {
        private T _value;
        private readonly Observable _observable = new Observable();

        public ObservableValue()
        {
            _value = default;
        }

        public ObservableValue(T value)
        {
            _value = value;
        }

        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    _observable.NotifyObservers();
                }
            }
        }

        public static implicit operator T(ObservableValue<T> ov)
        {
            return ov.Value;
        }

        public void RegisterWith(IObserver observer)
        {
            _observable.RegisterWith(observer);
        }

        public void UnregisterWith(IObserver observer)
        {
            _observable.UnregisterWith(observer);
        }
    }

    #endregion

    /// <summary>
    /// Global settings for the Antares library.
    /// </summary>
    public static class Settings
    {
        private static readonly ObservableValue<Date> _evaluationDate = new ObservableValue<Date>(Date.Today);
        private static readonly ObservableValue<bool> _includeReferenceDateEvents = new ObservableValue<bool>(false);
        private static readonly ObservableValue<bool?> _includeTodaysCashFlows = new ObservableValue<bool?>(null);
        private static readonly ObservableValue<bool> _enforcesTodaysHistoricFixings = new ObservableValue<bool>(false);

        /// <summary>
        /// Gets or sets the evaluation date for the library.
        /// </summary>
        /// <remarks>
        /// This is the date on which all calculations are performed.
        /// It should normally be set to the current business date.
        /// </remarks>
        public static Date EvaluationDate
        {
            get => _evaluationDate.Value;
            set => _evaluationDate.Value = value;
        }

        /// <summary>
        /// Gets or sets whether events happening on the reference date are considered to have occurred.
        /// </summary>
        /// <remarks>
        /// This affects the behavior of event.HasOccurred() when called on the evaluation date.
        /// </remarks>
        public static bool IncludeReferenceDateEvents
        {
            get => _includeReferenceDateEvents.Value;
            set => _includeReferenceDateEvents.Value = value;
        }

        /// <summary>
        /// Gets or sets whether cash flows occurring on today's date should be included in calculations.
        /// </summary>
        /// <remarks>
        /// This is a nullable boolean: null means "use the global IncludeReferenceDateEvents setting".
        /// </remarks>
        public static bool? IncludeTodaysCashFlows
        {
            get => _includeTodaysCashFlows.Value;
            set => _includeTodaysCashFlows.Value = value;
        }

        /// <summary>
        /// Gets or sets whether today's fixings should be enforced for historic fixings.
        /// </summary>
        /// <remarks>
        /// When true, missing fixings for today's date will cause an error.
        /// When false, missing fixings for today's date will be treated like any other missing fixing.
        /// </remarks>
        public static bool EnforcesTodaysHistoricFixings
        {
            get => _enforcesTodaysHistoricFixings.Value;
            set => _enforcesTodaysHistoricFixings.Value = value;
        }

        /// <summary>
        /// Settings specific to Lazy Object calculations.
        /// </summary>
        public static class LazyObjectSettings
        {
            private static readonly ObservableValue<bool> _fasterLazyObjects = new ObservableValue<bool>(true);

            /// <summary>
            /// Gets or sets whether to use faster lazy object calculations.
            /// </summary>
            /// <remarks>
            /// When true, lazy objects skip some redundant notifications for better performance.
            /// When false, all notifications are forwarded (safer but slower).
            /// </remarks>
            public static bool FasterLazyObjects
            {
                get => _fasterLazyObjects.Value;
                set => _fasterLazyObjects.Value = value;
            }
        }

        // Convenient accessor for backward compatibility
        public static bool FasterLazyObjects => LazyObjectSettings.FasterLazyObjects;
    }
}