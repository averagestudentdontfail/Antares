// C# code for FdmLinearOpComposite.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math;
using Antares.Math.Matrix;

namespace Antares.Method.Operator
{
    /// <summary>
    /// Composite pattern implementation for finite difference linear operators.
    /// Extends FdmLinearOp with additional functionality for multi-dimensional
    /// operators and operator splitting methods.
    /// </summary>
    public abstract class FdmLinearOpComposite : FdmLinearOp
    {
        /// <summary>
        /// Gets the number of factors (dimensions) in this operator.
        /// </summary>
        public abstract Size Size { get; }

        /// <summary>
        /// Sets the time interval for the operator.
        /// </summary>
        /// <param name="t1">Start time (must be ≤ t2).</param>
        /// <param name="t2">End time (must be ≥ t1).</param>
        /// <remarks>Time t1 ≤ t2 is required.</remarks>
        public abstract void SetTime(Time t1, Time t2);

        /// <summary>
        /// Applies mixed derivative terms of the operator.
        /// </summary>
        /// <param name="r">Input array.</param>
        /// <returns>Result of applying mixed derivative terms.</returns>
        public abstract Array ApplyMixed(Array r);

        /// <summary>
        /// Applies the operator in a specific direction.
        /// </summary>
        /// <param name="direction">The direction to apply the operator.</param>
        /// <param name="r">Input array.</param>
        /// <returns>Result of applying the operator in the specified direction.</returns>
        public abstract Array ApplyDirection(Size direction, Array r);

        /// <summary>
        /// Solves the splitting step for operator splitting methods.
        /// </summary>
        /// <param name="direction">The direction for the splitting step.</param>
        /// <param name="r">Right-hand side array.</param>
        /// <param name="s">Splitting parameter (typically time step).</param>
        /// <returns>Solution of the splitting step.</returns>
        public abstract Array SolveSplitting(Size direction, Array r, Real s);

        /// <summary>
        /// Applies preconditioning for iterative solvers.
        /// </summary>
        /// <param name="r">Input array.</param>
        /// <param name="s">Preconditioning parameter.</param>
        /// <returns>Preconditioned array.</returns>
        public abstract Array Preconditioner(Array r, Real s);

        /// <summary>
        /// Returns the matrix decomposition of the operator.
        /// Default implementation throws an exception - override in derived classes.
        /// </summary>
        /// <returns>Vector of sparse matrices representing the decomposition.</returns>
        public virtual List<SparseMatrix> ToMatrixDecomp()
        {
            QL.Fail("Matrix decomposition representation is not implemented");
            return null; // Unreachable
        }

        /// <summary>
        /// Converts this linear operator to its matrix representation.
        /// Accumulates the matrix decomposition into a single matrix.
        /// </summary>
        /// <returns>A sparse matrix representation of this operator.</returns>
        public override SparseMatrix ToMatrix()
        {
            List<SparseMatrix> dcmp = ToMatrixDecomp();
            
            if (dcmp == null || dcmp.Count == 0)
                throw new InvalidOperationException("Matrix decomposition returned empty result");

            // Start with the first matrix and accumulate the rest
            SparseMatrix result = dcmp[0];
            
            for (int i = 1; i < dcmp.Count; i++)
            {
                // In the original C++, this uses std::accumulate with SparseMatrix addition
                // Here we assume SparseMatrix supports addition operator
                result = AddSparseMatrices(result, dcmp[i]);
            }
            
            return result;
        }

        /// <summary>
        /// Helper method to add sparse matrices.
        /// This would typically be implemented as an operator in the SparseMatrix class.
        /// </summary>
        /// <param name="a">First matrix.</param>
        /// <param name="b">Second matrix.</param>
        /// <returns>Sum of the matrices.</returns>
        private static SparseMatrix AddSparseMatrices(SparseMatrix a, SparseMatrix b)
        {
            // This should be implemented in the SparseMatrix class as operator+
            // For now, we'll assume it exists or provide a placeholder
            throw new NotImplementedException("SparseMatrix addition needs to be implemented in the SparseMatrix class");
        }
    }
}