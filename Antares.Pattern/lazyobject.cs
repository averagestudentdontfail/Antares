// C# code for LazyObject.cs

using System;
using System.Collections.Generic;

namespace Antares.Pattern
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
        public void RegisterWith(IObserver observer) { if (!_observers.Contains(observer)) _observers.Add(observer); }
        public void UnregisterWith(IObserver observer) => _observers.Remove(observer);
        public void NotifyObservers()
        {
            var observersCopy = new List<IObserver>(_observers);
            foreach (var observer in observersCopy) observer.Update();
        }
    }

    // Placeholder for Settings from userconfig.cs
    public static partial class Settings
    {
        // public static readonly bool ThrowInCycles = false;
        // public static readonly bool FasterLazyObjects = true;
    }
    #endregion


    /// <summary>
    /// Framework for calculation on demand and result caching.
    /// </summary>
    public abstract class LazyObject : IObserver, IObservable
    {
        private readonly Observable _observable = new Observable();
        protected bool _calculated;
        protected bool _frozen;
        protected bool _alwaysForward;
        private bool _updating;

        protected LazyObject()
        {
            _calculated = false;
            _frozen = false;
            _updating = false;
            _alwaysForward = !Settings.FasterLazyObjects; // Defaults.ForwardsAllNotifications
        }

        /// <summary>
        /// Returns true if the object has been calculated since the last invalidation.
        /// </summary>
        public bool IsCalculated => _calculated;

        /// <summary>
        /// This method is called when an observed object changes.
        /// It marks this object as "dirty" (not calculated) and notifies its own observers.
        /// </summary>
        public virtual void Update()
        {
            if (_updating)
            {
                if (Settings.ThrowInCycles)
                {
                    QL.Fail("recursive notification loop detected; you probably created an object cycle");
                }
                return; // break the cycle
            }

            // This sets _updating to true and will set it back to false when we exit this scope.
            // This is the C# equivalent of the C++ RAII UpdateChecker.
            _updating = true;
            try
            {
                // Forwards notifications only the first time.
                if (_calculated || _alwaysForward)
                {
                    // Set to false early to prevent infinite recursion and to ensure
                    // non-lazy observers are served fresh data.
                    _calculated = false;

                    // Observers don't expect notifications from frozen objects.
                    if (!_frozen)
                    {
                        NotifyObservers();
                    }
                }
            }
            finally
            {
                _updating = false;
            }
        }

        #region Calculation methods
        /// <summary>
        /// Forces the recalculation of any results which would otherwise be cached.
        /// </summary>
        /// <remarks>
        /// Explicit invocation of this method is not necessary if the object
        /// registered itself as an observer of the structures on which its results depend.
        /// </remarks>
        public void Recalculate()
        {
            bool wasFrozen = _frozen;
            _calculated = false;
            _frozen = false;
            try
            {
                Calculate();
            }
            catch
            {
                _frozen = wasFrozen;
                NotifyObservers(); // Notify of failure
                throw;
            }

            _frozen = wasFrozen;
            NotifyObservers(); // Notify of success
        }

        /// <summary>
        /// Constrains the object to return the presently cached results on successive
        /// invocations, even if arguments upon which they depend should change.
        /// </summary>
        public void Freeze()
        {
            _frozen = true;
        }

        /// <summary>
        /// Reverts the effect of the Freeze method, thus re-enabling recalculations.
        /// </summary>
        public void Unfreeze()
        {
            // Send notifications, just in case we lost any, but only once if it was frozen.
            if (_frozen)
            {
                _frozen = false;
                NotifyObservers();
            }
        }

        /// <summary>
        /// This method performs all needed calculations by calling PerformCalculations().
        /// </summary>
        /// <remarks>
        /// Objects cache the results of the previous calculation. Such results will be
        /// returned upon later invocations of Calculate(). When the results depend on
        /// arguments which could change between invocations, the lazy object must
        /// register itself as an observer of such objects for the calculations to be
        /// performed again when they change.
        /// </remarks>
        protected virtual void Calculate()
        {
            if (!_calculated && !_frozen)
            {
                _calculated = true; // prevent infinite recursion in case of bootstrapping
                try
                {
                    PerformCalculations();
                }
                catch
                {
                    _calculated = false; // allow recalculation on error
                    throw;
                }
            }
        }

        /// <summary>
        /// This method must implement any calculations which must be (re)done
        /// in order to calculate the desired results.
        /// </summary>
        protected abstract void PerformCalculations();
        #endregion

        #region Notification settings
        /// <summary>
        /// Causes the object to forward only the first notification received,
        // and discard others until recalculated. This is the default "faster" behavior.
        /// </summary>
        public void ForwardFirstNotificationOnly()
        {
            _alwaysForward = false;
        }

        /// <summary>
        /// Causes the object to forward all notifications received.
        /// Although safer, this behavior is slower.
        /// </summary>
        public void AlwaysForwardNotifications()
        {
            _alwaysForward = true;
        }
        #endregion

        #region IObservable implementation
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
        #endregion
    }
}