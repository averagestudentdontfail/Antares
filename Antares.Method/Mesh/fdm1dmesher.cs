// C# code for Fdm1dMesher.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math; // For Antares.Math.Array

namespace Antares.Method.Mesh
{
    /// <summary>
    /// One-dimensional simple FDM mesher object working on an index.
    /// </summary>
    /// <remarks>
    /// This class serves as a base for one-dimensional meshers in the
    /// Finite Difference Method framework. It stores the grid point locations
    /// and the distances to the next and previous points.
    /// </remarks>
    public class Fdm1dMesher
    {
        protected readonly double[] _locations;
        protected readonly double[] _dplus;
        protected readonly double[] _dminus;

        /// <summary>
        /// Initializes a new instance of the Fdm1dMesher class.
        /// </summary>
        /// <param name="size">The number of points in the mesh.</param>
        public Fdm1dMesher(int size)
        {
            _locations = new double[size];
            _dplus = new double[size];
            _dminus = new double[size];
        }

        /// <summary>
        /// Gets the number of points in the mesh.
        /// </summary>
        public int Size => _locations.Length;

        /// <summary>
        /// Gets the distance to the next grid point.
        /// dplus(i) = location(i+1) - location(i)
        /// </summary>
        /// <param name="index">The index of the current grid point.</param>
        /// <returns>The distance to the next point.</returns>
        public double Dplus(int index) => _dplus[index];

        /// <summary>
        /// Gets the distance to the previous grid point.
        /// dminus(i) = location(i) - location(i-1)
        /// </summary>
        /// <param name="index">The index of the current grid point.</param>
        /// <returns>The distance to the previous point.</returns>
        public double Dminus(int index) => _dminus[index];

        /// <summary>
        /// Gets the location of a specific grid point.
        /// </summary>
        /// <param name="index">The index of the grid point.</param>
        /// <returns>The coordinate of the grid point.</returns>
        public double Location(int index) => _locations[index];

        /// <summary>
        /// Gets a read-only list of all grid point locations.
        /// </summary>
        public IReadOnlyList<double> Locations => _locations;
    }
}