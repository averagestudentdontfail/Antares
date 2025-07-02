// Matrix.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

// Type aliases for brevity and consistency
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using MNetMatrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;

namespace Antares.Math
{
    /// <summary>
    /// A placeholder class for Antares.Math.Array. This is required for Matrix compilation.
    /// It wraps a MathNet.Numerics.Vector and will be replaced by a full port of array.hpp.
    /// </summary>
    public class Array
    {
        public readonly Vector Storage;
        public Array(int size) { Storage = Vector.Build.Dense(size); }
        public Array(Vector vector) { Storage = vector; } // From existing vector
        public Array(IEnumerable<double> data) { Storage = Vector.Build.DenseOfEnumerable(data); }
        public int Count => Storage.Count;
        public double this[int i] { get => Storage[i]; set => Storage[i] = value; }
        public Array Clone() => new Array(Storage.Clone());
    }

    /// <summary>
    /// Matrix used in linear algebra.
    /// This class is a wrapper around MathNet.Numerics.LinearAlgebra.Matrix to provide
    /// an API that is closer to the original QuantLib::Matrix, simplifying the porting of dependent code.
    /// </summary>
    public class Matrix : ICloneable, IEquatable<Matrix>
    {
        internal readonly MNetMatrix _storage;

        #region Constructors
        /// <summary>
        /// Creates a null (0x0) matrix.
        /// </summary>
        public Matrix()
        {
            _storage = MNetMatrix.Build.Dense(0, 0);
        }

        /// <summary>
        /// Creates a matrix of the given size.
        /// </summary>
        public Matrix(int rows, int columns)
        {
            _storage = MNetMatrix.Build.Dense(rows, columns);
        }

        /// <summary>
        /// Creates a matrix of the given size and fills it with a constant value.
        /// </summary>
        public Matrix(int rows, int columns, double value)
        {
            _storage = MNetMatrix.Build.Dense(rows, columns, value);
        }

        /// <summary>
        /// Creates a matrix from a jagged array.
        /// </summary>
        public Matrix(double[][] data)
        {
            _storage = MNetMatrix.Build.DenseOfJaggedArray(data);
        }
        
        /// <summary>
        /// Internal constructor to wrap an existing MathNet Matrix.
        /// </summary>
        internal Matrix(MNetMatrix storage)
        {
            _storage = storage;
        }
        #endregion

        #region Properties and Indexers
        /// <summary>
        /// Number of rows.
        /// </summary>
        public int Rows => _storage.RowCount;

        /// <summary>
        /// Number of columns.
        /// </summary>
        public int Columns => _storage.ColumnCount;

        /// <summary>
        /// True if the matrix is empty.
        /// </summary>
        public bool IsEmpty => _storage.RowCount == 0 || _storage.ColumnCount == 0;

        /// <summary>
        /// Element access.
        /// </summary>
        public double this[int row, int col]
        {
            get => _storage[row, col];
            set => _storage[row, col] = value;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Returns a view of the specified row.
        /// </summary>
        public Vector Row(int index) => _storage.Row(index);

        /// <summary>
        /// Returns a view of the specified column.
        /// </summary>
        public Vector Column(int index) => _storage.Column(index);

        /// <summary>
        /// Returns the diagonal of the matrix as an Array.
        /// </summary>
        public Array Diagonal() => new Array(_storage.Diagonal());

        /// <summary>
        /// Creates a deep copy of the matrix.
        /// </summary>
        public Matrix Clone() => new Matrix(_storage.Clone());
        object ICloneable.Clone() => Clone();
        #endregion

        #region Static Methods (Free Functions in C++)
        /// <summary>
        /// Returns the inverse of a matrix.
        /// </summary>
        public static Matrix Inverse(Matrix m)
        {
            if (m.Rows != m.Columns)
                throw new ArgumentException("Matrix is not square.", nameof(m));
            return new Matrix(m._storage.Inverse());
        }

        /// <summary>
        /// Returns the determinant of a matrix.
        /// </summary>
        public static double Determinant(Matrix m)
        {
            if (m.Rows != m.Columns)
                throw new ArgumentException("Matrix is not square.", nameof(m));
            return m._storage.Determinant();
        }

        /// <summary>
        /// Returns the transpose of a matrix.
        /// </summary>
        public static Matrix Transpose(Matrix m) => new Matrix(m._storage.Transpose());

        /// <summary>
        /// Calculates the outer product of two vectors.
        /// </summary>
        public static Matrix OuterProduct(Array v1, Array v2)
        {
            if (v1.Count == 0 || v2.Count == 0)
                throw new ArgumentException("Input vectors cannot be empty.");
            return new Matrix(MNetMatrix.OuterProduct(v1.Storage, v2.Storage));
        }
        #endregion

        #region Operator Overloads
        // Unary minus
        public static Matrix operator -(Matrix m) => new Matrix(-m._storage);

        // Matrix-Matrix operations
        public static Matrix operator +(Matrix m1, Matrix m2) => new Matrix(m1._storage + m2._storage);
        public static Matrix operator -(Matrix m1, Matrix m2) => new Matrix(m1._storage - m2._storage);
        public static Matrix operator *(Matrix m1, Matrix m2) => new Matrix(m1._storage * m2._storage);

        // Matrix-Scalar operations
        public static Matrix operator *(Matrix m, double scalar) => new Matrix(m._storage * scalar);
        public static Matrix operator *(double scalar, Matrix m) => new Matrix(scalar * m._storage);
        public static Matrix operator /(Matrix m, double scalar) => new Matrix(m._storage / scalar);

        // Matrix-Vector operations
        public static Array operator *(Array v, Matrix m) => new Array(v.Storage * m._storage);
        public static Array operator *(Matrix m, Array v) => new Array(m._storage * v.Storage);
        #endregion
        
        #region Equality and Formatting
        public bool Equals(Matrix other)
        {
            if (other is null) return false;
            return _storage.Equals(other._storage);
        }

        public override bool Equals(object obj) => Equals(obj as Matrix);
        public override int GetHashCode() => _storage.GetHashCode();

        public override string ToString()
        {
            // Provides a formatted string similar to QL's operator<<
            var sb = new StringBuilder();
            sb.AppendLine($"Matrix({Rows}x{Columns})");
            for (int i = 0; i < Rows; i++)
            {
                sb.Append("| ");
                for (int j = 0; j < Columns; j++)
                {
                    sb.AppendFormat("{0,10:F4} ", this[i, j]);
                }
                sb.AppendLine("|");
            }
            return sb.ToString();
        }
        #endregion
    }
}