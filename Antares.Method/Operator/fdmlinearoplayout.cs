// C# code for FdmLinearOpLayout.cs

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Antares.Method.Operator
{
    /// <summary>
    /// Memory layout manager for finite difference linear operators.
    /// Handles multi-dimensional grid indexing and neighborhood calculations
    /// with automatic boundary reflection for finite difference stencils.
    /// </summary>
    public class FdmLinearOpLayout : IEnumerable<FdmLinearOpIterator>
    {
        private readonly Size _size;
        private readonly List<Size> _dim;
        private readonly List<Size> _spacing;

        /// <summary>
        /// Initializes a new instance of the FdmLinearOpLayout class.
        /// </summary>
        /// <param name="dim">Dimensions of the multi-dimensional grid.</param>
        public FdmLinearOpLayout(List<Size> dim)
        {
            _dim = new List<Size>(dim ?? throw new ArgumentNullException(nameof(dim)));
            _spacing = new List<Size>(new Size[_dim.Count]);

            // Calculate spacing for row-major layout
            // spacing[0] = 1, spacing[i] = spacing[i-1] * dim[i-1]
            _spacing[0] = 1;
            for (int i = 1; i < _dim.Count; i++)
            {
                _spacing[i] = _spacing[i - 1] * _dim[i - 1];
            }

            // Total size is the product of all dimensions
            _size = _spacing.Last() * _dim.Last();
        }

        /// <summary>
        /// Gets an iterator pointing to the beginning of the layout.
        /// </summary>
        /// <returns>Iterator at the first position.</returns>
        public FdmLinearOpIterator Begin()
        {
            return new FdmLinearOpIterator(_dim);
        }

        /// <summary>
        /// Gets an iterator pointing to the end of the layout.
        /// </summary>
        /// <returns>Iterator at the past-the-end position.</returns>
        public FdmLinearOpIterator End()
        {
            return new FdmLinearOpIterator(_size);
        }

        /// <summary>
        /// Gets the dimensions of the grid.
        /// </summary>
        public IReadOnlyList<Size> Dim => _dim;

        /// <summary>
        /// Gets the spacing (strides) for each dimension.
        /// </summary>
        public IReadOnlyList<Size> Spacing => _spacing;

        /// <summary>
        /// Gets the total size of the grid.
        /// </summary>
        public Size Size => _size;

        /// <summary>
        /// Calculates the linear index from multi-dimensional coordinates.
        /// </summary>
        /// <param name="coordinates">The coordinates in each dimension.</param>
        /// <returns>The corresponding linear index.</returns>
        public Size Index(IReadOnlyList<Size> coordinates)
        {
            if (coordinates == null)
                throw new ArgumentNullException(nameof(coordinates));
            if (coordinates.Count != _dim.Count)
                throw new ArgumentException("Coordinates count must match dimensions count");

            Size index = 0;
            for (int i = 0; i < coordinates.Count; i++)
            {
                index += coordinates[i] * _spacing[i];
            }
            return index;
        }

        /// <summary>
        /// Calculates the linear index of a neighbor in a specific dimension.
        /// Uses reflection boundary conditions when the offset goes out of bounds.
        /// </summary>
        /// <param name="iterator">Current position iterator.</param>
        /// <param name="i">Dimension index.</param>
        /// <param name="offset">Offset in the specified dimension.</param>
        /// <returns>Linear index of the neighbor.</returns>
        /// <remarks>
        /// Boundary conditions use reflection:
        /// - If coordinate + offset &lt; 0: use -(coordinate + offset)
        /// - If coordinate + offset ≥ dim[i]: use 2*(dim[i]-1) - (coordinate + offset)
        /// </remarks>
        public Size Neighbourhood(FdmLinearOpIterator iterator, Size i, Integer offset)
        {
            // Calculate base index by removing contribution from dimension i
            Size myIndex = iterator.Index - iterator.Coordinates[(int)i] * _spacing[(int)i];

            // Calculate new coordinate with offset
            Integer coorOffset = (Integer)iterator.Coordinates[(int)i] + offset;

            // Apply reflection boundary conditions
            if (coorOffset < 0)
            {
                coorOffset = -coorOffset;
            }
            else if ((Size)coorOffset >= _dim[(int)i])
            {
                coorOffset = 2 * ((Integer)_dim[(int)i] - 1) - coorOffset;
            }

            return myIndex + (Size)coorOffset * _spacing[(int)i];
        }

        /// <summary>
        /// Calculates the linear index of a neighbor in two dimensions simultaneously.
        /// Uses reflection boundary conditions when offsets go out of bounds.
        /// </summary>
        /// <param name="iterator">Current position iterator.</param>
        /// <param name="i1">First dimension index.</param>
        /// <param name="offset1">Offset in the first dimension.</param>
        /// <param name="i2">Second dimension index.</param>
        /// <param name="offset2">Offset in the second dimension.</param>
        /// <returns>Linear index of the neighbor.</returns>
        public Size Neighbourhood(FdmLinearOpIterator iterator, 
                                 Size i1, Integer offset1,
                                 Size i2, Integer offset2)
        {
            // Calculate base index by removing contributions from both dimensions
            Size myIndex = iterator.Index 
                         - iterator.Coordinates[(int)i1] * _spacing[(int)i1]
                         - iterator.Coordinates[(int)i2] * _spacing[(int)i2];

            // Calculate new coordinate with offset for first dimension
            Integer coorOffset1 = (Integer)iterator.Coordinates[(int)i1] + offset1;
            if (coorOffset1 < 0)
            {
                coorOffset1 = -coorOffset1;
            }
            else if ((Size)coorOffset1 >= _dim[(int)i1])
            {
                coorOffset1 = 2 * ((Integer)_dim[(int)i1] - 1) - coorOffset1;
            }

            // Calculate new coordinate with offset for second dimension
            Integer coorOffset2 = (Integer)iterator.Coordinates[(int)i2] + offset2;
            if (coorOffset2 < 0)
            {
                coorOffset2 = -coorOffset2;
            }
            else if ((Size)coorOffset2 >= _dim[(int)i2])
            {
                coorOffset2 = 2 * ((Integer)_dim[(int)i2] - 1) - coorOffset2;
            }

            return myIndex + (Size)coorOffset1 * _spacing[(int)i1] + (Size)coorOffset2 * _spacing[(int)i2];
        }

        /// <summary>
        /// Creates a neighbor iterator with reflection boundary conditions.
        /// Note: This method is marked as "smart but sometimes too slow" in the original code.
        /// </summary>
        /// <param name="iterator">Current position iterator.</param>
        /// <param name="i">Dimension index.</param>
        /// <param name="offset">Offset in the specified dimension.</param>
        /// <returns>Iterator pointing to the neighbor position.</returns>
        public FdmLinearOpIterator IterNeighbourhood(FdmLinearOpIterator iterator, Size i, Integer offset)
        {
            var coordinates = new List<Size>(iterator.Coordinates);

            // Calculate new coordinate with offset
            Integer coorOffset = (Integer)coordinates[(int)i] + offset;

            // Apply reflection boundary conditions
            if (coorOffset < 0)
            {
                coorOffset = -coorOffset;
            }
            else if ((Size)coorOffset >= _dim[(int)i])
            {
                coorOffset = 2 * ((Integer)_dim[(int)i] - 1) - coorOffset;
            }

            coordinates[(int)i] = (Size)coorOffset;

            return new FdmLinearOpIterator(_dim, coordinates, Index(coordinates));
        }

        #region IEnumerable Implementation

        /// <summary>
        /// Gets an enumerator for iterating through the layout.
        /// </summary>
        /// <returns>An enumerator for the layout.</returns>
        public IEnumerator<FdmLinearOpIterator> GetEnumerator()
        {
            var iter = Begin();
            var end = End();
            
            while (iter != end)
            {
                yield return iter;
                ++iter;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }
}