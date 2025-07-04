// BiCGStab.cs

using System;
using Antares.Math; // For Antares.Math.Array
using static Antares.Math.Array; // For static methods like DotProduct, Norm2

namespace Antares.Math.Matrix
{
    /// <summary>
    /// Result structure for the BiCGstab solver.
    /// </summary>
    public struct BiCGStabResult
    {
        /// <summary>
        /// The number of iterations performed.
        /// </summary>
        public int Iterations;

        /// <summary>
        /// The final relative error.
        /// </summary>
        public double Error;

        /// <summary>
        /// The solution vector.
        /// </summary>
        public Array X;
    }

    /// <summary>
    /// Biconjugate gradient stabilized (BiCGSTAB) method for solving sparse linear systems.
    /// </summary>
    /// <remarks>
    /// This is an iterative method for solving systems of linear equations of the form Ax = b.
    /// It is particularly useful when the matrix A is large and sparse.
    /// The C# implementation follows the algorithm outlined in the original QuantLib C++ code.
    /// </remarks>
    public class BiCGstab
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
        /// Initializes a new instance of the BiCGstab solver.
        /// </summary>
        /// <param name="A">A function that performs the matrix-vector multiplication Ax.</param>
        /// <param name="maxIter">The maximum number of iterations allowed.</param>
        /// <param name="relTol">The desired relative tolerance for the solution.</param>
        /// <param name="preConditioner">An optional preconditioner function M. If provided, it should solve the system Mz = r for z.</param>
        public BiCGstab(MatrixMult A, int maxIter, double relTol, MatrixMult preConditioner = null)
        {
            _A = A ?? throw new ArgumentNullException(nameof(A));
            _M = preConditioner; // Can be null
            _maxIter = maxIter;
            _relTol = relTol;
        }

        /// <summary>
        /// Solves the linear system Ax = b using the BiCGSTAB method.
        /// </summary>
        /// <param name="b">The right-hand side vector.</param>
        /// <param name="x0">An optional initial guess for the solution vector x. If not provided, a zero vector is used.</param>
        /// <returns>A <see cref="BiCGStabResult"/> containing the solution and convergence details.</returns>
        public BiCGStabResult Solve(Array b, Array x0 = null)
        {
            double bnorm2 = Norm2(b);
            if (bnorm2 == 0.0)
            {
                return new BiCGStabResult { Iterations = 0, Error = 0.0, X = b };
            }

            Array x = (x0 != null && !x0.IsEmpty) ? x0.Clone() : new Array(b.Count);
            Array r = b - _A(x);

            Array rTld = r.Clone();
            Array p = null, v = null, s = null, t = null;
            double omega = 1.0;
            double rho, rhoTld = 1.0;
            double alpha = 0.0, beta;
            double error = Norm2(r) / bnorm2;

            int i;
            for (i = 0; i < _maxIter && error >= _relTol; ++i)
            {
                rho = DotProduct(rTld, r);
                if (rho == 0.0 || omega == 0.0)
                    break;

                if (i != 0)
                {
                    beta = (rho / rhoTld) * (alpha / omega);
                    p = r + beta * (p - omega * v);
                }
                else
                {
                    p = r.Clone();
                }

                Array pTld = (_M == null) ? p : _M(p);
                v = _A(pTld);

                alpha = rho / DotProduct(rTld, v);
                s = r - alpha * v;
                if (Norm2(s) < _relTol * bnorm2)
                {
                    x += alpha * pTld;
                    error = Norm2(s) / bnorm2;
                    break;
                }

                Array sTld = (_M == null) ? s : _M(s);
                t = _A(sTld);
                omega = DotProduct(t, s) / DotProduct(t, t);
                x += alpha * pTld + omega * sTld;
                r = s - omega * t;
                error = Norm2(r) / bnorm2;
                rhoTld = rho;
            }

            QL.Require(i < _maxIter, "max number of iterations exceeded");
            QL.Require(error < _relTol, "could not converge");

            return new BiCGStabResult { Iterations = i, Error = error, X = x };
        }
    }
}