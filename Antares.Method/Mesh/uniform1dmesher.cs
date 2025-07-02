// C# code for Uniform1dMesher.cs

using Antares.Method.Mesh;

namespace Antares.Method.Mesh
{
    /// <summary>
    /// One-dimensional simple uniform grid mesher.
    /// </summary>
    public class Uniform1dMesher : Fdm1dMesher
    {
        /// <summary>
        /// Initializes a new instance of the Uniform1dMesher class.
        /// </summary>
        /// <param name="start">The start of the domain.</param>
        /// <param name="end">The end of the domain.</param>
        /// <param name="size">The number of points in the grid.</param>
        public Uniform1dMesher(double start, double end, int size)
            : base(size)
        {
            QL.Require(end > start, "end must be larger than start");

            // Avoid division by zero if size is 1
            double dx = (size > 1) ? (end - start) / (size - 1) : 0.0;

            for (int i = 0; i < size; ++i)
            {
                _locations[i] = start + i * dx;
            }
            
            // Ensure the last point is exactly at the end to avoid floating point drift
            if (size > 1)
            {
                _locations[size - 1] = end;
            }
            
            for (int i = 0; i < size - 1; ++i)
            {
                _dplus[i] = _dminus[i + 1] = _locations[i+1] - _locations[i];
            }
            
            // Set boundary deltas to NaN, representing null/undefined
            if (size > 0)
            {
                _dplus[size - 1] = double.NaN;
                _dminus[0] = double.NaN;
            }
        }
    }
}