// CPayoff.cs

using System;

namespace Antares
{
    #region Visitor Pattern Infrastructure
    /// <summary>
    /// Base marker interface for the Acyclic Visitor pattern.
    /// </summary>
    public interface IAcyclicVisitor { }

    /// <summary>
    /// A generic visitor interface for the Acyclic Visitor pattern.
    /// An object implementing this interface can visit objects of type T.
    /// </summary>
    /// <typeparam name="T">The type of the object to be visited.</typeparam>
    public interface IVisitor<in T> : IAcyclicVisitor
    {
        void Visit(T element);
    }
    #endregion

    /// <summary>
    /// Abstract base class for option payoffs.
    /// </summary>
    public abstract class Payoff
    {
        #region Payoff interface
        /// <summary>
        /// A name describing the payoff type.
        /// </summary>
        /// <remarks>
        /// This method is used for output and comparison between payoffs.
        /// It is not meant to be used for writing switch-on-type code.
        /// </remarks>
        public abstract string Name { get; }

        /// <summary>
        /// A more detailed description of the payoff, including its parameters.
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// Calculates the value of the payoff for a given underlying price.
        /// </summary>
        /// <param name="price">The price of the underlying asset.</param>
        /// <returns>The calculated payoff value.</returns>
        public abstract Real GetValue(Real price);
        #endregion

        #region Visitability
        /// <summary>
        /// Accepts a visitor.
        /// </summary>
        /// <param name="v">The visitor to accept.</param>
        public virtual void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<Payoff> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                throw new ArgumentException("The provided object is not a valid payoff visitor.", nameof(v));
            }
        }
        #endregion
    }
}