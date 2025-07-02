// C# code for MethodOfLinesScheme.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math;
using Antares.Math.Order;
using Antares.Method.Operator;
using Antares.Method.Utilities;

namespace Antares.Method.Scheme
{
    /// <summary>
    /// Method of Lines scheme for finite difference methods.
    /// </summary>
    /// <remarks>
    /// This scheme uses a high-order Runge-Kutta method with an adaptive
    /// step size control to solve the system of ordinary differential
    /// equations (ODEs) that results from discretizing the spatial
    /// derivatives of a PDE.
    /// </remarks>
    public class MethodOfLinesScheme
    {
        private double? _dt;
        private readonly double _eps;
        private readonly double _relInitStepSize;
        private readonly IFdmLinearOpComposite _map;
        private readonly BoundaryConditionSchemeHelper _bcSet;
        private readonly AdaptiveRungeKutta<double> _rungeKutta;

        /// <summary>
        /// Initializes a new instance of the MethodOfLinesScheme class.
        /// </summary>
        /// <param name="eps">Absolute tolerance for the Runge-Kutta solver.</param>
        /// <param name="relInitStepSize">Relative initial step size for the Runge-Kutta solver.</param>
        /// <param name="map">The composite linear operator representing the PDE.</param>
        /// <param name="bcSet">The set of boundary conditions.</param>
        public MethodOfLinesScheme(
            double eps,
            double relInitStepSize,
            IFdmLinearOpComposite map,
            IReadOnlyList<IFdmBoundaryCondition> bcSet = null)
        {
            _dt = null;
            _eps = eps;
            _relInitStepSize = relInitStepSize;
            _map = map;
            _bcSet = new BoundaryConditionSchemeHelper(bcSet ?? new List<IFdmBoundaryCondition>());
            
            // The Runge-Kutta solver is stateful, but its parameters don't change per step,
            // so we can instantiate it once in the constructor.
            _rungeKutta = new AdaptiveRungeKutta<double>(_eps);
        }

        /// <summary>
        /// Performs one time step of the scheme using an adaptive Runge-Kutta solver.
        /// </summary>
        /// <param name="a">The array of values at the current time step (input), which will be updated to the next time step (output).</param>
        /// <param name="t">The time of the next time step (i.e., the end of the integration interval).</param>
        public void Step(ref Array a, double t)
        {
            QL.Require(_dt.HasValue, "Time step not set.");
            QL.Require(t - _dt.Value > -1e-8, "A step towards negative time was given.");
            
            double from = t;
            double to = System.Math.Max(0.0, t - _dt.Value);

            // The ODE function to be solved by Runge-Kutta.
            AdaptiveRungeKutta<double>.OdeFunction odeFunction = (_t, _u) => Apply(_t, _u);

            // Convert input Array to double[] for the solver.
            double[] aArray = a._storage.ToArray();

            // The Runge-Kutta solver needs an initial step size hint.
            // The time step is backwards (from t to t-dt), so the initial step is negative.
            double h1 = _relInitStepSize * _dt.Value * (from <= to ? 1.0 : -1.0);
            var rk = new AdaptiveRungeKutta<double>(_eps, h1);
            
            // Solve the ODE system.
            double[] v = rk.Integrate(odeFunction, aArray, from, to);
            
            var y = new Array(v);

            _bcSet.ApplyAfterSolving(y);

            a = y;
        }

        /// <summary>
        /// Sets the time step size for subsequent calls to Step.
        /// </summary>
        /// <param name="dt">The time step size.</param>
        public void SetStep(double dt)
        {
            _dt = dt;
        }

        /// <summary>
        /// Represents the system of ODEs dy/dt = -L(y), where L is the FDM operator.
        /// This method is designed to be called by the Runge-Kutta solver.
        /// </summary>
        /// <param name="t">The current time in the integration.</param>
        /// <param name="u">The current solution vector.</param>
        /// <returns>The vector of time derivatives dy/dt.</returns>
        private double[] Apply(double t, double[] u)
        {
            // The time step for the operator is a small dummy value, as Runge-Kutta
            // controls the actual time evolution.
            _map.SetTime(t, t + 0.0001);
            _bcSet.ApplyBeforeApplying(_map);

            // Note: The ODE is du/d(tau) = L(u), where tau is time-to-maturity.
            // Since we are stepping backwards in calendar time t (t -> t-dt),
            // this is equivalent to dt = -d(tau), so du/dt = -L(u).
            Array dxdt = -_map.Apply(new Array(u));

            return dxdt._storage.ToArray();
        }
    }
}