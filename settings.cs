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
        private T? _value;
        private readonly Observable _observable = new Observable();

        public ObservableValue()
        {
            _value = default;
        }

        public ObservableValue(T? value)
        {
            _value = value;
        }

        public T? Value
        public T? Value
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

        public static implicit operator T?(ObservableValue<T> ov)
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
    /// Contains both compile-time configuration and runtime settings.
    /// </summary>
    public static class Settings
    {
        #region Configuration Constants (from userconfig.cs)
        
        /// <summary>
        /// If set to true, function information is added to the error messages
        /// thrown by the library.
        /// In C#, this can be achieved using attributes like CallerMemberName.
        /// </summary>
        public static readonly bool EnableErrorFunctions = false;

        /// <summary>
        /// If set to true, file and line information is added to the error
        /// messages thrown by the library.
        /// In C#, this can be achieved using attributes like CallerFilePath and CallerLineNumber.
        /// </summary>
        public static readonly bool EnableErrorLines = false;

        /// <summary>
        /// If set to true, tracing messages might be emitted by the library
        /// depending on run-time settings. Enabling this option can degrade
        /// performance.
        /// </summary>
        public static readonly bool EnableTracing = false;

        /// <summary>
        /// If set to true, extra run-time checks are added to a few functions. This can prevent their inlining and degrade performance.
        /// </summary>
        public static readonly bool EnableExtraSafetyChecks = false;

        /// <summary>
        /// If set to true, indexed coupons (see the documentation) are used in
        /// floating legs. If set to false, par coupons are used.
        /// </summary>
        public static readonly bool UseIndexedCoupon = false;

        /// <summary>
        /// If set to true, singletons will return different instances for
        /// different threads; in particular, this means that the evaluation
        /// date, the stored index fixings and any other settings will be
        /// per-thread.
        /// In C#, this can be implemented using <see cref="System.Threading.ThreadLocal{T}"/>.
        /// </summary>
        public static readonly bool EnableSessions = false;

        /// <summary>
        /// If set to true, a thread-safe (but less performant) version of the observer pattern is used. You should set this to true if you want to use
        /// the observer pattern in a multi-threaded environment.
        /// </summary>
        public static readonly bool EnableThreadSafeObserverPattern = false;

        /// <summary>
        /// If set to true, date objects will support an intraday datetime
        /// resolution down to microseconds. Strictly monotone daycounters
        /// (`Actual360`, `Actual365Fixed` and `ActualActual`) will take the
        /// additional information into account and allow for accurate intraday
        /// pricing. If set to false, the smallest resolution of date objects is
        /// a single day.
        /// Note: Intraday datetime resolution is experimental.
        /// </summary>
        public static readonly bool HighResolutionDate = false;

        /// <summary>
        /// If set to true, lazy objects will raise an exception when they detect a
        /// notification cycle which would result in an infinite recursion
        /// loop. If set to false, they will break the recursion without throwing.
        /// Enabling this option is recommended but might cause existing code
        /// to throw.
        /// </summary>
        public static readonly bool ThrowInCycles = false;

        /// <summary>
        /// If set to true, lazy objects will forward the first notification
        /// received, and discard the others until recalculated; the rationale
        /// is that observers were already notified, and don't need further
        /// notifications until they recalculate, at which point this object
        /// would be recalculated too. After recalculation, this object would
        /// again forward the first notification received. Although not always
        /// correct, this behavior is a lot faster and thus is the current
        /// default.
        /// </summary>
        public static readonly bool FasterLazyObjectsConfig = true;

        #endregion

        #region Runtime Settings

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

        // Convenient accessor for backward compatibility
        public static bool FasterLazyObjects => FasterLazyObjectsConfig;

        #endregion
    }
}