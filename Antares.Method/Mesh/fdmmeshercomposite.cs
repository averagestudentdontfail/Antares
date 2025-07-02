// C# code for FdmMesherComposite.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math; // For Antares.Math.Array
using Antares.Method.Operator;

// Placeholders for dependent types. In a full project, these would be in their own files.
namespace Antares.Method.Operator
{
    /// <summary>
    /// Represents the layout of a multi-dimensional grid.
    /// </summary>
    public class FdmLinearOpLayout : IEnumerable<IFdmLinearOpIterator>
    {
        private readonly int[] _dim;
        public FdmLinearOpLayout(IEnumerable<int> dim) { _dim = dim.ToArray(); }
        public IReadOnlyList<int> Dim => _dim;
        public int Size => _dim.Aggregate(1, (a, b) => a * b);
        
        // This would be a real implementation of an iterator in a full port.
        public IEnumerator<IFdmLinearOpIterator> GetEnumerator() { throw new NotImplementedException(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return GetEnumerator(); }
    }

    /// <summary>
    /// Represents an iterator over the grid points of a layout.
    /// </summary>
    public interface IFdmLinearOpIterator
    {
        int Index { get; }
        IReadOnlyList<int> Coordinates { get; }
    }
}


namespace Antares.Method.Mesh
{
    /// <summary>
    /// FDM mesher which is a composite of multiple 1-D meshers.
    /// It builds a multi-dimensional grid from a set of one-dimensional grids.
    /// </summary>
    public class FdmMesherComposite : FdmMesher
    {
        private readonly IReadOnlyList<Fdm1dMesher> _meshers;

        #region Constructors
        /// <summary>
        /// Creates a composite mesher from a list of 1-D meshers and an explicit layout.
        /// </summary>
        /// <param name="layout">The layout of the composite grid.</param>
        /// <param name="meshers">The list of 1-D meshers for each dimension.</param>
        public FdmMesherComposite(FdmLinearOpLayout layout, IReadOnlyList<Fdm1dMesher> meshers)
            : base(layout)
        {
            _meshers = meshers;
            for (int i = 0; i < meshers.Count; ++i)
            {
                QL.Require(meshers[i].Size == layout.Dim[i],
                           $"Size of 1D mesher {i} does not fit to layout.");
            }
        }

        /// <summary>
        /// Creates a composite mesher from a list of 1-D meshers.
        /// The layout is inferred from the sizes of the individual meshers.
        /// </summary>
        /// <param name="meshers">The list of 1-D meshers for each dimension.</param>
        public FdmMesherComposite(IReadOnlyList<Fdm1dMesher> meshers)
            : base(GetLayoutFromMeshers(meshers))
        {
            _meshers = meshers;
        }

        /// <summary>
        /// Creates a composite mesher from a variable number of 1-D meshers.
        /// </summary>
        /// <param name="meshers">The 1-D meshers for each dimension.</param>
        public FdmMesherComposite(params Fdm1dMesher[] meshers)
            : this((IReadOnlyList<Fdm1dMesher>)meshers)
        {
        }
        #endregion

        /// <summary>
        /// Gets the underlying 1-D meshers.
        /// </summary>
        public IReadOnlyList<Fdm1dMesher> GetFdm1dMeshers() => _meshers;

        #region FdmMesher implementation
        public override double Dplus(IFdmLinearOpIterator iter, int direction)
        {
            return _meshers[direction].Dplus(iter.Coordinates[direction]);
        }

        public override double Dminus(IFdmLinearOpIterator iter, int direction)
        {
            return _meshers[direction].Dminus(iter.Coordinates[direction]);
        }

        public override double Location(IFdmLinearOpIterator iter, int direction)
        {
            return _meshers[direction].Location(iter.Coordinates[direction]);
        }

        public override Array Locations(int direction)
        {
            var retVal = new Array(Layout.Size);
            
            // This loop requires a functional FdmLinearOpLayout iterator.
            // The logic correctly translates the C++ range-based for loop.
            foreach (var iter in (FdmLinearOpLayout)Layout)
            {
                retVal[iter.Index] = _meshers[direction].Locations[iter.Coordinates[direction]];
            }

            return retVal;
        }
        #endregion

        private static FdmLinearOpLayout GetLayoutFromMeshers(IReadOnlyList<Fdm1dMesher> meshers)
        {
            var dim = meshers.Select(m => m.Size).ToList();
            return new FdmLinearOpLayout(dim);
        }
    }
}