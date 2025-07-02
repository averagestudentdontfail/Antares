// C# code for TqrEigenDecomposition.cs

using System;
using System.Collections.Generic;
using Antares.Math;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Antares.Math.Matrix
{
    /// <summary>
    /// Tridiagonal QR Eigendecomposition with implicit shift (Wilkinson shift).
    /// </summary>
    /// <remarks>
    /// This algorithm computes the eigenvalues and eigenvectors of a symmetric
    /// tridiagonal matrix. The C++ implementation, a direct translation of the
    /// Wilkinson algorithm, has been ported to C#.
    ///
    /// References:
    /// - Wilkinson, J.H. and Reinsch, C. 1971, Linear Algebra, vol. II of Handbook for Automatic Computation.
    /// - "Numerical Recipes in C", 2nd edition, Press, Teukolsky, Vetterling, Flannery.
    /// </remarks>
    public class TqrEigenDecomposition
    {
        public enum EigenVectorCalculation
        {
            WithEigenVector,
            WithoutEigenVector,
            OnlyFirstRowEigenVector
        }

        public enum ShiftStrategy
        {
            NoShift,
            Overrelaxation,
            CloseEigenValue
        }

        private readonly Array _d;
        private readonly Matrix _ev;
        private readonly int _iter = 0;

        /// <summary>
        /// Gets the eigenvalues of the tridiagonal matrix.
        /// </summary>
        public Array Eigenvalues => _d;

        /// <summary>
        /// Gets the eigenvectors of the tridiagonal matrix.
        /// </summary>
        public Matrix Eigenvectors => _ev;

        /// <summary>
        /// Gets the number of iterations performed.
        /// </summary>
        public int Iterations => _iter;

        /// <summary>
        /// Initializes a new instance of the TqrEigenDecomposition class and performs the decomposition.
        /// </summary>
        /// <param name="diag">The diagonal elements of the tridiagonal matrix.</param>
        /// <param name="sub">The sub-diagonal elements of the tridiagonal matrix.</param>
        /// <param name="calc">Specifies whether to calculate eigenvectors.</param>
        /// <param name="strategy">The shift strategy to use for convergence.</param>
        public TqrEigenDecomposition(Array diag, Array sub,
            EigenVectorCalculation calc = EigenVectorCalculation.WithEigenVector,
            ShiftStrategy strategy = ShiftStrategy.CloseEigenValue)
        {
            _d = diag.Clone();
            
            int evRows = (calc == EigenVectorCalculation.WithEigenVector) ? _d.Count :
                         (calc == EigenVectorCalculation.WithoutEigenVector) ? 0 : 1;
            
            _ev = new Matrix(evRows, _d.Count);

            int n = diag.Count;
            QL.Require(n == sub.Count + 1, "Wrong dimensions for diagonal and sub-diagonal arrays.");

            var e = new Array(n);
            for (int i = 1; i < n; ++i)
                e[i] = sub[i - 1];

            for (int i = 0; i < _ev.Rows; ++i)
            {
                _ev[i, i] = 1.0;
            }

            for (int k = n - 1; k >= 1; --k)
            {
                while (!OffDiagIsZero(k, e))
                {
                    int l = k;
                    while (--l > 0 && !OffDiagIsZero(l, e)) { } // NOLINT
                    _iter++;

                    double q = _d[l];
                    if (strategy != ShiftStrategy.NoShift)
                    {
                        // Calculated eigenvalue of 2x2 sub matrix which is closer to d[k].
                        double t1 = System.Math.Sqrt(0.25 * (_d[k] * _d[k] + _d[k - 1] * _d[k - 1])
                                                 - 0.5 * _d[k - 1] * _d[k] + e[k] * e[k]);
                        double t2 = 0.5 * (_d[k] + _d[k - 1]);

                        double lambda = (System.Math.Abs(t2 + t1 - _d[k]) < System.Math.Abs(t2 - t1 - _d[k]))
                            ? (t2 + t1)
                            : (t2 - t1);

                        if (strategy == ShiftStrategy.CloseEigenValue)
                        {
                            q -= lambda;
                        }
                        else // Overrelaxation
                        {
                            q -= ((k == n - 1) ? 1.25 : 1.0) * lambda;
                        }
                    }

                    // The QR transformation
                    double sine = 1.0;
                    double cosine = 1.0;
                    double u = 0.0;
                    bool recoverUnderflow = false;

                    for (int i = l + 1; i <= k && !recoverUnderflow; ++i)
                    {
                        double h = cosine * e[i];
                        double p = sine * e[i];

                        e[i - 1] = System.Math.Sqrt(p * p + q * q);
                        if (e[i - 1] != 0.0)
                        {
                            sine = p / e[i - 1];
                            cosine = q / e[i - 1];

                            double g = _d[i - 1] - u;
                            double t = (_d[i] - g) * sine + 2.0 * cosine * h;

                            u = sine * t;
                            _d[i - 1] = g + u;
                            q = cosine * t - h;

                            for (int j = 0; j < _ev.Rows; ++j)
                            {
                                double tmp = _ev[j, i - 1];
                                _ev[j, i - 1] = sine * _ev[j, i] + cosine * tmp;
                                _ev[j, i] = cosine * _ev[j, i] - sine * tmp;
                            }
                        }
                        else
                        {
                            // Recover from underflow
                            _d[i - 1] -= u;
                            e[l] = 0.0;
                            recoverUnderflow = true;
                        }
                    }

                    if (!recoverUnderflow)
                    {
                        _d[k] -= u;
                        e[k] = q;
                        e[l] = 0.0;
                    }
                }
            }

            // Sort eigenvalues and eigenvectors
            SortResults();
        }

        private void SortResults()
        {
            int n = _d.Count;
            var temp = new List<Tuple<double, Vector<double>>>(n);

            for (int i = 0; i < n; i++)
            {
                Vector<double> eigenVector;
                if (_ev.Rows > 0)
                {
                    eigenVector = _ev._storage.Column(i);
                }
                else
                {
                    eigenVector = Vector.Build.Dense(n); // Placeholder if no eigenvectors
                }
                temp.Add(new Tuple<double, Vector<double>>( _d[i], eigenVector));
            }
            
            // Sort in descending order of eigenvalues
            temp.Sort((a, b) => b.Item1.CompareTo(a.Item1));

            for (int i = 0; i < n; i++)
            {
                _d[i] = temp[i].Item1;
                
                if (_ev.Rows > 0)
                {
                    // Normalize sign (first element non-negative)
                    double sign = 1.0;
                    if (temp[i].Item2[0] < 0.0)
                        sign = -1.0;

                    for (int j = 0; j < _ev.Rows; ++j)
                    {
                        _ev[j, i] = sign * temp[i].Item2[j];
                    }
                }
            }
        }
        
        /// <summary>
        /// Checks if an off-diagonal element is effectively zero relative to its neighbors.
        /// See NR for the rationale behind this check.
        /// </summary>
        private bool OffDiagIsZero(int k, Array e)
        {
            // This floating point comparison trick is from the original source.
            // It checks if adding e[k] changes the sum of the absolute values of the diagonal neighbors.
            // If it doesn't (due to loss of precision), e[k] is considered zero.
            return (System.Math.Abs(_d[k - 1]) + System.Math.Abs(_d[k]))
                == (System.Math.Abs(_d[k - 1]) + System.Math.Abs(_d[k]) + System.Math.Abs(e[k]));
        }
    }
}