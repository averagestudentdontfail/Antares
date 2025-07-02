// C# code for BoundaryConditionSchemeHelper.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using Antares.Method.Operator;
using Antares.Method.Utilities;

// This placeholder would be in its own file (e.g., FdmBoundaryCondition.cs) in a full project.
namespace Antares.Method.Utilities
{
    /// <summary>
    /// Placeholder for the FdmBoundaryCondition interface.
    /// This defines the contract for a single boundary condition in the FDM framework.
    /// </summary>
    public interface IFdmBoundaryCondition
    {
        void ApplyBeforeApplying(IFdmLinearOp op);
        void ApplyBeforeSolving(IFdmLinearOp op, Array a);
        void ApplyAfterApplying(Array a);
        void ApplyAfterSolving(Array a);
        void SetTime(double t);
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
        /// <param name="a">The array representing the solution of the system.</param>
        public void ApplyAfterSolving(Array a)
        {
            foreach (var bc in _bcSet)
            {
                bc.ApplyAfterSolving(a);
            }
        }

        /// <summary>
        /// Sets the current time for all boundary conditions.
        /// </summary>
        /// <param name="t">The time value.</param>
        public void SetTime(double t)
        {
            foreach (var bc in _bcSet)
            {
                bc.SetTime(t);
            }
        }
    }
}