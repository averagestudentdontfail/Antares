// C# code for FdmLinearOp.cs

using Antares.Math; // For Antares.Math.Array
using Antares.Math.Matrix; // For Antares.Math.Matrix.SparseMatrix

namespace Antares.Method.Operator
{
    /// <summary>
    /// Abstract base class for a linear operator to model a multi-dimensional PDE system.
    /// </summary>
    /// <remarks>
    /// This interface defines the contract for linear operators used in the
    /// Finite Difference Method (FDM) framework. Concrete implementations will
    /// represent specific differential operators.
    /// </remarks>
    public interface IFdmLinearOp
    {
        /// <summary>
        /// Applies the linear operator to a given array (vector).
        /// </summary>
        /// <param name="r">The input array.</param>
        /// <returns>The result of the operator application.</returns>
        Array Apply(Array r);

        /// <summary>
        /// Converts the linear operator into its sparse matrix representation.
        /// </summary>
        /// <returns>A SparseMatrix representing the operator.</returns>
        SparseMatrix ToMatrix();
    }
}