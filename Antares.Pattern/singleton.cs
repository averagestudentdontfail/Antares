// Singleton.cs

using System;
using System.Threading;

namespace Antares.Pattern
{
    /// <summary>
    /// Base class for creating a thread-safe, application-wide singleton.
    /// This corresponds to the C++ Singleton&lt;T, std::true_type&gt; or the default
    /// behavior when sessions are disabled.
    /// </summary>
    /// <remarks>
    /// The typical use of this class is:
    ///
    ///     class MyGlobalService : Singleton&lt;MyGlobalService&gt;
    ///     {
    ///         // A private or protected constructor is recommended.
    ///         private MyGlobalService() { }
    ///
    ///         public void DoWork() { /* ... */ }
    ///     }
    ///
    /// Usage:
    ///     MyGlobalService.Instance.DoWork();
    ///
    /// This implementation uses System.Lazy to ensure thread-safe, lazy initialization.
    /// It uses reflection to call a non-public constructor, mimicking the C++ friend pattern.
    /// </remarks>
    /// <typeparam name="T">The type of the singleton class itself.</typeparam>
    public abstract class Singleton<T> where T : class
    {
        private static readonly Lazy<T> lazyInstance =
            new Lazy<T>(() => (T)Activator.CreateInstance(typeof(T), true)!,
                        LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Provides access to the unique, global instance of the class.
        /// </summary>
        public static T Instance
        {
            get
            {
                var instance = lazyInstance.Value;
                if (instance == null)
                {
                    throw new InvalidOperationException($"Failed to create an instance of {typeof(T).FullName}.");
                }
                return instance;
            }
        }

        /// <summary>
        /// Protected constructor to prevent direct instantiation.
        /// </summary>
        protected Singleton() { }
    }

    /// <summary>
    /// Base class for creating a per-thread (session) singleton.
    /// This corresponds to the C++ Singleton&lt;T&gt; (with the default Global = false)
    /// when QL_ENABLE_SESSIONS is active.
    /// </summary>
    /// <remarks>
    /// Each thread accessing the Instance property will receive its own unique
    /// instance of the class.
    ///
    ///     class MySessionData : SessionSingleton&lt;MySessionData&gt;
    ///     {
    ///         private MySessionData() { }
    ///         public int ThreadSpecificValue { get; set; }
    ///     }
    ///
    /// Usage:
    ///     MySessionData.Instance.ThreadSpecificValue = 123; // This value is local to the current thread.
    ///
    /// This implementation uses System.Threading.ThreadLocal to ensure per-thread
    /// lazy initialization.
    /// </remarks>
    /// <typeparam name="T">The type of the singleton class itself.</typeparam>
    public abstract class SessionSingleton<T> where T : class
    {
        private static readonly ThreadLocal<T> sessionInstance =
            new ThreadLocal<T>(() => (T)Activator.CreateInstance(typeof(T), true)!);

        /// <summary>
        /// Provides access to the unique, per-thread instance of the class.
        /// </summary>
        public static T Instance
        {
            get
            {
                var instance = sessionInstance.Value;
                if (instance == null)
                {
                    throw new InvalidOperationException($"Failed to create an instance of {typeof(T).FullName} for the current thread.");
                }
                return instance;
            }
        }

        /// <summary>
        /// Protected constructor to prevent direct instantiation.
        /// </summary>
        protected SessionSingleton() { }

        /// <summary>
        /// Disposes the ThreadLocal instance when the SessionSingleton is disposed.
        /// </summary>
        static SessionSingleton()
        {
            AppDomain.CurrentDomain.DomainUnload += (sender, e) => sessionInstance.Dispose();
        }
    }
}