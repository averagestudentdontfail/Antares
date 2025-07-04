// Handle.cs

using System;

namespace Antares
{
    /// <summary>
    /// Shared handle to an observable object.
    /// </summary>
    /// <remarks>
    /// All copies of an instance of this class refer to the same observable link.
    /// When a RelinkableHandle is relinked to another observable, the change
    /// is propagated to all copies.
    /// The generic type T must be a class that implements IObservable.
    /// </remarks>
    /// <typeparam name="T">The type of the underlying observable object.</typeparam>
    public class Handle<T> where T : class, IObservable
    {
        /// <summary>
        /// The shared link that all copies of a Handle point to.
        /// It is both an observer of the underlying object and an observable to its own clients.
        /// </summary>
        protected class Link : IObservable, IObserver
        {
            private readonly Observable _observable = new Observable();
            private T _currentLink;
            private bool _isObserver;

            public Link(T h, bool registerAsObserver)
            {
                LinkTo(h, registerAsObserver);
            }

            public void LinkTo(T h, bool registerAsObserver)
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

            public bool IsEmpty => _currentLink == null;
            public T CurrentLinkObject => _currentLink;
            public void Update() => _observable.NotifyObservers();
            public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
            public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        }

        protected readonly Link _link;

        #region Constructors
        /// <summary>
        /// Creates an empty handle.
        /// </summary>
        public Handle() : this(null, true) { }

        /// <summary>
        /// Creates a handle pointing to the given object.
        /// </summary>
        /// <param name="p">The object to point to.</param>
        /// <param name="registerAsObserver">
        /// If true, the handle registers as an observer of the underlying object.
        /// Set this to false only in controlled environments to avoid receiving
        /// notifications from the underlying object, for example to break circular dependencies.
        /// </param>
        public Handle(T p, bool registerAsObserver = true)
        {
            _link = new Link(p, registerAsObserver);
        }
        #endregion

        #region Inspectors
        /// <summary>
        /// Returns the underlying object.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the handle is empty.</exception>
        public T Value
        {
            get
            {
                if (IsEmpty)
                    throw new InvalidOperationException("Empty Handle cannot be dereferenced");
                return _link.CurrentLinkObject;
            }
        }

        /// <summary>
        /// Checks if the contained pointer is null.
        /// </summary>
        public bool IsEmpty => _link.IsEmpty;

        /// <summary>
        /// Allows the handle to be used as an observable.
        /// </summary>
        public IObservable AsObservable => _link;
        #endregion

        #region Operators
        public static bool operator ==(Handle<T> h1, Handle<T> h2)
        {
            if (h1 is null) return h2 is null;
            return h1.Equals(h2);
        }

        public static bool operator !=(Handle<T> h1, Handle<T> h2) => !(h1 == h2);

        public override bool Equals(object? o)
        {
            if (o is not Handle<T> other) return false;
            return ReferenceEquals(this._link, other._link);
        }

        public override int GetHashCode() => _link.GetHashCode();
        #endregion
    }

    /// <summary>
    /// Relinkable handle to an observable object.
    /// </summary>
    /// <remarks>
    /// An instance of this class can be relinked so that it points to another observable.
    /// The change will be propagated to all handles that were created as copies of this instance.
    /// </remarks>
    /// <typeparam name="T">The type of the underlying observable object.</typeparam>
    public class RelinkableHandle<T> : Handle<T> where T : class, IObservable
    {
        #region Constructors
        /// <summary>
        /// Creates an empty handle.
        /// </summary>
        public RelinkableHandle() : base(null, true) { }

        /// <summary>
        /// Creates a handle pointing to the given object.
        /// </summary>
        public RelinkableHandle(T p, bool registerAsObserver = true) : base(p, registerAsObserver) { }
        #endregion

        /// <summary>
        /// Links this handle to a new object.
        /// </summary>
        /// <param name="h">The new object to link to.</param>
        /// <param name="registerAsObserver">Whether to register as an observer of the new object.</param>
        public void LinkTo(T h, bool registerAsObserver = true)
        {
            this._link.LinkTo(h, registerAsObserver);
        }

        /// <summary>
        /// Resets the handle, making it point to null.
        /// </summary>
        public void Reset()
        {
            this._link.LinkTo(null, true);
        }
    }
}