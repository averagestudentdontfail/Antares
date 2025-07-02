// C# code for Lmdif.cs

using System;
using Accord.Math.Optimization;

namespace Antares.Math.Optimal
{
    /// <summary>
    /// Contains a C# wrapper for the MINPACK lmdif optimization routine.
    /// </summary>
    namespace MINPACK
    {
        /// <summary>
        /// Delegate for the cost function to be minimized by lmdif.
        /// This function calculates the vector of residuals.
        /// </summary>
        /// <param name="m">The number of functions (residuals).</param>
        /// <param name="n">The number of variables.</param>
        /// <param name="x">The current vector of variables.</param>
        /// <param name="fvec">The output vector of residuals.</param>
        /// <param name="iflag">A flag to signal termination. Set to a negative value to stop.</param>
        public delegate void LmdifCostFunction(int m, int n, Array x, Array fvec, ref int iflag);

        /// <summary>
        /// A C# wrapper for the MINPACK lmdif algorithm.
        /// </summary>
        /// <remarks>
        /// The original C/Fortran implementation is replaced by a modern, object-oriented
        /// Levenberg-Marquardt solver from the Accord.Math library. This wrapper provides
        /// a similar procedural API to the original MINPACK function to simplify porting
        /// of calling code, but with a simplified signature that omits workspace arrays.
        /// </remarks>
        public static class Lmdif
        {
            /// <summary>
            /// Minimizes the sum of the squares of m nonlinear functions in n variables
            /// by a modification of the Levenberg-Marquardt algorithm.
            /// </summary>
            /// <param name="m">Number of functions (residuals).</param>
            /// <param name="n">Number of variables.</param>
            /// <param name="x">On input, an initial estimate of the solution. On output, the final estimate.</param>
            /// <param name="fvec">On output, the residuals at the final solution.</param>
            /// <param name="ftol">Termination tolerance for the sum of squares.</param>
            /// <param name="xtol">Termination tolerance for the solution vector.</param>
            /// <param name="gtol">Termination tolerance for the orthogonality of the Jacobian and residuals.</param>
            /// <param name="maxfev">Maximum number of function evaluations.</param>
            /// <param name="epsfcn">Forward-difference step for numerical Jacobian. (Not used by this wrapper, Accord handles this internally).</param>
            /// <param name="diag">Scaling factors for variables. (Not used by this wrapper).</param>
            /// <param name="mode">Scaling mode. (Not used by this wrapper).</param>
            /// <param name="factor">Initial step bound factor. (Not used by this wrapper).</param>
            /// <param name="nprint">Print control. (Not used by this wrapper).</param>
            /// <param name="info">Output status code of the optimization.</param>
            /// <param name="nfev">Output number of function evaluations.</param>
            /// <param name="fcn">The cost function that computes the residuals.</param>
            /// <param name="jacFcn">An optional function to compute the Jacobian. If null, a numerical approximation is used.</param>
            public static void lmdif(int m, int n, Array x, out Array fvec,
                double ftol, double xtol, double gtol, int maxfev, double epsfcn,
                Array diag, int mode, double factor, int nprint,
                out int info, out int nfev,
                LmdifCostFunction fcn,
                LmdifCostFunction jacFcn = null)
            {
                // Validate inputs
                if (n <= 0 || m < n || ftol < 0.0 || xtol < 0.0 || gtol < 0.0 || maxfev <= 0)
                {
                    info = 0; // Improper input parameters
                    nfev = 0;
                    fvec = new Array(m);
                    return;
                }

                // Adapter for the Accord.Math cost function
                Func<double[], double[]> costAdapter = currentParams =>
                {
                    var qlX = new Array(currentParams);
                    var qlFvec = new Array(m);
                    int iflag = 0;
                    fcn(m, n, qlX, qlFvec, ref iflag);
                    if (iflag < 0)
                        throw new OperationCanceledException("Optimization terminated by user function.");
                    return qlFvec._storage.ToArray();
                };

                LevenbergMarquardt lm;

                // Adapter for the Accord.Math Jacobian function, if provided
                if (jacFcn != null)
                {
                    Func<double[], double[,]> jacobianAdapter = currentParams =>
                    {
                        var qlX = new Array(currentParams);
                        var qlFjac = new Array(m * n); // Jacobian flattened in column-major order
                        int iflag = 0;
                        jacFcn(m, n, qlX, qlFjac, ref iflag);
                        if (iflag < 0)
                            throw new OperationCanceledException("Optimization terminated by user function.");

                        // Reshape the flattened column-major array into a 2D row-major array for Accord
                        var accordJac = new double[m, n];
                        for (int j = 0; j < n; j++)
                        {
                            for (int i = 0; i < m; i++)
                            {
                                accordJac[i, j] = qlFjac[i + j * m];
                            }
                        }
                        return accordJac;
                    };
                    lm = new LevenbergMarquardt(n, costAdapter) { Gradient = jacobianAdapter };
                }
                else
                {
                    lm = new LevenbergMarquardt(n, costAdapter);
                }

                // Map control parameters to Accord solver properties
                lm.SolutionTolerance = ftol;
                lm.ParameterTolerance = xtol;
                lm.GradientTolerance = gtol;
                lm.MaxIterations = maxfev;

                try
                {
                    // Run the optimization
                    bool success = lm.Minimize(x._storage.ToArray());

                    // Map results back to output parameters
                    for (int i = 0; i < n; i++)
                        x[i] = lm.Solution[i];

                    fvec = new Array(lm.Value);
                    nfev = lm.Evaluations.Function;
                    info = MapStatusToInfoCode(lm.Status);
                }
                catch (OperationCanceledException)
                {
                    info = -1; // User-imposed termination
                    nfev = lm.Evaluations.Function;
                    fvec = new Array(m);
                }
                catch (Exception)
                {
                    info = 0; // Generic failure
                    nfev = lm.Evaluations.Function;
                    fvec = new Array(m);
                }
            }

            private static int MapStatusToInfoCode(ConvergenceStatus status)
            {
                switch (status)
                {
                    case ConvergenceStatus.Success:
                        return 1; // Corresponds to ftol, xtol, or gtol convergence
                    case ConvergenceStatus.MaximumIterations:
                        return 5;
                    case ConvergenceStatus.Stalled:
                        return 6; // ftol is too small
                    case ConvergenceStatus.Diverged:
                    default:
                        return 0; // Generic failure/improper input
                }
            }
        }
    }
}