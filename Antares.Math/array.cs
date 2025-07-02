// Array.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

// Type aliases for brevity and consistency
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace Antares.Math
{
    /// <summary>
    /// 1-D array used in linear algebra.
    /// This class implements the concept of a vector as used in linear algebra.
    /// As such, it is not meant to be used as a general-purpose container.
    /// It wraps a MathNet.Numerics.Vector to provide an API consistent with
    /// the original QuantLib C++ Array.
    /// </summary>
    public class Array : ICloneable, IEquatable<Array>, IEnumerable<double>
    {
        internal readonly Vector _storage;

        #region Constructors
        /// <summary>
        /// Creates an empty array (size 0).
        /// </summary>
        public Array()
        {
            _storage = Vector.Build.Dense(0);
        }

        /// <summary>
        /// Creates an array of the given size, initialized to zeros.
        /// </summary>
        public Array(int size)
        {
            _storage = Vector.Build.Dense(size);
        }

        /// <summary>
        /// Creates an array of the given size and fills it with a constant value.
        /// </summary>
        public Array(int size, double value)
        {
            _storage = Vector.Build.Dense(size, value);
        }

        /// <summary>
        /// Creates an array and fills it with an arithmetic progression.
        /// a[0] = value, a[i] = a[i-1] + increment
        /// </summary>
        public Array(int size, double value, double increment)
        {
            _storage = Vector.Build.Dense(size);
            for (int i = 0; i < size; ++i, value += increment)
            {
                _storage[i] = value;
            }
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        public Array(Array from)
        {
            _storage = from._storage.Clone();
        }

        /// <summary>
        /// Creates an array from an enumerable sequence of doubles.
        /// </summary>
        public Array(IEnumerable<double> from)
        {
            _storage = Vector.Build.DenseOfEnumerable(from);
        }

        /// <summary>
        /// Internal constructor to wrap an existing MathNet Vector.
        /// </summary>
        internal Array(Vector from)
        {
            _storage = from;
        }
        #endregion

        #region Properties and Indexers
        /// <summary>
        /// Dimension of the array.
        /// </summary>
        public int Count => _storage.Count;

        /// <summary>
        /// Whether the array is empty.
        /// </summary>
        public bool IsEmpty => _storage.Count == 0;

        /// <summary>
        /// Read-write element access.
        /// </summary>
        public double this[int i]
        {
            get => _storage[i];
            set => _storage[i] = value;
        }

        /// <summary>
        /// Read-only access to the first element.
        /// </summary>
        public double Front => _storage[0];

        /// <summary>
        /// Read-only access to the last element.
        /// </summary>
        public double Back => _storage[Count - 1];
        #endregion

        #region Methods
        /// <summary>
        /// Resizes the array. Data is preserved if the new size is larger.
        /// </summary>
        public void Resize(int newSize)
        {
            _storage.Resize(newSize);
        }

        /// <summary>
        /// Swaps the content of this array with another.
        /// </summary>
        public void Swap(Array other)
        {
            _storage.Swap(other._storage);
        }
        
        /// <summary>
        /// Creates a deep copy of the array.
        /// </summary>
        public Array Clone() => new Array(this);
        object ICloneable.Clone() => Clone();
        #endregion

        #region IEnumerable<double> implementation
        public IEnumerator<double> GetEnumerator() => ((IEnumerable<double>)_storage).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion

        #region Equality
        public bool Equals(Array other)
        {
            if (other is null) return false;
            return _storage.Equals(other._storage);
        }
        public override bool Equals(object obj) => Equals(obj as Array);
        public override int GetHashCode() => _storage.GetHashCode();
        
        public static bool operator ==(Array left, Array right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }
        public static bool operator !=(Array left, Array right) => !(left == right);
        #endregion

        #region Static Methods (Free Functions in C++)
        /// <summary>
        /// Calculates the dot product of two arrays.
        /// </summary>
        public static double DotProduct(Array v1, Array v2)
        {
            if (v1.Count != v2.Count)
                throw new ArgumentException($"Arrays with different sizes ({v1.Count}, {v2.Count}) cannot be multiplied.");
            return v1._storage.DotProduct(v2._storage);
        }

        /// <summary>
        /// Calculates the L2-norm (Euclidean norm) of an array.
        /// </summary>
        public static double Norm2(Array v) => v._storage.L2Norm();

        // Math functions
        public static Array Abs(Array v) => new Array(v._storage.PointwiseAbs());
        public static Array Sqrt(Array v) => new Array(v._storage.PointwiseSqrt());
        public static Array Log(Array v) => new Array(v._storage.PointwiseLog());
        public static Array Exp(Array v) => new Array(v._storage.PointwiseExp());
        public static Array Pow(Array v, double exponent) => new Array(v._storage.PointwisePower(exponent));
        #endregion

        #region Operator Overloads
        // Unary minus
        public static Array operator -(Array v) => new Array(-v._storage);

        // Array-Array operations (element-wise)
        public static Array operator +(Array v1, Array v2) => new Array(v1._storage + v2._storage);
        public static Array operator -(Array v1, Array v2) => new Array(v1._storage - v2._storage);
        public static Array operator *(Array v1, Array v2) => new Array(v1._storage.PointwiseMultiply(v2._storage));
        public static Array operator /(Array v1, Array v2) => new Array(v1._storage.PointwiseDivide(v2._storage));

        // Array-Scalar operations
        public static Array operator +(Array v, double scalar) => new Array(v._storage + scalar);
        public static Array operator +(double scalar, Array v) => new Array(v._storage + scalar);
        public static Array operator -(Array v, double scalar) => new Array(v._storage - scalar);
        public static Array operator -(double scalar, Array v) => new Array(scalar - v._storage);
        public static Array operator *(Array v, double scalar) => new Array(v._storage * scalar);
        public static Array operator *(double scalar, Array v) => new Array(v._storage * scalar);
        public static Array operator /(Array v, double scalar) => new Array(v._storage / scalar);
        public static Array operator /(double scalar, Array v) => new Array(v._storage.PointwisePower(-1) * scalar);
        #endregion
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[ ");
            if (!IsEmpty)
            {
                for (int i = 0; i < Count - 1; i++)
                {
                    sb.AppendFormat("{0:G6}; ", this[i]);
                }
                sb.AppendFormat("{0:G6}", Back);
            }
            sb.Append(" ]");
            return sb.ToString();
        }
    }
}