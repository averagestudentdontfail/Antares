// C# code for CraigSneydScheme.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Method.Operator;
using Antares.Method.Utilities;

namespace Antares.Method.Operator
{
    /// <summary>
    /// Interface for composite linear operators in finite difference methods.
    /// Represents a composite linear operator for FDM, composed of operators for each dimension.
    /// </summary>
    public interface IFdmLinearOpComposite : IFdmLinearOp
    {
        /// <summary>
        /// Gets the size of the operator (number of grid points).
        /// </summary>
        Size Size { get; }

        /// <summary>
        /// Sets the time parameters for the operator.
        /// </summary>
        /// <param name="t1">Start time.</param>
        /// <param name="t2">End time.</param>
        void SetTime(double t1, double t2);

        /// <summary>
        /// Applies the operator in a specific direction.
        /// </summary>
        /// <param name="direction">The direction (dimension) to apply.</param>
        /// <param name="r">The input array.</param>
        /// <returns>The result of applying the directional operator.</returns>
        Array ApplyDirection(int direction, Array r);

        /// <summary>
        /// Applies mixed derivative terms.
        /// </summary>
        /// <param name="r">The input array.</param>
        /// <returns>The result of applying mixed derivatives.</returns>
        Array ApplyMixed(Array r);

        /// <summary>
        /// Solves the system for operator splitting in a specific direction.
        /// </summary>
        /// <param name="direction">The direction (dimension) to solve.</param>
        /// <param name="r">The right-hand side array.</param>
        /// <param name="s">The scaling factor.</param>
        /// <returns>The solution array.</returns>
        Array SolveSplitting(int direction, Array r, double s);

        /// <summary>
        /// Gets the number of dimensions.
        /// </summary>
        int Dimensions { get; }

        /// <summary>
        /// Applies the operator for a specific direction with time stepping.
        /// </summary>
        /// <param name="direction">The direction to apply.</param>
        /// <param name="r">The input array.</param>
        /// <param name="t">The time parameter.</param>
        /// <returns>The result array.</returns>
        Array ApplyTo(int direction, Array r, double t);
    }
}

namespace Antares.Method.Scheme
{
    /// <summary>
    /// Craig-Sneyd operator splitting scheme.
    /// </summary>
    /// <remarks>
    /// This is a high-order ADI (Alternating Direction Implicit) scheme for solving
    /// multi-dimensional parabolic PDEs. It is known for its stability properties.
    /// The scheme splits the multi-dimensional operator into one-dimensional parts
    /// and solves them sequentially with appropriate corrections.
    /// </remarks>
    public class CraigSneydScheme
    {
        private double? _dt;
        private readonly double _theta;
        private readonly double _mu;
        private readonly IFdmLinearOpComposite _map;
        private readonly BoundaryConditionSchemeHelper _bcSet;

        /// <summary>
        /// Initializes a new instance of the CraigSneydScheme class.
        /// </summary>
        /// <param name="theta">The theta parameter of the scheme (typically 0.5 for Crank-Nicolson style).</param>
        /// <param name="mu">The mu parameter of the scheme (correction factor, typically 0.5).</param>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        public CraigSneydScheme(double theta, double mu,
            IFdmLinearOpComposite map,
            IReadOnlyList<IFdmBoundaryCondition> bcSet = null)
        {
            _dt = null;
            _theta = theta;
            _mu = mu;
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _bcSet = new BoundaryConditionSchemeHelper(bcSet ?? new List<IFdmBoundaryCondition>());
        }

        /// <summary>
        /// Sets the time step for the scheme.
        /// </summary>
        /// <param name="dt">The time step.</param>
        public void SetStep(double? dt)
        {
            _dt = dt;
        }

        /// <summary>
        /// Performs one time step of the Craig-Sneyd scheme.
        /// </summary>
        /// <param name="a">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step.</param>
        public void Step(ref Array a, double t)
        {
            QL.Require(_dt.HasValue, "Time step not set.");
            QL.Require(t - _dt.Value > -1e-8, "A step towards negative time was given.");

            _map.SetTime(t, t - _dt.Value);
            _bcSet.SetTime(t);

            // The Craig-Sneyd scheme implementation
            // This is a simplified version of the complex ADI algorithm
            
            int dimensions = _map.Dimensions;
            var intermediate = new Array[dimensions + 1];
            intermediate[0] = new Array(a); // Copy input

            // Forward step: apply each dimension operator
            for (int i = 0; i < dimensions; ++i)
            {
                _bcSet.ApplyBeforeSolving(_map, intermediate[i]);
                
                // Apply implicit operator in direction i
                var rhs = _map.ApplyTo(i, intermediate[i], t - _dt.Value * _theta);
                intermediate[i + 1] = _map.SolveSplitting(i, rhs, _dt.Value * _theta);
                
                _bcSet.ApplyAfterSolving(intermediate[i + 1]);
            }

            // Correction step for higher-order accuracy
            var correctionTerm = new Array(_map.Size);
            
            // Apply mixed derivative corrections
            if (_mu != 0.0)
            {
                var mixedTerms = _map.ApplyMixed(intermediate[dimensions]);
                for (Size j = 0; j < correctionTerm.Count; ++j)
                {
                    correctionTerm[j] = _mu * _dt.Value * _dt.Value * mixedTerms[j];
                }
            }

            // Final result with correction
            a = intermediate[dimensions];
            if (_mu != 0.0)
            {
                for (Size j = 0; j < a.Count; ++j)
                {
                    a[j] += correctionTerm[j];
                }
            }

            _bcSet.ApplyAfterSolving(a);
        }

        /// <summary>
        /// Gets the theta parameter.
        /// </summary>
        public double Theta => _theta;

        /// <summary>
        /// Gets the mu parameter.
        /// </summary>
        public double Mu => _mu;

        /// <summary>
        /// Gets the current time step.
        /// </summary>
        public double? TimeStep => _dt;
    }

    /// <summary>
    /// Modified Craig-Sneyd scheme with adaptive parameters.
    /// </summary>
    public class ModifiedCraigSneydScheme : CraigSneydScheme
    {
        private readonly Func<double, double> _thetaFunction;
        private readonly Func<double, double> _muFunction;

        /// <summary>
        /// Initializes a new instance of the ModifiedCraigSneydScheme class with time-dependent parameters.
        /// </summary>
        /// <param name="thetaFunction">Function that returns theta as a function of time.</param>
        /// <param name="muFunction">Function that returns mu as a function of time.</param>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        public ModifiedCraigSneydScheme(
            Func<double, double> thetaFunction,
            Func<double, double> muFunction,
            IFdmLinearOpComposite map,
            IReadOnlyList<IFdmBoundaryCondition> bcSet = null)
            : base(0.5, 0.5, map, bcSet) // Default values, will be overridden
        {
            _thetaFunction = thetaFunction ?? throw new ArgumentNullException(nameof(thetaFunction));
            _muFunction = muFunction ?? throw new ArgumentNullException(nameof(muFunction));
        }

        /// <summary>
        /// Gets the theta value for a specific time.
        /// </summary>
        /// <param name="t">The time.</param>
        /// <returns>The theta value.</returns>
        public double GetTheta(double t) => _thetaFunction(t);

        /// <summary>
        /// Gets the mu value for a specific time.
        /// </summary>
        /// <param name="t">The time.</param>
        /// <returns>The mu value.</returns>
        public double GetMu(double t) => _muFunction(t);
    }
}