// Symmetricschurdecomposition.cs

using System;
using Antares.Math; 
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Antares.Math.Matrix
{
    /// <summary>
    /// Computes the eigenvalues and eigenvectors of a real symmetric matrix.
    /// </summary>
    /// <remarks>
    /// Given a real symmetric matrix S, the Schur decomposition finds the
    /// eigenvalues and eigenvectors of S. If D is the diagonal matrix formed
    /// by the eigenvalues and U is the unitary matrix of the eigenvectors,
    /// we can write the Schur decomposition as S = U * D * U^T.
    ///
    /// This implementation replaces the original Jacobi algorithm with the robust
    /// and highly optimized Eigendecomposition from the MathNet.Numerics library.
    /// </remarks>
    public class SymmetricSchurDecomposition
    {
        private readonly Array _eigenvalues;
        private readonly Matrix _eigenvectors;

        /// <summary>
        /// Gets the eigenvalues of the matrix.
        /// </summary>
        public Array Eigenvalues => _eigenvalues;

        /// <summary>
        /// Gets the matrix whose columns are the eigenvectors of the original matrix.
        /// </summary>
        public Matrix Eigenvectors => _eigenvectors;

        /// <summary>
        /// Initializes a new instance of the SymmetricSchurDecomposition class
        /// and performs the decomposition.
        /// </summary>
        /// <param name="s">The symmetric matrix to decompose.</param>
        public SymmetricSchurDecomposition(Matrix s)
        {
            QL.Require(s.Rows > 0 && s.Columns > 0, "Null matrix given.");
            QL.Require(s.Rows == s.Columns, "Input matrix must be square.");
            
            // MathNet.Numerics provides a robust implementation for Eigendecomposition.
            var evd = s._storage.Evd(Symmetricity.Symmetric);

            // The eigenvalues are returned as a vector of Complex, but for a symmetric
            // real matrix, the imaginary parts will be zero. We take the real parts.
            var realEigenvalues = new double[s.Rows];
            for (int i = 0; i < s.Rows; i++)
            {
                realEigenvalues[i] = evd.EigenValues[i].Real;
            }
            _eigenvalues = new Array(realEigenvalues);

            // Eigenvectors are the columns of the returned matrix.
            _eigenvectors = new Matrix(evd.EigenVectors);

            SortDecomposition();
        }

        /// <summary>
        /// Sorts the eigenvalues and corresponding eigenvectors in descending order.
        /// Normalizes the sign of eigenvectors.
        /// </summary>
        private void SortDecomposition()
        {
            int size = _eigenvalues.Count;
            var sortedPairs = new List<Tuple<double, Vector<double>>>(size);

            for (int i = 0; i < size; i++)
            {
                sortedPairs.Add(new Tuple<double, Vector<double>>(
                    _eigenvalues[i], 
                    _eigenvectors._storage.Column(i)));
            }
            
            // Sort in descending order based on eigenvalues
            sortedPairs.Sort((a, b) => b.Item1.CompareTo(a.Item1));
            
            double maxEv = (size > 0) ? sortedPairs[0].Item1 : 0.0;
            if (maxEv == 0.0) maxEv = 1.0; // Avoid division by zero for zero matrix

            for (int col = 0; col < size; col++)
            {
                // Set eigenvalue, checking for round-off errors
                _eigenvalues[col] = (System.Math.Abs(sortedPairs[col].Item1 / maxEv) < 1e-16) 
                    ? 0.0 
                    : sortedPairs[col].Item1;

                // Normalize eigenvector sign (first element should be non-negative)
                var eigenvector = sortedPairs[col].Item2;
                if (eigenvector[0] < 0.0)
                {
                    eigenvector *= -1.0;
                }

                // Place the sorted and normalized eigenvector back into the matrix
                for (int row = 0; row < size; row++)
                {
                    _eigenvectors[row, col] = eigenvector[row];
                }
            }
        }
    }
}