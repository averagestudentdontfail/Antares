// C# code for BoundaryConditionSchemeHelper.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Method.Operator;
using Antares.Method.Utilities;

namespace Antares.Method.Utilities
{
    /// <summary>
    /// Interface for a single boundary condition in the FDM framework.
    /// </summary>
    public interface IFdmBoundaryCondition
    {
        /// <summary>
        /// Called before the operator is applied to modify the operator if needed.
        /// </summary>
        /// <param name="op">The linear operator to be modified.</param>
        void ApplyBeforeApplying(IFdmLinearOp op);

        /// <summary>
        /// Called before solving the linear system to modify the right-hand side.
        /// </summary>
        /// <param name="op">The linear operator.</param>
        /// <param name="a">The right-hand side array to be modified.</param>
        void ApplyBeforeSolving(IFdmLinearOp op, Array a);

        /// <summary>
        /// Called after the operator has been applied to adjust the result.
        /// </summary>
        /// <param name="a">The result array to be modified.</param>
        void ApplyAfterApplying(Array a);

        /// <summary>
        /// Called after solving the linear system to adjust the solution.
        /// </summary>
        /// <param name="a">The solution array to be modified.</param>
        void ApplyAfterSolving(Array a);

        /// <summary>
        /// Sets the current time for time-dependent boundary conditions.
        /// </summary>
        /// <param name="t">The current time.</param>
        void SetTime(double t);
    }

    /// <summary>
    /// Abstract base class for boundary conditions providing default implementations.
    /// </summary>
    public abstract class FdmBoundaryCondition : IFdmBoundaryCondition
    {
        /// <summary>
        /// Default implementation does nothing.
        /// Override in derived classes if operator modification is needed.
        /// </summary>
        public virtual void ApplyBeforeApplying(IFdmLinearOp op) { }

        /// <summary>
        /// Default implementation does nothing.
        /// Override in derived classes if right-hand side modification is needed.
        /// </summary>
        public virtual void ApplyBeforeSolving(IFdmLinearOp op, Array a) { }

        /// <summary>
        /// Default implementation does nothing.
        /// Override in derived classes if result adjustment is needed.
        /// </summary>
        public virtual void ApplyAfterApplying(Array a) { }

        /// <summary>
        /// Default implementation does nothing.
        /// Override in derived classes if solution adjustment is needed.
        /// </summary>
        public virtual void ApplyAfterSolving(Array a) { }

        /// <summary>
        /// Default implementation does nothing.
        /// Override in derived classes for time-dependent boundary conditions.
        /// </summary>
        public virtual void SetTime(double t) { }
    }

    /// <summary>
    /// Dirichlet boundary condition (fixed value at boundary).
    /// </summary>
    public class FdmDirichletBoundary : FdmBoundaryCondition
    {
        private readonly Func<double, double> _valueFunction;
        private readonly int _direction;
        private readonly FdmBoundaryCondition.Side _side;

        public enum Side { Lower, Upper }

        public FdmDirichletBoundary(Func<double, double> valueFunction, 
                                   int direction, 
                                   Side side)
        {
            _valueFunction = valueFunction ?? throw new ArgumentNullException(nameof(valueFunction));
            _direction = direction;
            _side = side;
        }

        public override void ApplyAfterSolving(Array a)
        {
            // Implementation would set boundary values according to the Dirichlet condition
            // This is a simplified version - the actual implementation would depend on 
            // the mesher and layout details
        }
    }

    /// <summary>
    /// Neumann boundary condition (fixed derivative at boundary).
    /// </summary>
    public class FdmNeumannBoundary : FdmBoundaryCondition
    {
        private readonly Func<double, double> _derivativeFunction;
        private readonly int _direction;
        private readonly FdmBoundaryCondition.Side _side;

        public FdmNeumannBoundary(Func<double, double> derivativeFunction, 
                                 int direction, 
                                 FdmBoundaryCondition.Side side)
        {
            _derivativeFunction = derivativeFunction ?? throw new ArgumentNullException(nameof(derivativeFunction));
            _direction = direction;
            _side = side;
        }

        public override void ApplyAfterSolving(Array a)
        {
            // Implementation would enforce the derivative condition at the boundary
        }
    }
}

namespace Antares.Method.Scheme
{
    /// <summary>
    /// Helper class to apply a set of boundary conditions to an FDM scheme.
    /// </summary>
    /// <remarks>
    /// This class acts as a facade, iterating over a collection of boundary
    /// conditions and applying the relevant action from each one.
    /// </remarks>
    public class BoundaryConditionSchemeHelper
    {
        private readonly IReadOnlyList<IFdmBoundaryCondition> _bcSet;

        /// <summary>
        /// Initializes a new instance of the BoundaryConditionSchemeHelper class.
        /// </summary>
        /// <param name="bcSet">The set of boundary conditions to be managed by this helper.</param>
        public BoundaryConditionSchemeHelper(IReadOnlyList<IFdmBoundaryCondition> bcSet)
        {
            _bcSet = bcSet ?? throw new ArgumentNullException(nameof(bcSet));
        }

        /// <summary>
        /// Applies all boundary conditions before the operator is applied.
        /// </summary>
        /// <param name="op">The linear operator.</param>
        public void ApplyBeforeApplying(IFdmLinearOp op)
        {
            foreach (var bc in _bcSet)
            {
                bc.ApplyBeforeApplying(op);
            }
        }

        /// <summary>
        /// Applies all boundary conditions before the system is solved.
        /// </summary>
        /// <param name="op">The linear operator.</param>
        /// <param name="a">The array representing the right-hand side of the system.</param>
        public void ApplyBeforeSolving(IFdmLinearOp op, Array a)
        {
            foreach (var bc in _bcSet)
            {
                bc.ApplyBeforeSolving(op, a);
            }
        }

        /// <summary>
        /// Applies all boundary conditions after the operator has been applied.
        /// </summary>
        /// <param name="a">The array representing the result of the operator application.</param>
        public void ApplyAfterApplying(Array a)
        {
            foreach (var bc in _bcSet)
            {
                bc.ApplyAfterApplying(a);
            }
        }

        /// <summary>
        /// Applies all boundary conditions after the system has been solved.
        /// </summary>
        /// <param name="a">The array representing the solution of the linear system.</param>
        public void ApplyAfterSolving(Array a)
        {
            foreach (var bc in _bcSet)
            {
                bc.ApplyAfterSolving(a);
            }
        }

        /// <summary>
        /// Sets the time for all time-dependent boundary conditions.
        /// </summary>
        /// <param name="t">The current time.</param>
        public void SetTime(double t)
        {
            foreach (var bc in _bcSet)
            {
                bc.SetTime(t);
            }
        }
    }
}