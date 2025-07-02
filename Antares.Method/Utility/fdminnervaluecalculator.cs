// C# code for FdmInnerValueCalculator.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Math.Integral;

namespace Antares.Method.Utility
{
    // Forward declarations - these classes should be defined in their respective FDM files
    // BasketPayoff would typically extend Payoff to handle basket-style payoffs
    public abstract class BasketPayoff : Payoff
    {
        public abstract Real GetValue(Array values);
    }

    // Helper class for mapped payoff functionality
    internal class MappedPayoff
    {
        private readonly Payoff _payoff;
        private readonly Func<Real, Real> _gridMapping;

        public MappedPayoff(Payoff payoff, Func<Real, Real> gridMapping)
        {
            _payoff = payoff;
            _gridMapping = gridMapping;
        }

        public Real Invoke(Real x) => _payoff.GetValue(_gridMapping(x));
    }

    /// <summary>
    /// Abstract base class for finite difference method inner value calculators.
    /// Provides interface for calculating inner values and averaged inner values.
    /// </summary>
    public abstract class FdmInnerValueCalculator
    {
        /// <summary>
        /// Calculates the inner value at a specific grid point.
        /// </summary>
        public abstract Real InnerValue(FdmLinearOpIterator iter, Time t);

        /// <summary>
        /// Calculates the averaged inner value over a cell.
        /// </summary>
        public abstract Real AvgInnerValue(FdmLinearOpIterator iter, Time t);
    }

    /// <summary>
    /// FDM inner value calculator with cell averaging capabilities.
    /// Calculates inner values and averaged inner values for general payoffs.
    /// </summary>
    public class FdmCellAveragingInnerValue : FdmInnerValueCalculator
    {
        private readonly Payoff _payoff;
        private readonly FdmMesher _mesher;
        private readonly Size _direction;
        private readonly Func<Real, Real> _gridMapping;
        private List<Real> _avgInnerValues;

        /// <summary>
        /// Initializes a new instance of the FdmCellAveragingInnerValue class.
        /// </summary>
        /// <param name="payoff">The payoff to evaluate.</param>
        /// <param name="mesher">The finite difference mesher.</param>
        /// <param name="direction">The direction for averaging.</param>
        /// <param name="gridMapping">Optional grid mapping function (identity by default).</param>
        public FdmCellAveragingInnerValue(Payoff payoff,
                                          FdmMesher mesher,
                                          Size direction,
                                          Func<Real, Real> gridMapping = null)
        {
            _payoff = payoff ?? throw new ArgumentNullException(nameof(payoff));
            _mesher = mesher ?? throw new ArgumentNullException(nameof(mesher));
            _direction = direction;
            _gridMapping = gridMapping ?? (x => x); // Identity function as default
            _avgInnerValues = new List<Real>();
        }

        /// <summary>
        /// Calculates the inner value at a specific grid point.
        /// </summary>
        public override Real InnerValue(FdmLinearOpIterator iter, Time t)
        {
            Real loc = _mesher.Location(iter, _direction);
            return _payoff.GetValue(_gridMapping(loc));
        }

        /// <summary>
        /// Calculates the averaged inner value using caching for efficiency.
        /// </summary>
        public override Real AvgInnerValue(FdmLinearOpIterator iter, Time t)
        {
            if (_avgInnerValues.Count == 0)
            {
                // Calculate caching values
                _avgInnerValues = new List<Real>(new Real[_mesher.Layout.Dim[_direction]]);
                bool[] initialized = new bool[_avgInnerValues.Count];

                foreach (var i in _mesher.Layout)
                {
                    Size xn = (Size)i.Coordinates[_direction];
                    if (!initialized[xn])
                    {
                        initialized[xn] = true;
                        _avgInnerValues[(int)xn] = AvgInnerValueCalc(i, t);
                    }
                }
            }

            return _avgInnerValues[iter.Coordinates[_direction]];
        }

        /// <summary>
        /// Calculates the averaged inner value for a specific cell using Simpson integration.
        /// </summary>
        private Real AvgInnerValueCalc(FdmLinearOpIterator iter, Time t)
        {
            Size coord = (Size)iter.Coordinates[_direction];

            // Use simple inner value for boundary cells
            if (coord == 0 || coord == _mesher.Layout.Dim[_direction] - 1)
                return InnerValue(iter, t);

            Real loc = _mesher.Location(iter, _direction);
            Real a = loc - _mesher.Dminus(iter, _direction) / 2.0;
            Real b = loc + _mesher.Dplus(iter, _direction) / 2.0;

            var f = new MappedPayoff(_payoff, _gridMapping);

            Real retVal;
            try
            {
                Real fa = f.Invoke(a);
                Real fb = f.Invoke(b);
                Real acc = ((fa != 0.0 || fb != 0.0) ? (fa + fb) * 5e-5 : 1e-4);
                
                var simpson = new SimpsonIntegral(acc, 8);
                retVal = simpson.Integrate(f.Invoke, a, b) / (b - a);
            }
            catch (Exception)
            {
                // Use default value if integration fails
                retVal = InnerValue(iter, t);
            }

            return retVal;
        }
    }

    /// <summary>
    /// FDM inner value calculator for log-transformed grids.
    /// Uses exponential mapping to transform from log space to price space.
    /// </summary>
    public class FdmLogInnerValue : FdmCellAveragingInnerValue
    {
        /// <summary>
        /// Initializes a new instance of the FdmLogInnerValue class.
        /// </summary>
        /// <param name="payoff">The payoff to evaluate.</param>
        /// <param name="mesher">The finite difference mesher.</param>
        /// <param name="direction">The direction for averaging.</param>
        public FdmLogInnerValue(Payoff payoff,
                               FdmMesher mesher,
                               Size direction)
            : base(payoff, mesher, direction, x => Math.Exp(x))
        {
        }
    }

    /// <summary>
    /// FDM inner value calculator for basket options in log space.
    /// Handles multi-dimensional payoffs for basket options.
    /// </summary>
    public class FdmLogBasketInnerValue : FdmInnerValueCalculator
    {
        private readonly BasketPayoff _payoff;
        private readonly FdmMesher _mesher;

        /// <summary>
        /// Initializes a new instance of the FdmLogBasketInnerValue class.
        /// </summary>
        /// <param name="payoff">The basket payoff to evaluate.</param>
        /// <param name="mesher">The finite difference mesher.</param>
        public FdmLogBasketInnerValue(BasketPayoff payoff, FdmMesher mesher)
        {
            _payoff = payoff ?? throw new ArgumentNullException(nameof(payoff));
            _mesher = mesher ?? throw new ArgumentNullException(nameof(mesher));
        }

        /// <summary>
        /// Calculates the inner value for a basket option.
        /// Transforms from log space to price space for all dimensions.
        /// </summary>
        public override Real InnerValue(FdmLinearOpIterator iter, Time t)
        {
            var x = new Array(_mesher.Layout.Dim.Length);
            
            for (Size i = 0; i < x.Count; ++i)
            {
                x[(int)i] = Math.Exp(_mesher.Location(iter, i));
            }

            return _payoff.GetValue(x);
        }

        /// <summary>
        /// For basket options, the averaged inner value equals the inner value.
        /// </summary>
        public override Real AvgInnerValue(FdmLinearOpIterator iter, Time t)
        {
            return InnerValue(iter, t);
        }
    }

    /// <summary>
    /// Simple FDM inner value calculator that always returns zero.
    /// Useful for instruments with no intrinsic value or as a null object.
    /// </summary>
    public class FdmZeroInnerValue : FdmInnerValueCalculator
    {
        /// <summary>
        /// Always returns zero.
        /// </summary>
        public override Real InnerValue(FdmLinearOpIterator iter, Time t) => 0.0;

        /// <summary>
        /// Always returns zero.
        /// </summary>
        public override Real AvgInnerValue(FdmLinearOpIterator iter, Time t) => 0.0;
    }
}