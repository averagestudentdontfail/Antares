// Handle.cs

using System;

namespace Antares
{
    /// <summary>
    /// Shared handle to any observable.
    /// </summary>
    /// <typeparam name="T">The type of observable object this handle points to.</typeparam>
    /// <remarks>
    /// All copies of an instance of this class refer to the same observable by means of a relinkable smart pointer.
    /// When such pointer is relinked to another observable, the change will be propagated to all the copies.
    /// 
    /// For performance/space reasons, it should be kept as close as possible to a bare pointer.
    /// </remarks>
    public class Handle<T> where T : class, IObservable
    {
        private readonly Link _link;

        #region Constructors
        /// <summary>
        /// Default constructor. Builds an empty handle.
        /// </summary>
        public Handle()
        {
            _link = new Link(null, true);
        }

        /// <summary>
        /// Builds a handle pointing to the given object.
        /// </summary>
        /// <param name="h">The object to point to.</param>
        /// <param name="registerAsObserver">Whether to register as an observer of the object.</param>
        public Handle(T? h, bool registerAsObserver = true)
        {
            _link = new Link(h, registerAsObserver);
        }
        #endregion

        #region Dereferencing
        /// <summary>
        /// Dereferencing operator.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the handle is empty.</exception>
        public T this[int dummy]
        {
            get
            {
                if (empty())
                    throw new InvalidOperationException("Empty Handle cannot be dereferenced");
                return _link.currentLink()!;
            }
        }

        /// <summary>
        /// Member access operator.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the handle is empty.</exception>
        public T currentLink()
        {
            if (empty())
                throw new InvalidOperationException("Empty Handle cannot be dereferenced");
            return _link.currentLink()!;
        }
        #endregion

        #region Inspectors
        /// <summary>
        /// Checks if the contained pointer is null.
        /// </summary>
        public bool empty() => _link.empty();

        /// <summary>
        /// Allows the handle to be used as an observable.
        /// </summary>
        public IObservable asObservable() => _link;

        /// <summary>
        /// Implicit conversion to the underlying object.
        /// </summary>
        public static implicit operator T(Handle<T> handle) => handle.currentLink();

        /// <summary>
        /// Registers with this handle as an observable.
        /// </summary>
        public void registerWith(IObserver observer) => _link.RegisterWith(observer);

        /// <summary>
        /// Unregisters with this handle as an observable.
        /// </summary>
        public void unregisterWith(IObserver observer) => _link.UnregisterWith(observer);
        #endregion

        #region Modifiers
        /// <summary>
        /// Links this handle to a new object.
        /// </summary>
        /// <param name="h">The new object to link to.</param>
        /// <param name="registerAsObserver">Whether to register as an observer of the new object.</param>
        public virtual void linkTo(T? h, bool registerAsObserver = true)
        {
            this._link.linkTo(h, registerAsObserver);
        }

        /// <summary>
        /// Resets the handle, making it point to null.
        /// </summary>
        public void reset()
        {
            this._link.linkTo(null, true);
        }
        #endregion

        #region Relinkable handle implementation
        /// <summary>
        /// Internal implementation of the shared pointer with observer behavior.
        /// </summary>
        private class Link : IObserver, IObservable
        {
            private T? _currentLink;
            private bool _isObserver;
            private readonly Observable _observable = new Observable();

            public Link(T? h, bool registerAsObserver)
            {
                linkTo(h, registerAsObserver);
            }

            public void linkTo(T? h, bool registerAsObserver)
            {
                if (!ReferenceEquals(h, _currentLink) || _isObserver != registerAsObserver)
                {
                    if (_currentLink != null && _isObserver)
                    {
                        _currentLink.UnregisterWith(this);
                    }

                    _currentLink = h;
                    _isObserver = registerAsObserver;

                    if (_currentLink != null && _isObserver)
                    {
                        _currentLink.RegisterWith(this);
                    }

                    _observable.NotifyObservers();
                }
            }

            public bool empty() => _currentLink == null;
            public T? currentLink() => _currentLink;
            public void update() => _observable.NotifyObservers();

            // For C++ compatibility
            public void Update() => update();

            // IObservable implementation
            public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
            public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        }
        #endregion
    }

    /// <summary>
    /// Helper class for creating handles.
    /// </summary>
    public static class HandleUtils
    {
        /// <summary>
        /// Creates a handle to the given object.
        /// </summary>
        public static Handle<T> MakeHandle<T>(T obj, bool registerAsObserver = true) where T : class, IObservable
        {
            return new Handle<T>(obj, registerAsObserver);
        }

        /// <summary>
        /// Creates an empty handle.
        /// </summary>
        public static Handle<T> EmptyHandle<T>() where T : class, IObservable
        {
            return new Handle<T>();
        }
    }

    /// <summary>
    /// Relinkable handle which only retakes an observer relationship when the referred object changes.
    /// </summary>
    /// <typeparam name="T">The type of observable object this handle points to.</typeparam>
    /// <remarks>
    /// This is the same as a regular Handle, but it only notifies observers when the pointer changes,
    /// not when the pointed-to object changes.
    /// </remarks>
    public class RelinkableHandle<T> : Handle<T> where T : class, IObservable
    {
        public RelinkableHandle() : base() { }
        public RelinkableHandle(T? h, bool registerAsObserver = true) : base(h, registerAsObserver) { }

        /// <summary>
        /// Relinkable-handle assignment operator
        /// </summary>
        public new void linkTo(T? h, bool registerAsObserver = false)
        {
            base.linkTo(h, registerAsObserver);
        }
    }
}