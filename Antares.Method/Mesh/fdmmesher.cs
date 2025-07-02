// C# code for FdmMesher.cs

using Antares.Math; // For Antares.Math.Array
using Antares.Method.Operator;

// Placeholders for dependent types. In a full project, these would be in their own files.
namespace Antares.Method.Operator
{
    /// <summary>
    /// Placeholder for the FdmLinearOpLayout interface.
    /// Represents the layout of a multi-dimensional grid.
    /// </summary>
    public interface IFdmLinearOpLayout { }

    /// <summary>
    /// Placeholder for the FdmLinearOpIterator interface.
    /// Represents an iterator over the grid points of a layout.
    /// </summary>
    public interface IFdmLinearOpIterator { }
}


namespace Antares.Method.Mesh
{
    /// <summary>
    /// Base class for a mesher for a multi-dimensional FDM grid.
    /// </summary>
    public abstract class FdmMesher
    {
        private readonly IFdmLinearOpLayout _layout;

        /// <summary>
        /// Gets the layout of the FDM operator associated with this mesher.
        /// </summary>
        public IFdmLinearOpLayout Layout => _layout;

        /// <summary>
        /// Initializes a new instance of the FdmMesher class.
        /// The constructor is protected as this is an abstract base class.
        /// </summary>
        /// <param name="layout">The layout of the FDM operator.</param>
        protected FdmMesher(IFdmLinearOpLayout layout)
        {
            _layout = layout;
        }

        // The virtual destructor in C++ is not needed in C# due to automatic garbage collection.

        /// <summary>
        /// Gets the distance to the next grid point in a given direction.
        /// </summary>
        /// <param name="iter">The iterator pointing to the current grid location.</param>
        /// <param name="direction">The direction of the step.</param>
        /// <returns>The distance to the next point.</returns>
        public abstract double Dplus(IFdmLinearOpIterator iter, int direction);

        /// <summary>
        /// Gets the distance to the previous grid point in a given direction.
        /// </summary>
        /// <param name="iter">The iterator pointing to the current grid location.</param>
        /// <param name="direction">The direction of the step.</param>
        /// <returns>The distance to the previous point.</returns>
        public abstract double Dminus(IFdmLinearOpIterator iter, int direction);

        /// <summary>
        /// Gets the coordinate of the current grid point in a given direction.
        /// </summary>
        /// <param name="iter">The iterator pointing to the current grid location.</param>
        /// <param name="direction">The direction (axis) of the coordinate.</param>
        /// <returns>The coordinate value.</returns>
        public abstract double Location(IFdmLinearOpIterator iter, int direction);

        /// <summary>
        /// Gets all grid locations for a given direction (axis).
        /// </summary>
        /// <param name="direction">The direction (axis) of the coordinates.</param>
        /// <returns>An Array containing all grid points along the specified axis.</returns>
        public abstract Array Locations(int direction);
    }
}