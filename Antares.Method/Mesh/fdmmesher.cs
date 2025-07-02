// C# code for FdmMesher.cs

using System;
using Antares.Math; // For Antares.Math.Array
using Antares.Method.Operator;

namespace Antares.Method.Mesh
{
    /// <summary>
    /// Base class for a mesher for a multi-dimensional FDM grid.
    /// </summary>
    public abstract class FdmMesher
    {
        private readonly FdmLinearOpLayout _layout;

        /// <summary>
        /// Gets the layout of the FDM operator associated with this mesher.
        /// </summary>
        public FdmLinearOpLayout Layout => _layout;

        /// <summary>
        /// Initializes a new instance of the FdmMesher class.
        /// The constructor is protected as this is an abstract base class.
        /// </summary>
        /// <param name="layout">The layout of the FDM operator.</param>
        protected FdmMesher(FdmLinearOpLayout layout)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
        }

        // The virtual destructor in C++ is not needed in C# due to automatic garbage collection.

        /// <summary>
        /// Gets the distance to the next grid point in a given direction.
        /// </summary>
        /// <param name="iter">The iterator pointing to the current grid location.</param>
        /// <param name="direction">The direction of the step.</param>
        /// <returns>The distance to the next point.</returns>
        public abstract double Dplus(FdmLinearOpIterator iter, int direction);

        /// <summary>
        /// Gets the distance to the previous grid point in a given direction.
        /// </summary>
        /// <param name="iter">The iterator pointing to the current grid location.</param>
        /// <param name="direction">The direction of the step.</param>
        /// <returns>The distance to the previous point.</returns>
        public abstract double Dminus(FdmLinearOpIterator iter, int direction);

        /// <summary>
        /// Gets the coordinate of the current grid point in a given direction.
        /// </summary>
        /// <param name="iter">The iterator pointing to the current grid location.</param>
        /// <param name="direction">The direction (axis) of the coordinate.</param>
        /// <returns>The coordinate value.</returns>
        public abstract double Location(FdmLinearOpIterator iter, int direction);

        /// <summary>
        /// Gets all grid locations for a given direction (axis).
        /// </summary>
        /// <param name="direction">The direction (axis) for which to retrieve locations.</param>
        /// <returns>An array containing the coordinates for all grid points in the specified direction.</returns>
        public abstract Array Locations(int direction);

        /// <summary>
        /// Gets the number of grid points in total.
        /// </summary>
        /// <returns>The total number of grid points across all dimensions.</returns>
        public Size Size() => _layout.Size;

        /// <summary>
        /// Gets the dimensions of the grid.
        /// </summary>
        /// <returns>The dimensions of the multi-dimensional grid.</returns>
        public System.Collections.Generic.IReadOnlyList<Size> GetDim() => _layout.Dim;
    }

    /// <summary>
    /// Abstract base class for one-dimensional meshers.
    /// </summary>
    public abstract class Fdm1dMesher
    {
        /// <summary>
        /// Gets the number of grid points in this dimension.
        /// </summary>
        public abstract Size Size { get; }

        /// <summary>
        /// Gets the distance to the next grid point.
        /// </summary>
        /// <param name="index">The current grid index.</param>
        /// <returns>The distance to the next point.</returns>
        public abstract double Dplus(Size index);

        /// <summary>
        /// Gets the distance to the previous grid point.
        /// </summary>
        /// <param name="index">The current grid index.</param>
        /// <returns>The distance to the previous point.</returns>
        public abstract double Dminus(Size index);

        /// <summary>
        /// Gets the coordinate at a specific grid index.
        /// </summary>
        /// <param name="index">The grid index.</param>
        /// <returns>The coordinate at the specified index.</returns>
        public abstract double Location(Size index);

        /// <summary>
        /// Gets all grid coordinates for this dimension.
        /// </summary>
        public abstract Array Locations { get; }

        /// <summary>
        /// Gets the first derivative weights for finite difference approximation.
        /// </summary>
        /// <param name="index">The grid index.</param>
        /// <returns>The first derivative weights.</returns>
        public abstract Array FirstDerivativeWeights(Size index);

        /// <summary>
        /// Gets the second derivative weights for finite difference approximation.
        /// </summary>
        /// <param name="index">The grid index.</param>
        /// <returns>The second derivative weights.</returns>
        public abstract Array SecondDerivativeWeights(Size index);
    }

    /// <summary>
    /// Uniform grid mesher for one dimension.
    /// </summary>
    public class Uniform1dMesher : Fdm1dMesher
    {
        private readonly Size _size;
        private readonly double _start, _end;
        private readonly Array _locations;
        private readonly double _dx;

        /// <summary>
        /// Initializes a new instance of the Uniform1dMesher class.
        /// </summary>
        /// <param name="start">The start coordinate.</param>
        /// <param name="end">The end coordinate.</param>
        /// <param name="size">The number of grid points.</param>
        public Uniform1dMesher(double start, double end, Size size)
        {
            QL.Require(end > start, "End must be greater than start");
            QL.Require(size > 1, "Size must be greater than 1");

            _start = start;
            _end = end;
            _size = size;
            _dx = (end - start) / (size - 1);
            
            _locations = new Array(size);
            for (Size i = 0; i < size; ++i)
            {
                _locations[i] = start + i * _dx;
            }
        }

        public override Size Size => _size;

        public override Array Locations => _locations;

        public override double Dplus(Size index)
        {
            return (index < _size - 1) ? _dx : 0.0;
        }

        public override double Dminus(Size index)
        {
            return (index > 0) ? _dx : 0.0;
        }

        public override double Location(Size index)
        {
            QL.Require(index < _size, "Index out of bounds");
            return _locations[index];
        }

        public override Array FirstDerivativeWeights(Size index)
        {
            var weights = new Array(3); // Support for 3-point stencil
            
            if (index == 0)
            {
                // Forward difference
                weights[0] = -1.0 / _dx;
                weights[1] = 1.0 / _dx;
                weights[2] = 0.0;
            }
            else if (index == _size - 1)
            {
                // Backward difference  
                weights[0] = 0.0;
                weights[1] = -1.0 / _dx;
                weights[2] = 1.0 / _dx;
            }
            else
            {
                // Central difference
                weights[0] = -0.5 / _dx;
                weights[1] = 0.0;
                weights[2] = 0.5 / _dx;
            }
            
            return weights;
        }

        public override Array SecondDerivativeWeights(Size index)
        {
            var weights = new Array(3); // Support for 3-point stencil
            double dx2 = _dx * _dx;
            
            if (index == 0 || index == _size - 1)
            {
                // At boundaries, use one-sided differences or set to zero
                weights[0] = 0.0;
                weights[1] = 0.0;
                weights[2] = 0.0;
            }
            else
            {
                // Central difference for second derivative
                weights[0] = 1.0 / dx2;
                weights[1] = -2.0 / dx2;
                weights[2] = 1.0 / dx2;
            }
            
            return weights;
        }
    }
}