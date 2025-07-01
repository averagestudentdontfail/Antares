// Curiouslyrecurring.cs

namespace Antares.Pattern
{
    /// <summary>
    /// Support for the Curiously Recurring Template Pattern (CRTP).
    /// </summary>
    /// <remarks>
    /// This is a C# implementation of the pattern described by James O. Coplien
    /// in "C++ Gems" (1996). It allows a base class to access members of its
    /// derived class without using virtual methods, enabling a form of static polymorphism.
    /// <para>
    /// A derived class `MyImpl` would be defined as `class MyImpl : CuriouslyRecurringTemplate<MyImpl>`.
    /// </para>
    /// </remarks>
    /// <typeparam name="TImpl">
    /// The derived class that is inheriting from this base class.
    /// </typeparam>
    public abstract class CuriouslyRecurringTemplate<TImpl>
        where TImpl : CuriouslyRecurringTemplate<TImpl>
    {
        /// <summary>
        /// Protected constructor to ensure this class is only used as a base class.
        /// </summary>
        protected CuriouslyRecurringTemplate() { }

        /// <summary>
        /// Gets a reference to the derived class instance.
        /// This provides access to the concrete implementation from the base class.
        /// </summary>
        protected TImpl Impl => (TImpl)this;
    }
}