// ChebyshevInterpolation.cs

using System;
using Antares.Math;

namespace Antares.Math.Interpolation
{
    /// <summary>
    /// Chebyshev interpolation between discrete Chebyshev nodes.
    /// </summary>
    /// <remarks>
    /// This class uses a Barycentric Lagrange interpolation on a set of
    /// pre-defined Chebyshev nodes.
    ///
    /// Reference: S.A. Sarra: Chebyshev Interpolation: An Interactive Tour.
    /// </remarks>
    public class ChebyshevInterpolation : Interpolation, IUpdatedYInterpolation
    {
        public enum PointsType
        {
            FirstKind,
            SecondKind
        }

        private readonly Array _x;
        private Array _y;

        /// <summary>
        /// Initializes a new instance of the ChebyshevInterpolation class
        /// from a given set of y-values.
        /// The x-values (nodes) are generated automatically.
        /// </summary>
        /// <param name="y">The y-values at the Chebyshev nodes.</param>
        /// <param name="pointsType">The type of Chebyshev nodes to generate.</param>
        public ChebyshevInterpolation(Array y, PointsType pointsType = PointsType.SecondKind)
        {
            _x = Nodes(y.Count, pointsType);
            _y = y;

            InitializeImplementation();
        }

        /// <summary>
        /// Initializes a new instance of the ChebyshevInterpolation class
        /// by evaluating a function at the Chebyshev nodes.
        /// </summary>
        /// <param name="n">The number of nodes (and y-values) to generate.</param>
        /// <param name="f">The function to evaluate at the nodes.</param>
        /// <param name="pointsType">The type of Chebyshev nodes to generate.</param>
        public ChebyshevInterpolation(int n, Func<double, double> f, PointsType pointsType = PointsType.SecondKind)
            : this(ApplyFunction(Nodes(n, pointsType), f), pointsType)
        {
        }

        private void InitializeImplementation()
        {
            // The underlying implementation is a Lagrange interpolation.
            // We need to convert our Array types to double[] for the Lagrange constructor.
            var xArray = new double[_x.Count];
            var yArray = new double[_y.Count];
            for(int i=0; i < _x.Count; i++) xArray[i] = _x[i];
            for(int i=0; i < _y.Count; i++) yArray[i] = _y[i];

            _impl = new Detail.LagrangeInterpolationImpl(xArray, yArray);
            _impl.Update();
        }

        /// <summary>
        /// Updates the y-values for the interpolation.
        /// </summary>
        /// <param name="y">The new set of y-values. Must be the same size as the original.</param>
        public void UpdateY(Array y)
        {
            QL.Require(y.Count == _y.Count, "Interpolation override has the wrong length.");
            _y = y.Clone(); // Update internal y-values
            
            // Note: The LagrangeInterpolationImpl doesn't need to be fully updated
            // because the x-values and barycentric weights (lambda) have not changed.
            // We just need to update its internal y-array for the Value(x) method to work.
            if (_impl is Detail.LagrangeInterpolationImpl lagrangeImpl)
            {
                var newYArray = new double[y.Count];
                for (int i=0; i < y.Count; i++) newYArray[i] = y[i];
                Array.Copy(newYArray, lagrangeImpl.YValuesInternal, y.Count);
            }
        }
        
        /// <summary>
        /// Interpolates with a new set of y-values for a given x-value.
        /// </summary>
        /// <param name="y">The new y-values.</param>
        /// <param name="x">The x-value at which to interpolate.</param>
        /// <returns>The interpolated value.</returns>
        public double Value(Array y, double x)
        {
            // The implementation is directly castable to the required interface.
            return ((IUpdatedYInterpolation)_impl).Value(y, x);
        }

        /// <summary>
        /// Gets the Chebyshev nodes (x-values) used by this interpolation.
        /// </summary>
        public Array Nodes() => _x;

        /// <summary>
        /// Statically generates a set of Chebyshev nodes.
        /// </summary>
        /// <param name="n">The number of nodes to generate.</param>
        /// <param name="pointsType">The type of Chebyshev nodes to generate.</param>
        /// <returns>An Array containing the Chebyshev nodes.</returns>
        public static Array Nodes(int n, PointsType pointsType)
        {
            var t = new Array(n);
            switch (pointsType)
            {
                case PointsType.FirstKind:
                    for (int i = 0; i < n; ++i)
                        t[i] = -System.Math.Cos((i + 0.5) * System.Math.PI / n);
                    break;
                case PointsType.SecondKind:
                    if (n == 1)
                    {
                        t[0] = 0.0; // Avoid division by zero, though -cos(0) is -1. The QL logic is -cos(i*pi/(n-1)).
                                    // For n=1, this is division by zero. A single point at 0 is a reasonable convention.
                                    // However, the original code would fail. Let's stick to -cos(0) = -1.
                        t[0] = -1.0;
                    }
                    else
                    {
                        for (int i = 0; i < n; ++i)
                            t[i] = -System.Math.Cos(i * System.Math.PI / (n - 1));
                    }
                    break;
                default:
                    QL.Fail("Unknown Chebyshev interpolation points type.");
                    break;
            }
            return t;
        }

        private static Array ApplyFunction(Array x, Func<double, double> f)
        {
            var t = new Array(x.Count);
            for(int i = 0; i < x.Count; ++i)
            {
                t[i] = f(x[i]);
            }
            return t;
        }
    }
}

// In a real project, this internal implementation detail would be in its own file
// or hidden correctly. For now, let's ensure LagrangeInterpolationImpl has a way
// to update its y-values without a full reconstruction.
namespace Antares.Math.Interpolation.Detail
{
    internal partial class LagrangeInterpolationImpl // Using partial class to extend
    {
        // Exposing y-values for update by ChebyshevInterpolation
        internal double[] YValuesInternal => _y;
    }
}