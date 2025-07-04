// C# code for FdmBoundaryConditionSet.cs

using System.Collections.Generic;

namespace Antares.Method.Utility
{
    /// <summary>
    /// Type alias for a set of finite difference method boundary conditions.
    /// This represents a collection of boundary conditions that can be applied
    /// to FDM linear operators during the solution process.
    /// </summary>
    /// <remarks>
    /// This type alias provides a convenient shorthand for the boundary condition
    /// set type used throughout the finite difference method framework.
    /// It corresponds to the C++ typedef:
    /// typedef OperatorTraits&lt;FdmLinearOp&gt;::bc_set FdmBoundaryConditionSet;
    /// </remarks>
    public class FdmBoundaryConditionSet : List<BoundaryCondition<FdmLinearOp>>
    {
        /// <summary>
        /// Initializes a new instance of the FdmBoundaryConditionSet class.
        /// </summary>
        public FdmBoundaryConditionSet() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the FdmBoundaryConditionSet class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity of the boundary condition set.</param>
        public FdmBoundaryConditionSet(int capacity) : base(capacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the FdmBoundaryConditionSet class with boundary conditions from another collection.
        /// </summary>
        /// <param name="collection">The collection of boundary conditions to copy.</param>
        public FdmBoundaryConditionSet(IEnumerable<BoundaryCondition<FdmLinearOp>> collection) : base(collection)
        {
        }
    }
}