// Pricingengine.cs

using Antares.Pattern;

namespace Antares
{
    /// <summary>
    /// Interface for pricing engines.
    /// </summary>
    public interface IPricingEngine : IObservable
    {
        /// <summary>
        /// Interface for pricing engine arguments.
        /// </summary>
        interface IArguments
        {
            /// <summary>
            /// Validates the arguments before calculation.
            /// </summary>
            void Validate();
        }

        /// <summary>
        /// Interface for pricing engine results.
        /// </summary>
        interface IResults
        {
            /// <summary>
            /// Resets the results to a default state.
            /// </summary>
            void Reset();
        }

        /// <summary>
        /// Gets the arguments object for this engine.
        /// </summary>
        IArguments GetArguments();

        /// <summary>
        /// Gets the results object for this engine.
        /// </summary>
        IResults GetResults();

        /// <summary>
        /// Resets the engine, typically by resetting its results.
        /// </summary>
        void Reset();

        /// <summary>
        /// Performs the actual calculation.
        /// </summary>
        void Calculate();
    }

    /// <summary>
    /// Template base class for pricing engines.
    /// Derived engines only need to implement the Calculate() method.
    /// </summary>
    /// <typeparam name="TArguments">The type of the arguments class.</typeparam>
    /// <typeparam name="TResults">The type of the results class.</typeparam>
    public abstract class GenericEngine<TArguments, TResults> : IPricingEngine, IObserver
        where TArguments : IPricingEngine.IArguments, new()
        where TResults : IPricingEngine.IResults, new()
    {
        private readonly Observable _observable = new Observable();

        protected TArguments _arguments = new TArguments();
        protected TResults _results = new TResults();

        public IPricingEngine.IArguments GetArguments() => _arguments;

        public IPricingEngine.IResults GetResults() => _results;

        public void Reset()
        {
            _results.Reset();
        }

        public abstract void Calculate();

        // IObserver implementation
        public virtual void Update()
        {
            // When an observer of this engine is notified, it means one of
            // its inputs has changed. The engine passes the notification on
            // to its own observers (typically the instrument).
            NotifyObservers();
        }

        // IObservable implementation
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
    }
}