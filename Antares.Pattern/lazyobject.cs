// LazyObject.cs

using System;
using Antares;

namespace Antares.Pattern
{
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
            _alwaysForward = !Settings.FasterLazyObjects; // Fixed: removed Antares. prefix
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
            // Skip if the calculation was explicitly frozen
            if (_frozen) return;

            // Skip if we're already updating to avoid infinite recursion
            if (_updating) return;

            bool wasCalculated = _calculated;
            _calculated = false;

            // Only forward the notification if:
            // 1. We always forward notifications (_alwaysForward is true), OR
            // 2. The object was calculated before (wasCalculated is true)
            if (_alwaysForward || wasCalculated)
            {
                _updating = true;
                try
                {
                    NotifyObservers();
                }
                finally
                {
                    _updating = false;
                }
            }
        }

        /// <summary>
        /// This method forces the recalculation of any results which would otherwise be cached.
        /// It is not called automatically; the user must call it explicitly.
        /// </summary>
        public virtual void Recalculate()
        {
            bool wasFrozen = _frozen;
            _calculated = false;
            _frozen = false;
            try
            {
                Calculate();
            }
            finally
            {
                _frozen = wasFrozen;
            }
        }

        /// <summary>
        /// This method constrains the object to return the presently cached results on successive invocations,
        /// even if the inputs which the results depend upon change.
        /// </summary>
        public virtual void Freeze()
        {
            _frozen = true;
        }

        /// <summary>
        /// This method reverts the effect of the freeze method.
        /// </summary>
        public virtual void Unfreeze()
        {
            _frozen = false;

            // If there was any result invalidation while we were frozen, we need to signal it now
            NotifyObservers();
        }

        /// <summary>
        /// This method performs all needed calculations by calling the performCalculations method.
        /// Objects cache the results of the previous calculation.
        /// Such results will be returned upon later invocations of calculate until the object is invalidated.
        /// When the object is invalidated a new calculation will be performed the next time calculate is called.
        /// </summary>
        public virtual void Calculate()
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
                    _calculated = false;
                    throw;
                }
            }
        }

        /// <summary>
        /// This method must implement any calculations which must be (re)done
        /// in order to calculate the results which will be returned by the method of the object.
        /// </summary>
        protected abstract void PerformCalculations();

        /// <summary>
        /// This method sets the object to always forward all notifications,
        /// even when not calculated. After calling this method,
        /// the object will forward the first notification received,
        /// and all subsequent ones until recalculation is triggered.
        /// The object will not forward notifications whose generation is triggered by recalculation.
        /// </summary>
        /// <remarks>
        /// For backwards compatibility, this is enabled by default unless Settings.FasterLazyObjects is true.
        /// </remarks>
        protected void SetAlwaysForward()
        {
            _alwaysForward = true;
        }

        #region IObservable implementation
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
        #endregion
    }
}