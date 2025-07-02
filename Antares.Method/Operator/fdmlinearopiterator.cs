// C# code for FdmLinearOpIterator.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Antares.Method.Operator
{
    /// <summary>
    /// Iterator for finite difference linear operators.
    /// Provides multi-dimensional grid iteration with automatic coordinate tracking.
    /// </summary>
    public class FdmLinearOpIterator : IEnumerator<FdmLinearOpIterator>, IEnumerable<FdmLinearOpIterator>
    {
        private Size _index;
        private readonly List<Size> _dim;
        private readonly List<Size> _coordinates;

        /// <summary>
        /// Initializes a new instance with a specific index.
        /// </summary>
        /// <param name="index">Starting index (default 0).</param>
        public FdmLinearOpIterator(Size index = 0)
        {
            _index = index;
            _dim = new List<Size>();
            _coordinates = new List<Size>();
        }

        /// <summary>
        /// Initializes a new instance with specified dimensions.
        /// </summary>
        /// <param name="dim">Dimensions of the grid.</param>
        public FdmLinearOpIterator(List<Size> dim)
        {
            _index = 0;
            _dim = new List<Size>(dim ?? throw new ArgumentNullException(nameof(dim)));
            _coordinates = new List<Size>(new Size[_dim.Count]); // Initialize with zeros
        }

        /// <summary>
        /// Initializes a new instance with dimensions, coordinates, and index.
        /// </summary>
        /// <param name="dim">Dimensions of the grid.</param>
        /// <param name="coordinates">Initial coordinates.</param>
        /// <param name="index">Initial index.</param>
        public FdmLinearOpIterator(List<Size> dim, List<Size> coordinates, Size index)
        {
            _index = index;
            _dim = new List<Size>(dim ?? throw new ArgumentNullException(nameof(dim)));
            _coordinates = new List<Size>(coordinates ?? throw new ArgumentNullException(nameof(coordinates)));
        }

        /// <summary>
        /// Gets the current linear index.
        /// </summary>
        public Size Index => _index;

        /// <summary>
        /// Gets the current coordinates.
        /// </summary>
        public IReadOnlyList<Size> Coordinates => _coordinates;

        /// <summary>
        /// Increments the iterator to the next position.
        /// </summary>
        public void Increment()
        {
            ++_index;
            
            // Update coordinates using row-major ordering
            for (int i = 0; i < _dim.Count; ++i)
            {
                if (++_coordinates[i] == _dim[i])
                {
                    _coordinates[i] = 0;
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Swaps the contents of this iterator with another.
        /// </summary>
        /// <param name="other">Iterator to swap with.</param>
        public void Swap(FdmLinearOpIterator other)
        {
            if (other == null) throw new ArgumentNullException(nameof(other));

            (_index, other._index) = (other._index, _index);
            
            // Swap the contents of the lists
            var tempDim = new List<Size>(_dim);
            _dim.Clear();
            _dim.AddRange(other._dim);
            other._dim.Clear();
            other._dim.AddRange(tempDim);

            var tempCoord = new List<Size>(_coordinates);
            _coordinates.Clear();
            _coordinates.AddRange(other._coordinates);
            other._coordinates.Clear();
            other._coordinates.AddRange(tempCoord);
        }

        /// <summary>
        /// Checks if this iterator is not equal to another.
        /// </summary>
        /// <param name="other">Iterator to compare with.</param>
        /// <returns>True if not equal, false otherwise.</returns>
        public bool NotEquals(FdmLinearOpIterator other)
        {
            return other == null || _index != other._index;
        }

        #region IEnumerator<FdmLinearOpIterator> Implementation

        /// <summary>
        /// Gets the current iterator (for range-based for loop compatibility).
        /// </summary>
        public FdmLinearOpIterator Current => this;

        object IEnumerator.Current => Current;

        /// <summary>
        /// Advances to the next position.
        /// </summary>
        /// <returns>True if there are more positions, false otherwise.</returns>
        public bool MoveNext()
        {
            Increment();
            // This is a simple implementation - in practice, you'd need bounds checking
            return true; // The original C++ version doesn't have bounds checking in the iterator
        }

        /// <summary>
        /// Resets the iterator to the beginning.
        /// </summary>
        public void Reset()
        {
            _index = 0;
            for (int i = 0; i < _coordinates.Count; i++)
            {
                _coordinates[i] = 0;
            }
        }

        /// <summary>
        /// Disposes the iterator.
        /// </summary>
        public void Dispose()
        {
            // Nothing to dispose for this iterator
        }

        #endregion

        #region IEnumerable<FdmLinearOpIterator> Implementation

        /// <summary>
        /// Gets an enumerator for this iterator.
        /// </summary>
        /// <returns>This iterator as an enumerator.</returns>
        public IEnumerator<FdmLinearOpIterator> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region Operator Overloads

        /// <summary>
        /// Prefix increment operator.
        /// </summary>
        /// <param name="iterator">Iterator to increment.</param>
        /// <returns>Reference to the incremented iterator.</returns>
        public static FdmLinearOpIterator operator ++(FdmLinearOpIterator iterator)
        {
            iterator.Increment();
            return iterator;
        }

        /// <summary>
        /// Inequality operator.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if not equal, false otherwise.</returns>
        public static bool operator !=(FdmLinearOpIterator left, FdmLinearOpIterator right)
        {
            if (left is null) return right is not null;
            return left.NotEquals(right);
        }

        /// <summary>
        /// Equality operator.
        /// </summary>
        /// <param name="left">Left operand.</param>
        /// <param name="right">Right operand.</param>
        /// <returns>True if equal, false otherwise.</returns>
        public static bool operator ==(FdmLinearOpIterator left, FdmLinearOpIterator right)
        {
            return !(left != right);
        }

        public override bool Equals(object obj)
        {
            return obj is FdmLinearOpIterator other && !NotEquals(other);
        }

        public override int GetHashCode()
        {
            return _index.GetHashCode();
        }

        #endregion
    }
}