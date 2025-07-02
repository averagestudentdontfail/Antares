// Gmres.cs

using System;
using System.Collections.Generic;
using Antares.Math; 
using static Antares.Math.Array; 

namespace Antares.Math.Matrix
{
    /// <summary>
    /// Result structure for the GMRES solver.
    /// </summary>
    public struct GMRESResult
    {
        /// <summary>
        /// A list of relative errors at each iteration.
        /// </summary>
        public List<double> Errors;

        /// <summary>
        /// The solution vector.
        /// </summary>
        public Array X;
    }

    /// <summary>
    /// Generalized Minimal Residual (GMRES) method for solving sparse linear systems.
    /// </summary>
    /// <remarks>
    /// This is an iterative method for solving systems of linear equations of the form Ax = b.
    /// It is particularly useful when the matrix A is large, sparse, and non-symmetric.
    /// The C# implementation follows the algorithm outlined in the original QuantLib C++ code.
    ///
    /// References:
    /// - Saad, Yousef. 1996, Iterative methods for sparse linear systems.
    /// - Dongarra et al. 1994, Templates for the Solution of Linear Systems.
    /// </remarks>
    public class GMRES
    {
        /// <summary>
        /// Delegate representing a matrix-vector multiplication operation.
        /// </summary>
        /// <param name="v">The input vector.</param>
        /// <returns>The result of the matrix-vector product.</returns>
        public delegate Array MatrixMult(Array v);

        private readonly MatrixMult _A;
        private readonly MatrixMult _M; // Preconditioner
        private readonly int _maxIter;
        private readonly double _relTol;

        /// <summary>
        /// Initializes a new instance of the GMRES solver.
        /// </summary>
        /// <param name="A">A function that performs the matrix-vector multiplication Ax.</param>
        /// <param name="maxIter">The maximum number of iterations for the Krylov subspace.</param>
        /// <param name="relTol">The desired relative tolerance for the solution.</param>
        /// <param name="preConditioner">An optional preconditioner function M. If provided, it should solve the system Mz = r for z.</param>
        public GMRES(MatrixMult A, int maxIter, double relTol, MatrixMult preConditioner = null)
        {
            QL.Require(maxIter > 0, "maxIter must be greater than zero");
            _A = A ?? throw new ArgumentNullException(nameof(A));
            _M = preConditioner; // Can be null
            _maxIter = maxIter;
            _relTol = relTol;
        }

        /// <summary>
        /// Solves the linear system Ax = b using the GMRES method.
        /// </summary>
        /// <param name="b">The right-hand side vector.</param>
        /// <param name="x0">An optional initial guess for the solution vector x. If not provided, a zero vector is used.</param>
        /// <returns>A <see cref="GMRESResult"/> containing the solution and convergence details.</returns>
        public GMRESResult Solve(Array b, Array x0 = null)
        {
            GMRESResult result = SolveImpl(b, x0);
            QL.Require(result.Errors[result.Errors.Count - 1] < _relTol, "could not converge");
            return result;
        }

        /// <summary>
        /// Solves the linear system Ax = b using the GMRES method with restarts.
        /// </summary>
        /// <param name="restart">The number of restarts to perform.</param>
        /// <param name="b">The right-hand side vector.</param>
        /// <param name="x0">An optional initial guess for the solution vector x.</param>
        /// <returns>A <see cref="GMRESResult"/> containing the solution and convergence details.</returns>
        public GMRESResult SolveWithRestart(int restart, Array b, Array x0 = null)
        {
            GMRESResult result = SolveImpl(b, x0);
            List<double> errors = new List<double>(result.Errors);

            for (int i = 0; i < restart - 1 && errors[errors.Count - 1] >= _relTol; ++i)
            {
                result = SolveImpl(b, result.X);
                errors.AddRange(result.Errors);
            }

            QL.Require(errors[errors.Count - 1] < _relTol, "could not converge");

            result.Errors = errors;
            return result;
        }

        protected GMRESResult SolveImpl(Array b, Array x0)
        {
            double bn = Norm2(b);
            if (bn == 0.0)
            {
                return new GMRESResult { Errors = new List<double> { 0.0 }, X = b };
            }

            Array x = (x0 != null && !x0.IsEmpty) ? x0.Clone() : new Array(b.Count);
            Array r = b - _A(x);

            double g = Norm2(r);
            if (g / bn < _relTol)
            {
                return new GMRESResult { Errors = new List<double> { g / bn }, X = x };
            }

            var v = new List<Array> { r / g };
            var h = new List<Array> { new Array(_maxIter) }; // Stores Hessenberg matrix columns
            var c = new double[_maxIter + 1];
            var s = new double[_maxIter + 1];
            var z = new double[_maxIter + 1];

            z[0] = g;
            var errors = new List<double> { g / bn };

            int j;
            for (j = 0; j < _maxIter && errors[errors.Count - 1] >= _relTol; ++j)
            {
                h.Add(new Array(_maxIter));
                Array w = _A(_M == null ? v[j] : _M(v[j]));

                for (int i = 0; i <= j; ++i)
                {
                    h[i][j] = DotProduct(w, v[i]);
                    w -= h[i][j] * v[i];
                }

                h[j + 1][j] = Norm2(w);

                if (h[j + 1][j] < QLDefines.EPSILON * QLDefines.EPSILON)
                    break;

                v.Add(w / h[j + 1][j]);

                for (int i = 0; i < j; ++i)
                {
                    double h0 = c[i] * h[i][j] + s[i] * h[i + 1][j];
                    double h1 = -s[i] * h[i][j] + c[i] * h[i + 1][j];
                    h[i][j] = h0;
                    h[i + 1][j] = h1;
                }

                double nu = System.Math.Sqrt(h[j][j] * h[j][j] + h[j + 1][j] * h[j + 1][j]);
                c[j] = h[j][j] / nu;
                s[j] = h[j + 1][j] / nu;
                h[j][j] = nu;
                h[j + 1][j] = 0.0;
                z[j + 1] = -s[j] * z[j];
                z[j] = c[j] * z[j];

                errors.Add(System.Math.Abs(z[j + 1] / bn));
            }

            int k = v.Count - 1;
            Array y = new Array(k);
            y[k - 1] = z[k - 1] / h[k - 1][k - 1];

            for (int i = k - 2; i >= 0; --i)
            {
                double sum = 0.0;
                for (int l = i + 1; l < k; ++l)
                {
                    sum += h[i][l] * y[l];
                }
                y[i] = (z[i] - sum) / h[i][i];
            }

            Array xm = new Array(x.Count); // Initialized to zero
            for (int i = 0; i < k; ++i)
            {
                xm += v[i] * y[i];
            }

            xm = x + (_M == null ? xm : _M(xm));

            return new GMRESResult { Errors = errors, X = xm };
        }
    }
}