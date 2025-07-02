// C# code for SecondDerivativeOp.cs

using System;

namespace Antares.Method.Operator
{
    /// <summary>
    /// Second derivative linear operator for finite difference methods.
    /// Implements second derivative approximation using finite differences
    /// with zero boundary conditions.
    /// </summary>
    /// <remarks>
    /// This operator constructs a tridiagonal matrix that approximates the second derivative
    /// operator ∂²/∂x² using finite differences. The treatment varies by location:
    /// 
    /// - At boundary points (first and last coordinates): Sets all coefficients to zero
    /// - At interior points: Uses centered differences on potentially non-uniform grids
    /// 
    /// For non-uniform grids, the coefficients are calculated using Lagrange interpolation
    /// to maintain second-order accuracy. The boundary treatment with zero coefficients
    /// is commonly used with Neumann boundary conditions or when boundary values 
    /// are handled separately in the overall scheme.
    /// 
    /// The second derivative approximation at interior point i is:
    /// ∂²u/∂x² ≈ 2/(hm*(hm+hp)) * u[i-1] - 2/(hm*hp) * u[i] + 2/(hp*(hm+hp)) * u[i+1]
    /// 
    /// where hm is the spacing to the left neighbor and hp is the spacing to the right neighbor.
    /// </remarks>
    public class SecondDerivativeOp : TripleBandLinearOp
    {
        /// <summary>
        /// Initializes a new instance of the SecondDerivativeOp class.
        /// </summary>
        /// <param name="direction">The spatial direction for the derivative operator.</param>
        /// <param name="mesher">The finite difference mesher defining the spatial grid.</param>
        public SecondDerivativeOp(Size direction, FdmMesher mesher)
            : base(direction, mesher)
        {
            if (mesher == null)
                throw new ArgumentNullException(nameof(mesher));

            // Calculate finite difference coefficients for each grid point
            foreach (var iter in mesher.Layout)
            {
                Size i = iter.Index;
                
                // Get grid spacing information
                Real hm = mesher.Dminus(iter, Direction); // spacing to left neighbor
                Real hp = mesher.Dplus(iter, Direction);  // spacing to right neighbor

                // Calculate denominators for finite difference approximation
                // These zeta values come from Lagrange interpolation on three points
                Real zetam1 = hm * (hm + hp);  // coefficient denominator for left point
                Real zeta0 = hm * hp;          // coefficient denominator for center point
                Real zetap1 = hp * (hm + hp);  // coefficient denominator for right point

                Size coord = (Size)iter.Coordinates[Direction];
                Size lastCoord = mesher.Layout.Dim[Direction] - 1;

                if (coord == 0 || coord == lastCoord)
                {
                    // Boundary points: Set all coefficients to zero
                    // This effectively treats the second derivative as zero at boundaries
                    // Common for Neumann boundary conditions or when boundaries are handled separately
                    Lower[i] = 0.0;
                    Diag[i] = 0.0;
                    Upper[i] = 0.0;
                }
                else
                {
                    // Interior points: Use centered finite differences for second derivative
                    // These coefficients come from Lagrange interpolation and provide
                    // second-order accuracy even on non-uniform grids
                    //
                    // The approximation is: ∂²u/∂x² ≈ lower*u[i-1] + diag*u[i] + upper*u[i+1]
                    Lower[i] = 2.0 / zetam1;   // coefficient for u[i-1]
                    Diag[i] = -2.0 / zeta0;    // coefficient for u[i] (always negative)
                    Upper[i] = 2.0 / zetap1;   // coefficient for u[i+1]
                }
            }
        }
    }
}