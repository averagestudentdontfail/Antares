// Observable.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using QLNet;

namespace Antares.Pattern
{
    /// <summary>
    /// Global repository for run-time observer/observable settings.
    /// This is a C# replacement for the C++ `ObservableSettings` singleton.
    /// </summary>
    public static class ObservableSettings
    {
        [ThreadStatic]
        private static bool _updatesEnabled = true;
        [ThreadStatic]
        private static bool _updatesDeferred = false;
        [ThreadStatic]
        private static HashSet<IObserver> _deferredObservers = new HashSet<IObserver>();

        /// <summary>
        /// Gets a value indicating whether updates are currently enabled.
        /// </summary>
        public static bool UpdatesEnabled => _updatesEnabled;

        /// <summary>
        /// Gets a value indicating whether updates are deferred.
        /// If true, notifications are queued until updates are re-enabled.
        /// </summary>
        public static bool UpdatesDeferred => _updatesDeferred;

        /// <summary>
        /// Disables updates. Can optionally defer notifications until updates are re-enabled.
        /// </summary>
        /// <param name="deferred">If true, notifications will be queued. If false, they are discarded.</param>
        public static void DisableUpdates(bool deferred = false)
        {
            _updatesEnabled = false;
            _updatesDeferred = deferred;
            if (deferred && _deferredObservers == null)
            {
                _deferredObservers = new HashSet<IObserver>();
            }
        }

        /// <summary>
        /// Re-enables updates and sends any deferred notifications.
        /// </summary>
        public static void EnableUpdates()
        {
            _updatesEnabled = true;
            _updatesDeferred = false;

            if (_deferredObservers != null && _deferredObservers.Any())
            {
                var observersToNotify = new List<IObserver>(_deferredObservers);
                _deferredObservers.Clear();

                bool successful = true;
                var errMsg = new StringBuilder();

                foreach (var observer in observersToNotify)
                {
                    try
                    {
                        observer.Update();
                    }
                    catch (Exception e)
                    {
                        successful = false;
                        errMsg.AppendLine(e.Message);
                    }
                }

                if (!successful)
                {
                    throw new Exception($"Could not notify one or more observers: {errMsg}");
                }
            }
        }

        internal static void RegisterDeferredObservers(IEnumerable<IObserver> observers)
        {
            if (UpdatesDeferred)
            {
                _deferredObservers?.UnionWith(observers);
            }
        }

        internal static void UnregisterDeferredObserver(IObserver observer)
        {
            if (observer != null)
            {
                _deferredObservers?.Remove(observer);
            }
        }
    }


    /// <summary>
    /// Observer interface for the observer pattern.
    /// </summary>
    public interface IObserver
    {
        /// <summary>
        /// This method is called when an observed object changes.
        /// </summary>
        void Update();
    }

    /// <summary>
    /// Observable interface for the observer pattern.
    /// </summary>
    public interface IObservable
    {
        /// <summary>
        /// Registers an observer to be notified of changes.
        /// </summary>
        void RegisterWith(IObserver observer);

        /// <summary>
        /// Unregisters an observer.
        /// </summary>
        void UnregisterWith(IObserver observer);
    }

    /// <summary>
    /// Object that notifies its changes to a set of observers.
    /// This is a C# implementation of the non-thread-safe `QuantLib::Observable`.
    /// </summary>
    public class Observable : IObservable
    {
        private readonly List<IObserver> _observers = new List<IObserver>();

        public virtual void NotifyObservers()
        {
            if (!ObservableSettings.UpdatesEnabled)
            {
                ObservableSettings.RegisterDeferredObservers(_observers);
            }
            else if (_observers.Any())
            {
                bool successful = true;
                var errMsg = new StringBuilder();

                // A copy of the collection is used to prevent issues if an observer
                // tries to unregister itself during the notification loop.
                var observersCopy = new List<IObserver>(_observers);

                foreach (var observer in observersCopy)
                {
                    try
                    {
                        observer.Update();
                    }
                    catch (Exception e)
                    {
                        successful = false;
                        errMsg.AppendLine(e.Message);
                    }
                }
                if (!successful)
                {
                    throw new Exception($"Could not notify one or more observers: {errMsg}");
                }
            }
        }

        public void RegisterWith(IObserver observer)
        {
            // std::set prevents duplicates, so we do the same.
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
        }

        public void UnregisterWith(IObserver observer)
        {
            if (ObservableSettings.UpdatesDeferred)
            {
                ObservableSettings.UnregisterDeferredObserver(observer);
            }
            _observers.Remove(observer);
        }
    }

    /// <summary>
    /// Object that gets notified when a given observable changes.
    /// This is a C# implementation of the non-thread-safe `QuantLib::Observer`.
    /// </summary>
    public abstract class Observer : IObserver, IDisposable
    {
        private readonly HashSet<IObservable> _observables = new HashSet<IObservable>();
        private bool _disposed;

        /// <summary>
        /// This method must be implemented in derived classes.
        /// </summary>
        public abstract void Update();

        /// <summary>
        /// Registers this observer with the given observable object.
        /// </summary>
        public void RegisterWith(IObservable h)
        {
            if (h != null)
            {
                h.RegisterWith(this);
                _observables.Add(h);
            }
        }

        /// <summary>
        /// Registers with all observables of a given observer.
        /// </summary>
        public void RegisterWithObservables(Observer o)
        {
            if (o != null)
            {
                foreach (var observable in o._observables)
                    RegisterWith(observable);
            }
        }

        /// <summary>
        /// Unregisters this observer from the given observable object.
        /// </summary>
        public void UnregisterWith(IObservable h)
        {
            if (h != null)
            {
                h.UnregisterWith(this);
            }
            _observables.Remove(h);
        }

        /// <summary>
        /// Unregisters this observer from all observables it is currently registered with.
        /// </summary>
        public void UnregisterWithAll()
        {
            foreach (var observable in _observables)
            {
                observable.UnregisterWith(this);
            }
            _observables.Clear();
        }

        /// <summary>
        /// This method allows to explicitly update the instance itself and nested observers.
        /// </summary>
        public virtual void DeepUpdate()
        {
            Update();
        }

        #region IDisposable
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unregister from all observables to break circular references
                    UnregisterWithAll();
                }
                _disposed = true;
            }
        }

        ~Observer()
        {
            Dispose(false);
        }
        #endregion

        /// <summary>
        /// Returns true if the observer is disposed.
        /// </summary>
        public bool IsDisposed => _disposed;
    }
}