// Event.cs

using System;

namespace Antares
{
    /// <summary>
    /// Base interface for events associated with a given date.
    /// </summary>
    public interface IEvent : IObservable
    {
        /// <summary>
        /// Returns the date at which the event occurs.
        /// </summary>
        Date Date { get; }

        /// <summary>
        /// Returns true if an event has already occurred before a given reference date.
        /// </summary>
        /// <param name="refDate">The reference date. If null, the global evaluation date is used.</param>
        /// <param name="includeRefDate">
        /// Specifies whether an event occurring on the reference date has occurred.
        /// If null, the global setting is used.
        /// If true, an event on the reference date has NOT occurred.
        /// If false, an event on the reference date HAS occurred.
        /// </param>
        bool HasOccurred(Date? refDate = null, bool? includeRefDate = null);

        /// <summary>
        /// Accepts a visitor.
        /// </summary>
        void Accept(IAcyclicVisitor v);
    }

    /// <summary>
    /// Abstract base class for event implementations.
    /// </summary>
    public abstract class Event : IEvent
    {
        private readonly Observable _observable = new Observable();

        /// <summary>
        /// Returns the date at which the event occurs.
        /// </summary>
        public abstract Date Date { get; }

        /// <summary>
        /// Returns true if an event has already occurred before a given reference date.
        /// </summary>
        /// <param name="refDate">The reference date. If null, the global evaluation date is used.</param>
        /// <param name="includeRefDate">
        /// Specifies whether an event occurring on the reference date has occurred.
        /// If null, the global setting is used.
        /// If true, an event on the reference date has NOT occurred (returns false).
        /// If false, an event on the reference date HAS occurred (returns true).
        /// </param>
        public virtual bool HasOccurred(Date? refDate = null, bool? includeRefDate = null)
        {
            Date resolvedRefDate = refDate ?? Settings.EvaluationDate;
            bool include = includeRefDate ?? Settings.IncludeReferenceDateEvents;

            // The logic here seems reversed but is correct according to the C++ comments:
            // "includeRefDate is true" => "event on refDate has NOT occurred" => use <
            // "includeRefDate is false" => "event on refDate HAS occurred" => use <=
            if (include)
                return this.Date < resolvedRefDate;
            else
                return this.Date <= resolvedRefDate;
        }

        /// <summary>
        /// Accepts a visitor.
        /// </summary>
        public virtual void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<IEvent> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                throw new ArgumentException("The provided object is not a valid event visitor.", nameof(v));
            }
        }

        // IObservable implementation
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
    }

    /// <summary>
    /// A simple concrete event implementation.
    /// Corresponds to QuantLib::detail::simple_event.
    /// </summary>
    public sealed class SimpleEvent : Event
    {
        private readonly Date _date;

        public SimpleEvent(Date date)
        {
            _date = date ?? throw new ArgumentNullException(nameof(date));
        }

        public override Date Date => _date;
    }
}