using System;

namespace Antares.Interpolation
{
    /// <summary>
    /// Abstract base class for 1-D interpolations.
    /// Defines the contract for an interpolation scheme.
    /// </summary>
    public abstract class Interpolation
    {
        protected readonly double[] Y;

        protected Interpolation(double[] y)
        {
            Y = (double[])y.Clone();
        }

        /// <summary>
        /// Performs the interpolation at a given point.
        /// </summary>
        /// <param name="x">The point at which to interpolate. For many methods, this should be in a canonical range like [-1, 1].</param>
        /// <returns>The interpolated value.</returns>
        public abstract double Value(double x);

        /// <summary>
        /// Updates the function values (y-values) used for the interpolation.
        /// This is crucial for iterative schemes like the fixed-point engine.
        /// </summary>
        /// <param name="newY">The new array of function values. Must have the same length as the original.</param>
        public virtual void UpdateY(double[] newY)
        {
            if (newY.Length != Y.Length)
            {
                throw new ArgumentException("Input array 'newY' must have the same length as the original data.");
            }
            Array.Copy(newY, Y, newY.Length);
        }

        /// <summary>
        /// Returns a copy of the function values at the interpolation nodes.
        /// </summary>
        public double[] Values() => (double[])Y.Clone();
    }
}