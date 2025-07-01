// Settings.cs

using System;
using System.Collections.Generic;
using QLNet;
using QLNet.Time;

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

    /// <summary>
    /// A value that notifies observers when it changes.
    /// </summary>
    public class ObservableValue<T> : IObservable
    {
        private readonly Observable _observable = new Observable();
        private T _value;

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

        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
    }
    #endregion

    /// <summary>
    /// Global repository for run-time library settings.
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// A proxy for the evaluation date that handles floating vs. fixed dates and notifications.
        /// </summary>
        public class DateProxy : ObservableValue<Date>
        {
            /// <summary>
            /// Initializes the proxy with a null date, indicating a floating evaluation date.
            /// </summary>
            public DateProxy() : base(new Date()) { }

            /// <summary>
            /// Implicit conversion to Date. Returns today's date if the stored date is null,
            /// otherwise returns the stored date.
            /// </summary>
            public static implicit operator Date(DateProxy proxy)
            {
                // QLNet's default Date() is a null date.
                if (proxy.Value == new Date())
                    return Date.Today;
                return proxy.Value;
            }

            public override string ToString()
            {
                return ((Date)this).ToString();
            }
        }

        private static readonly DateProxy evaluationDateProxy = new DateProxy();
        private static bool _includeReferenceDateEvents = false;
        private static bool? _includeTodaysCashFlows;
        private static bool _enforcesTodaysHistoricFixings = false;

        /// <summary>
        /// The date at which pricing is to be performed.
        /// <para>
        /// Client code can inspect the evaluation date, set it to a new value,
        /// and register with it to be notified of changes.
        /// </para>
        /// <para>
        /// If the underlying value is null, this property will return today's date.
        /// </para>
        /// <example>
        /// <code>
        /// // Get the effective evaluation date
        /// Date d = Settings.EvaluationDate;
        ///
        /// // Set a fixed evaluation date
        /// Settings.EvaluationDate.Value = new Date(15, Month.May, 2023);
        ///
        /// // Register for notifications
        /// Settings.EvaluationDate.RegisterWith(myObserver);
        /// </code>
        /// </example>
        /// </summary>
        public static DateProxy EvaluationDate => evaluationDateProxy;

        /// <summary>
        /// Call this to prevent the evaluation date from changing at midnight.
        /// If no evaluation date was previously set, it is set to today's date.
        /// If an evaluation date was already set, this has no effect.
        /// </summary>
        public static void AnchorEvaluationDate()
        {
            // Set to today's date only if it's not already set (i.e., is null).
            if (evaluationDateProxy.Value == new Date())
                evaluationDateProxy.Value = Date.Today;
        }

        /// <summary>
        /// Call this to reset the evaluation date to be today's date and allow it to
        /// change at midnight. This is achieved by setting the underlying value to null.
        /// </summary>
        public static void ResetEvaluationDate()
        {
            evaluationDateProxy.Value = new Date();
        }

        /// <summary>
        /// This flag specifies whether or not Events occurring on the reference
        /// date should, by default, be taken into account as not happened yet.
        /// </summary>
        public static bool IncludeReferenceDateEvents
        {
            get => _includeReferenceDateEvents;
            set => _includeReferenceDateEvents = value;
        }

        /// <summary>
        /// If set, this flag specifies whether or not CashFlows occurring on today's
        /// date should enter the NPV. When the NPV date equals today's date, this
        /// flag overrides the behavior chosen for IncludeReferenceDateEvents.
        /// </summary>
        public static bool? IncludeTodaysCashFlows
        {
            get => _includeTodaysCashFlows;
            set => _includeTodaysCashFlows = value;
        }

        /// <summary>
        /// If set, this flag specifies whether or not historical fixings for today's date
        /// are enforced.
        /// </summary>
        public static bool EnforcesTodaysHistoricFixings
        {
            get => _enforcesTodaysHistoricFixings;
            set => _enforcesTodaysHistoricFixings = value;
        }
    }


    /// <summary>
    /// Helper class to temporarily and safely change the global settings.
    /// When an instance of this class is disposed (e.g., at the end of a 'using' block),
    /// the settings are restored to their state at the time of construction.
    /// </summary>
    public class SavedSettings : IDisposable
    {
        private readonly Date _evaluationDate;
        private readonly bool _includeReferenceDateEvents;
        private readonly bool? _includeTodaysCashFlows;
        private readonly bool _enforcesTodaysHistoricFixings;
        private readonly Date _rawEvaluationDate;

        public SavedSettings()
        {
            _evaluationDate = Settings.EvaluationDate; // Implicit conversion gets effective date
            _rawEvaluationDate = Settings.EvaluationDate.Value; // Get the underlying value
            _includeReferenceDateEvents = Settings.IncludeReferenceDateEvents;
            _includeTodaysCashFlows = Settings.IncludeTodaysCashFlows;
            _enforcesTodaysHistoricFixings = Settings.EnforcesTodaysHistoricFixings;
        }

        public void Dispose()
        {
            // Restore settings. The logic here carefully replicates the C++ RAII behavior.
            // If the date was floating, it will be restored to its original floating state
            // unless the effective date changed for other reasons (e.g. day roll-over).
            Date currentEvaluationDate = Settings.EvaluationDate;
            if (currentEvaluationDate != _evaluationDate)
            {
                Settings.EvaluationDate.Value = _rawEvaluationDate;
            }

            Settings.IncludeReferenceDateEvents = _includeReferenceDateEvents;
            Settings.IncludeTodaysCashFlows = _includeTodaysCashFlows;
            Settings.EnforcesTodaysHistoricFixings = _enforcesTodaysHistoricFixings;
        }
    }
}