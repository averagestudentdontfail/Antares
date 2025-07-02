// Qrdecomposition.cs

using System;
using Antares.Math; 
using Antares.Math.Optimal.MINPACK; 
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Antares.Math.Matrix
{
    /// <summary>
    /// QR decomposition and solver functionality.
    /// </summary>
    public static class QRDecomposition
    {
        /// <summary>
        /// Computes the QR factorization of a matrix A such that A*P = Q*R.
        /// </summary>
        /// <remarks>
        /// This implementation replaces the original MINPACK-based logic with the
        /// robust and optimized QR decomposition from the MathNet.Numerics library.
        /// The `pivot` parameter is supported to maintain API compatibility, but
        /// MathNet's QR may handle pivoting internally differently.
        /// </remarks>
        /// <param name="A">The matrix to decompose.</param>
        /// <param name="q">Output: The orthogonal matrix Q.</param>
        /// <param name="r">Output: The upper trapezoidal/triangular matrix R.</param>
        /// <param name="pivot">Whether to use pivoting. (Supported for compatibility).</param>
        /// <returns>An integer array representing the permutation matrix P.</returns>
        public static int[] Decompose(Matrix A, out Matrix q, out Matrix r, bool pivot = true)
        {
            var qr = A._storage.QR(pivot ? QRMethod.Full : QRMethod.Thin);
            
            q = new Matrix(qr.Q);
            r = new Matrix(qr.R);

            // MathNet's QR factorization includes the permutation matrix P.
            // We return its permutation vector to match the MINPACK signature.
            return qr.P.Permutation;
        }

        /// <summary>
        /// Solves the least-squares problem Ax = b using QR decomposition.
        /// </summary>
        /// <remarks>
        /// This implementation replaces the original MINPACK-based `qrsolv` with a
        /// modern, direct solution using the QR decomposition from MathNet.Numerics.
        /// The optional diagonal matrix `d` for Tikhonov regularization is not directly
        /// supported by MathNet's QR.Solve but can be handled by augmenting the system.
        /// </remarks>
        /// <param name="a">The M-by-N matrix A.</param>
        /// <param name="b">The M-dimensional right-hand side vector b.</param>
        /// <param name="pivot">Whether to use pivoting in the decomposition.</param>
        /// <param name="d">An optional N-dimensional vector representing the diagonal of a regularization matrix D.</param>
        /// <returns>The solution vector x that minimizes ||Ax - b||^2 (+ ||Dx||^2 if d is provided).</returns>
        public static Array Solve(Matrix a, Array b, bool pivot = true, Array d = null)
        {
            QL.Require(b.Count == a.Rows, "Dimensions of A and b don't match.");
            QL.Require(d == null || d.IsEmpty || d.Count == a.Columns, "Dimensions of A and d don't match.");

            // If no regularization is applied, use the direct QR solver from MathNet.
            if (d == null || d.IsEmpty)
            {
                var qr = a._storage.QR(pivot ? QRMethod.Full : QRMethod.Thin);
                Vector<double> solutionVector = qr.Solve(b._storage);
                return new Array(solutionVector);
            }
            
            // If regularization is applied (d is not empty), we need to solve the augmented system:
            // [ A ]      [ b ]
            // [ D ] x =  [ 0 ]
            // where D is a diagonal matrix with elements from vector d.
            
            int m = a.Rows;
            int n = a.Columns;
            int augmentedRows = m + n;

            // Create the augmented matrix [A; D]
            var augmentedA = DenseMatrix.Create(augmentedRows, n, 0.0);
            augmentedA.SetSubMatrix(0, 0, a._storage);
            for (int i = 0; i < n; i++)
            {
                augmentedA[m + i, i] = d[i];
            }
            
            // Create the augmented vector [b; 0]
            var augmentedB = DenseVector.Create(augmentedRows, 0.0);
            augmentedB.SetSubVector(0, m, b._storage);
            
            // Solve the augmented system using QR decomposition
            var augmentedQr = augmentedA.QR(pivot ? QRMethod.Full : QRMethod.Thin);
            Vector<double> augmentedSolution = augmentedQr.Solve(augmentedB);

            return new Array(augmentedSolution);
        }
    }
}