// Userconfig.cs

namespace Antares
{
    /// <summary>
    /// User configuration section.
    /// Modify the values in this class to suit your preferences.
    /// </summary>
    public static class Settings
    {
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
        public static readonly bool FasterLazyObjects = true;
    }
}