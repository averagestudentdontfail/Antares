// C# code for FirstDerivativeOp.cs

using System;

namespace Antares.Method.Operator
{
    /// <summary>
    /// First derivative linear operator for finite difference methods.
    /// Implements first derivative approximation using finite differences
    /// with appropriate boundary conditions (upwinding/downwinding schemes).
    /// </summary>
    /// <remarks>
    /// This operator constructs a tridiagonal matrix that approximates the first derivative
    /// operator ∂/∂x using finite differences. The stencil adapts to boundary conditions:
    /// 
    /// - At the left boundary (coordinate 0): Uses upwinding scheme
    /// - At the right boundary (last coordinate): Uses downwinding scheme  
    /// - At interior points: Uses centered differences on potentially non-uniform grids
    /// 
    /// For non-uniform grids, the coefficients are calculated to maintain second-order
    /// accuracy in the interior while ensuring stability at boundaries.
    /// </remarks>
    public class FirstDerivativeOp : TripleBandLinearOp
    {
        /// <summary>
        /// Initializes a new instance of the FirstDerivativeOp class.
        /// </summary>
        /// <param name="direction">The spatial direction for the derivative operator.</param>
        /// <param name="mesher">The finite difference mesher defining the spatial grid.</param>
        public FirstDerivativeOp(Size direction, FdmMesher mesher)
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
                // These zeta values come from the finite difference approximation on non-uniform grids
                Real zetam1 = hm * (hm + hp);  // coefficient denominator for left point
                Real zeta0 = hm * hp;          // coefficient denominator for center point
                Real zetap1 = hp * (hm + hp);  // coefficient denominator for right point

                Size coord = (Size)iter.Coordinates[Direction];
                Size lastCoord = mesher.Layout.Dim[Direction] - 1;

                if (coord == 0)
                {
                    // Left boundary: Use upwinding scheme (forward difference)
                    // Approximates ∂u/∂x ≈ (u[i+1] - u[i])/hp
                    Lower[i] = 0.0;
                    Diag[i] = -1.0 / hp;
                    Upper[i] = 1.0 / hp;
                }
                else if (coord == lastCoord)
                {
                    // Right boundary: Use downwinding scheme (backward difference)
                    // Approximates ∂u/∂x ≈ (u[i] - u[i-1])/hm
                    Lower[i] = -1.0 / hm;
                    Diag[i] = 1.0 / hm;
                    Upper[i] = 0.0;
                }
                else
                {
                    // Interior points: Use centered finite differences on non-uniform grid
                    // These coefficients come from Lagrange interpolation on three points
                    // and provide second-order accuracy even on non-uniform grids
                    Lower[i] = -hp / zetam1;
                    Diag[i] = (hp - hm) / zeta0;
                    Upper[i] = hm / zetap1;
                }
            }
        }
    }
}