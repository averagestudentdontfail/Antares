// Visitor.cs

namespace Antares.Pattern
{
    /// <summary>
    /// A degenerate base interface for the Acyclic Visitor pattern.
    /// This serves as a common marker for all visitor interfaces.
    /// </summary>
    public interface IAcyclicVisitor
    {
        // This interface is intentionally empty.
    }

    /// <summary>
    /// A generic visitor interface for a specific class in the Acyclic Visitor pattern.
    /// An object implementing this interface can visit objects of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of the object to be visited.</typeparam>
    public interface IVisitor<in T> : IAcyclicVisitor
    {
        /// <summary>
        /// Performs the visit operation on the given element.
        /// </summary>
        /// <param name="element">The element to visit.</param>
        void Visit(T element);
    }
}