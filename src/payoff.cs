// CPayoff.cs

using System;

namespace Antares
{
    /// <summary>
    /// Interface for option payoffs.
    /// </summary>
    public interface IPayoff
    {
        /// <summary>
        /// A name describing the payoff type.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// A more detailed description of the payoff, including its parameters.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Calculates the value of the payoff for a given underlying price.
        /// </summary>
        /// <param name="price">The price of the underlying asset.</param>
        /// <returns>The calculated payoff value.</returns>
        Real GetValue(Real price);

        /// <summary>
        /// Accepts a visitor.
        /// </summary>
        /// <param name="v">The visitor to accept.</param>
        void Accept(IAcyclicVisitor v);
    }

    /// <summary>
    /// Abstract base class for option payoffs.
    /// </summary>
    public abstract class Payoff : IPayoff
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
            if (v is IVisitor<IPayoff> payoffVisitor)
            {
                payoffVisitor.Visit(this);
            }
            else if (v is IVisitor<Payoff> concretePayoffVisitor)
            {
                concretePayoffVisitor.Visit(this);
            }
            else
            {
                throw new ArgumentException("The provided object is not a valid payoff visitor.", nameof(v));
            }
        }
        #endregion
    }
}