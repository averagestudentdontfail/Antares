// C# code for SparseMatrix.cs

using System;
using Antares.Math; 
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Antares.Math.Matrix
{
    /// <summary>
    /// Represents a sparse matrix.
    /// </summary>
    /// <remarks>
    /// This class acts as a wrapper around the `MathNet.Numerics.LinearAlgebra.Double.SparseMatrix`
    /// class. It provides an API consistent with the QuantLib C++ `SparseMatrix` type alias,
    /// which was based on Boost.Ublas. This approach simplifies the porting of dependent code
    /// by leveraging a robust, modern, and high-performance sparse matrix implementation
    /// from an approved .NET library.
    /// </remarks>
    public class SparseMatrix
    {
        internal readonly Matrix<double> _storage;

        /// <summary>
        /// Gets the number of rows in the sparse matrix.
        /// </summary>
        public int RowCount => _storage.RowCount;

        /// <summary>
        /// Gets the number of columns in the sparse matrix.
        /// </summary>
        public int ColumnCount => _storage.ColumnCount;

        /// <summary>
        /// Initializes a new instance of the SparseMatrix class from an existing
        /// MathNet.Numerics sparse matrix.
        /// </summary>
        /// <param name="storage">The underlying MathNet sparse matrix.</param>
        public SparseMatrix(Matrix<double> storage)
        {
            if (!(storage is MathNet.Numerics.LinearAlgebra.Double.SparseMatrix))
            {
                // While we could technically wrap a dense matrix, the intent of this class
                // is specifically for sparse matrices. We enforce this for clarity.
                // To be more flexible, one could also just call .ToSparse() on the input.
                throw new ArgumentException("The provided storage must be a SparseMatrix instance.", nameof(storage));
            }
            _storage = storage;
        }
        
        /// <summary>
        /// Initializes a new, empty sparse matrix of the specified dimensions.
        /// </summary>
        /// <param name="rows">The number of rows.</param>
        /// <param name="columns">The number of columns.</param>
        public SparseMatrix(int rows, int columns)
        {
            _storage = MathNet.Numerics.LinearAlgebra.Double.SparseMatrix.Create(rows, columns);
        }


        /// <summary>
        /// Gets or sets the value at the specified row and column.
        /// </summary>
        /// <remarks>
        /// Getting a value is efficient. Setting a value can be inefficient for sparse
        /// matrices if it requires restructuring the underlying storage (e.g., inserting
        /// a new non-zero element). It is often more efficient to build sparse matrices
        /// from a list of triplets (row, column, value).
        /// </remarks>
        public double this[int row, int column]
        {
            get => _storage[row, column];
            set => _storage[row, column] = value;
        }

        /// <summary>
        /// Performs matrix-vector multiplication (y = A*x).
        /// </summary>
        /// <param name="a">The sparse matrix A.</param>
        /// <param name="x">The vector x.</param>
        /// <returns>A new Array containing the result of the multiplication.</returns>
        public static Array Multiply(SparseMatrix a, Array x)
        {
            QL.Require(x.Count == a.ColumnCount,
                $"Vectors and sparse matrices with different sizes ({x.Count}, {a.RowCount}x{a.ColumnCount}) cannot be multiplied.");

            // MathNet.Numerics overloads the * operator for matrix-vector multiplication.
            Vector<double> resultVector = a._storage * x._storage;
            return new Array(resultVector);
        }

        /// <summary>
        /// Overloads the multiplication operator for matrix-vector multiplication (y = A*x).
        /// </summary>
        public static Array operator *(SparseMatrix a, Array x)
        {
            return Multiply(a, x);
        }
    }
}