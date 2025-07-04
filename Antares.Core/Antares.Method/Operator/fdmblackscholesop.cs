// C# code for FdmBlackScholesOp.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Math.Matrix;
using Antares.Method.Utility;
using Antares.Time;
using Antares.Process;
using Antares.Term;

namespace Antares.Method.Operator
{
    /// <summary>
    /// Finite difference Black-Scholes linear operator.
    /// Implements the spatial discretization of the Black-Scholes partial differential equation
    /// for use in finite difference pricing methods.
    /// </summary>
    /// <remarks>
    /// The Black-Scholes PDE in log-coordinates is:
    /// ∂V/∂t + (r-q-σ²/2)∂V/∂x + σ²/2 ∂²V/∂x² - rV = 0
    /// 
    /// This operator discretizes the spatial derivatives for numerical solution.
    /// Supports both constant volatility and local volatility models, with optional
    /// quanto adjustments for multi-currency derivatives.
    /// </remarks>
    public class FdmBlackScholesOp : FdmLinearOpComposite
    {
        private readonly FdmMesher _mesher;
        private readonly YieldTermStructure _rTS;
        private readonly YieldTermStructure _qTS;
        private readonly BlackVolTermStructure _volTS;
        private readonly LocalVolTermStructure _localVol;
        private readonly Array _x;
        private readonly FirstDerivativeOp _dxMap;
        private readonly TripleBandLinearOp _dxxMap;
        private readonly TripleBandLinearOp _mapT;
        private readonly Real _strike;
        private readonly Real _illegalLocalVolOverwrite;
        private readonly Size _direction;
        private readonly FdmQuantoHelper _quantoHelper;

        /// <summary>
        /// Initializes a new instance of the FdmBlackScholesOp class.
        /// </summary>
        /// <param name="mesher">The finite difference mesher defining the spatial grid.</param>
        /// <param name="bsProcess">The generalized Black-Scholes process containing market data.</param>
        /// <param name="strike">The strike price for volatility calculations.</param>
        /// <param name="localVol">Whether to use local volatility model.</param>
        /// <param name="illegalLocalVolOverwrite">Fallback volatility for numerical issues (negative means no fallback).</param>
        /// <param name="direction">The spatial direction for this operator (default 0 for 1D).</param>
        /// <param name="quantoHelper">Optional quanto helper for currency adjustments.</param>
        public FdmBlackScholesOp(FdmMesher mesher,
                                GeneralizedBlackScholesProcess bsProcess,
                                Real strike,
                                bool localVol = false,
                                Real illegalLocalVolOverwrite = double.NaN,
                                Size direction = 0,
                                FdmQuantoHelper quantoHelper = null)
        {
            _mesher = mesher ?? throw new ArgumentNullException(nameof(mesher));
            if (bsProcess == null) throw new ArgumentNullException(nameof(bsProcess));

            _rTS = bsProcess.riskFreeRate().link;
            _qTS = bsProcess.dividendYield().link;
            _volTS = bsProcess.blackVolatility().link;
            _localVol = localVol ? bsProcess.localVolatility().link : null;
            
            // Initialize locations in price space for local volatility
            _x = localVol ? Array.Exp(_mesher.Locations(direction)) : new Array();
            
            _dxMap = new FirstDerivativeOp(direction, mesher);
            _dxxMap = new SecondDerivativeOp(direction, mesher);
            _mapT = new TripleBandLinearOp(direction, mesher);
            _strike = strike;
            _illegalLocalVolOverwrite = double.IsNaN(illegalLocalVolOverwrite) ? -1.0 : illegalLocalVolOverwrite;
            _direction = direction;
            _quantoHelper = quantoHelper;
        }

        /// <summary>
        /// Gets the number of factors in this operator (always 1 for Black-Scholes).
        /// </summary>
        public override Size Size => 1;

        /// <summary>
        /// Updates the operator coefficients for the specified time interval.
        /// </summary>
        /// <param name="t1">Start time of the interval.</param>
        /// <param name="t2">End time of the interval.</param>
        public override void SetTime(Time t1, Time t2)
        {
            Rate r = _rTS.forwardRate(t1, t2, Compounding.Continuous).rate();
            Rate q = _qTS.forwardRate(t1, t2, Compounding.Continuous).rate();

            if (_localVol != null)
            {
                // Local volatility case - calculate volatility at each grid point
                var v = new Array(_mesher.Layout.Size);
                
                foreach (var iter in _mesher.Layout)
                {
                    Size i = iter.Index;
                    
                    if (_illegalLocalVolOverwrite < 0.0)
                    {
                        // No fallback - use local volatility directly
                        Real localVol = _localVol.localVol(0.5 * (t1 + t2), _x[(int)i], true);
                        v[(int)i] = localVol * localVol; // squared volatility
                    }
                    else
                    {
                        // Use fallback for numerical issues
                        try
                        {
                            Real localVol = _localVol.localVol(0.5 * (t1 + t2), _x[(int)i], true);
                            v[(int)i] = localVol * localVol;
                        }
                        catch (Exception)
                        {
                            v[(int)i] = _illegalLocalVolOverwrite * _illegalLocalVolOverwrite;
                        }
                    }
                }

                // Set up the operator: ∂V/∂t + (r-q-σ²/2)∂V/∂x + σ²/2 ∂²V/∂x² - rV = 0
                if (_quantoHelper != null)
                {
                    // Include quanto adjustment
                    Array drift = (r - q - 0.5 * v) - _quantoHelper.QuantoAdjustment(Array.Sqrt(v), t1, t2);
                    Array diffusion = 0.5 * v;
                    Array discount = new Array(1, -r);
                    
                    _mapT.Axpyb(drift, _dxMap, _dxxMap.Mult(diffusion), discount);
                }
                else
                {
                    // Standard Black-Scholes operator
                    Array drift = r - q - 0.5 * v;
                    Array diffusion = 0.5 * v;
                    Array discount = new Array(1, -r);
                    
                    _mapT.Axpyb(drift, _dxMap, _dxxMap.Mult(diffusion), discount);
                }
            }
            else
            {
                // Constant volatility case
                Real variance = _volTS.blackForwardVariance(t1, t2, _strike) / (t2 - t1);

                if (_quantoHelper != null)
                {
                    // Include quanto adjustment
                    Array drift = new Array(1, r - q - 0.5 * variance) - 
                                  _quantoHelper.QuantoAdjustment(new Array(1, Math.Sqrt(variance)), t1, t2);
                    Array diffusion = new Array(_mesher.Layout.Size, 0.5 * variance);
                    Array discount = new Array(1, -r);
                    
                    _mapT.Axpyb(drift, _dxMap, _dxxMap.Mult(diffusion), discount);
                }
                else
                {
                    // Standard Black-Scholes operator
                    Array drift = new Array(1, r - q - 0.5 * variance);
                    Array diffusion = new Array(_mesher.Layout.Size, 0.5 * variance);
                    Array discount = new Array(1, -r);
                    
                    _mapT.Axpyb(drift, _dxMap, _dxxMap.Mult(diffusion), discount);
                }
            }
        }

        /// <summary>
        /// Applies the operator to a vector.
        /// </summary>
        /// <param name="u">Input vector.</param>
        /// <returns>Result of applying the operator.</returns>
        public override Array Apply(Array u)
        {
            return _mapT.Apply(u);
        }

        /// <summary>
        /// Applies the operator in a specific direction.
        /// </summary>
        /// <param name="direction">The direction to apply.</param>
        /// <param name="r">Input vector.</param>
        /// <returns>Result of applying the operator in the specified direction.</returns>
        public override Array ApplyDirection(Size direction, Array r)
        {
            if (direction == _direction)
                return _mapT.Apply(r);
            else
                return new Array(r.Count, 0.0);
        }

        /// <summary>
        /// Applies mixed derivative terms (none for single-factor Black-Scholes).
        /// </summary>
        /// <param name="r">Input vector.</param>
        /// <returns>Zero vector since there are no mixed derivatives.</returns>
        public override Array ApplyMixed(Array r)
        {
            return new Array(r.Count, 0.0);
        }

        /// <summary>
        /// Solves the splitting step for operator splitting methods.
        /// </summary>
        /// <param name="direction">The direction to solve.</param>
        /// <param name="r">Right-hand side vector.</param>
        /// <param name="dt">Time step size.</param>
        /// <returns>Solution vector.</returns>
        public override Array SolveSplitting(Size direction, Array r, Real dt)
        {
            if (direction == _direction)
                return _mapT.SolveSplitting(r, dt, 1.0);
            else
                return r;
        }

        /// <summary>
        /// Applies preconditioning for iterative solvers.
        /// </summary>
        /// <param name="r">Input vector.</param>
        /// <param name="dt">Time step size.</param>
        /// <returns>Preconditioned vector.</returns>
        public override Array Preconditioner(Array r, Real dt)
        {
            return SolveSplitting(_direction, r, dt);
        }

        /// <summary>
        /// Returns the matrix decomposition of the operator.
        /// </summary>
        /// <returns>Vector containing the matrix representation.</returns>
        public override List<SparseMatrix> ToMatrixDecomp()
        {
            return new List<SparseMatrix> { _mapT.ToMatrix() };
        }
    }
}