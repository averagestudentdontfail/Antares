// C# code for FdmEscrowedLogInnerValueCalculator.cs

using System;

namespace Antares.Method.Utility
{
    /// <summary>
    /// Interface for payoff calculations
    /// </summary>
    public interface IPayoff
    {
        /// <summary>
        /// Calculates the payoff for a given underlying price
        /// </summary>
        /// <param name="price">The underlying price</param>
        /// <returns>The payoff value</returns>
        Real Value(Real price);
    }

    /// <summary>
    /// Interface for finite difference meshers
    /// </summary>
    public interface IFdmMesher
    {
        /// <summary>
        /// Gets the location at the given iterator position and direction
        /// </summary>
        /// <param name="iter">The iterator</param>
        /// <param name="direction">The direction</param>
        /// <returns>The location value</returns>
        Real Location(IFdmLinearOpIterator iter, Size direction);
    }

    /// <summary>
    /// Interface for finite difference linear operator iterators
    /// </summary>
    public interface IFdmLinearOpIterator
    {
        // Marker interface for FDM linear operator iterators
    }

    /// <summary>
    /// Abstract base class for finite difference inner value calculators
    /// </summary>
    public abstract class FdmInnerValueCalculator
    {
        /// <summary>
        /// Calculates the inner value at the given iterator position and time
        /// </summary>
        /// <param name="iter">The iterator position</param>
        /// <param name="t">The time</param>
        /// <returns>The inner value</returns>
        public abstract Real InnerValue(IFdmLinearOpIterator iter, Time t);

        /// <summary>
        /// Calculates the average inner value at the given iterator position and time
        /// </summary>
        /// <param name="iter">The iterator position</param>
        /// <param name="t">The time</param>
        /// <returns>The average inner value</returns>
        public abstract Real AvgInnerValue(IFdmLinearOpIterator iter, Time t);
    }

    /// <summary>
    /// Inner value calculator for escrowed dividend model using log-normal approach
    /// </summary>
    public class FdmEscrowedLogInnerValueCalculator : FdmInnerValueCalculator
    {
        private readonly EscrowedDividendAdjustment _escrowedDividendAdj;
        private readonly IPayoff _payoff;
        private readonly IFdmMesher _mesher;
        private readonly Size _direction;

        /// <summary>
        /// Constructs an escrowed log inner value calculator
        /// </summary>
        /// <param name="escrowedDividendAdj">The escrowed dividend adjustment calculator</param>
        /// <param name="payoff">The payoff function</param>
        /// <param name="mesher">The finite difference mesher</param>
        /// <param name="direction">The spatial direction for the underlying asset</param>
        public FdmEscrowedLogInnerValueCalculator(
            EscrowedDividendAdjustment escrowedDividendAdj,
            IPayoff payoff,
            IFdmMesher mesher,
            Size direction)
        {
            _escrowedDividendAdj = escrowedDividendAdj ?? throw new ArgumentNullException(nameof(escrowedDividendAdj));
            _payoff = payoff ?? throw new ArgumentNullException(nameof(payoff));
            _mesher = mesher ?? throw new ArgumentNullException(nameof(mesher));
            _direction = direction;
        }

        /// <summary>
        /// Calculates the inner value at the given iterator position and time
        /// </summary>
        /// <param name="iter">The iterator position</param>
        /// <param name="t">The time</param>
        /// <returns>The inner value</returns>
        public override Real InnerValue(IFdmLinearOpIterator iter, Time t)
        {
            // Get the log-space location and convert to real space
            Real s_t = Math.Exp(_mesher.Location(iter, _direction));
            
            // Adjust the spot price by subtracting the dividend adjustment
            Real spot = s_t - _escrowedDividendAdj.DividendAdjustment(t);

            // Apply the payoff to get the final value
            return _payoff.Value(spot);
        }

        /// <summary>
        /// Calculates the average inner value at the given iterator position and time
        /// </summary>
        /// <param name="iter">The iterator position</param>
        /// <param name="t">The time</param>
        /// <returns>The average inner value (same as inner value for this implementation)</returns>
        public override Real AvgInnerValue(IFdmLinearOpIterator iter, Time t)
        {
            return InnerValue(iter, t);
        }
    }
}