// Settings.cs

using System;
using Antares.Time;
using Antares.Utility;

namespace Antares
{
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
            /// Implicit conversion to Date.
            /// </summary>
            public static implicit operator Date(DateProxy proxy) => proxy.Value;

            /// <summary>
            /// Returns the evaluation date; if a null date is specified, today's date is used.
            /// </summary>
            public Date EvaluationDate
            {
                get
                {
                    Date date = Value;
                    return date ?? Date.Today;
                }
            }
        }

        private static readonly DateProxy _evaluationDate = new DateProxy();
        private static readonly ObservableValue<bool> _includeReferenceDateEvents = new ObservableValue<bool>(false);
        private static readonly ObservableValue<bool?> _includeTodaysCashFlows = new ObservableValue<bool?>(null);
        private static readonly ObservableValue<bool> _enforcesTodaysHistoricFixings = new ObservableValue<bool>(false);

        /// <summary>
        /// Gets or sets the global evaluation date.
        /// </summary>
        /// <remarks>
        /// This is the date at which instruments are valued, curves are built, etc.
        /// Setting this will notify all observers.
        /// </remarks>
        public static Date EvaluationDate
        {
            get => _evaluationDate.EvaluationDate;
            set => _evaluationDate.Value = value;
        }

        /// <summary>
        /// Gets the evaluation date proxy for advanced usage scenarios where observer registration is needed.
        /// </summary>
        public static DateProxy EvaluationDateProxy => _evaluationDate;

        /// <summary>
        /// Gets or sets whether events occurring on the reference date should be considered as having already occurred.
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