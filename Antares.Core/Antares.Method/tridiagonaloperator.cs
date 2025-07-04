// C# code for TridiagonalOperator.cs

using System;

namespace Antares.Method
{
    /// <summary>
    /// Interface for time-setting logic in tridiagonal operators
    /// </summary>
    public interface ITimeSetter
    {
        void SetTime(Time t, TridiagonalOperator operatorL);
    }

    /// <summary>
    /// Base implementation for tridiagonal operator
    /// </summary>
    /// <remarks>
    /// Warning: to use real time-dependent algebra, you must override
    /// the corresponding operators in the inheriting time-dependent class.
    /// </remarks>
    public class TridiagonalOperator
    {
        protected Size _n;
        protected Array _diagonal;
        protected Array _lowerDiagonal; 
        protected Array _upperDiagonal;
        protected Array _temp;
        protected ITimeSetter _timeSetter;

        #region Properties
        /// <summary>
        /// Gets the size of the operator
        /// </summary>
        public Size Size => _n;

        /// <summary>
        /// Gets whether this operator is time-dependent
        /// </summary>
        public bool IsTimeDependent => _timeSetter != null;

        /// <summary>
        /// Gets the lower diagonal elements
        /// </summary>
        public Array LowerDiagonal => _lowerDiagonal;

        /// <summary>
        /// Gets the main diagonal elements
        /// </summary>
        public Array Diagonal => _diagonal;

        /// <summary>
        /// Gets the upper diagonal elements
        /// </summary>
        public Array UpperDiagonal => _upperDiagonal;
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a tridiagonal operator of given size
        /// </summary>
        /// <param name="size">The size of the operator</param>
        public TridiagonalOperator(Size size = 0)
        {
            if (size >= 2)
            {
                _n = size;
                _diagonal = new Array((int)size);
                _lowerDiagonal = new Array((int)size - 1);
                _upperDiagonal = new Array((int)size - 1);
                _temp = new Array((int)size);
            }
            else if (size == 0)
            {
                _n = 0;
                _diagonal = new Array(0);
                _lowerDiagonal = new Array(0);
                _upperDiagonal = new Array(0);
                _temp = new Array(0);
            }
            else
            {
                QL.Fail($"invalid size ({size}) for tridiagonal operator (must be null or >= 2)");
            }
        }

        /// <summary>
        /// Constructs a tridiagonal operator from diagonal arrays
        /// </summary>
        /// <param name="low">Lower diagonal elements</param>
        /// <param name="mid">Main diagonal elements</param>
        /// <param name="high">Upper diagonal elements</param>
        public TridiagonalOperator(Array low, Array mid, Array high)
        {
            _n = (Size)mid.Count;
            _diagonal = mid.Clone();
            _lowerDiagonal = low.Clone();
            _upperDiagonal = high.Clone();
            _temp = new Array((int)_n);

            QL.Require(low.Count == _n - 1,
                $"low diagonal vector of size {low.Count} instead of {_n - 1}");
            QL.Require(high.Count == _n - 1,
                $"high diagonal vector of size {high.Count} instead of {_n - 1}");
        }
        #endregion

        #region Operator Interface
        /// <summary>
        /// Apply operator to a given array
        /// </summary>
        /// <param name="v">The array to apply the operator to</param>
        /// <returns>The result of the operation</returns>
        public Array ApplyTo(Array v)
        {
            QL.Require(_n != 0, "uninitialized TridiagonalOperator");
            QL.Require(v.Count == _n,
                $"vector of the wrong size {v.Count} instead of {_n}");

            var result = new Array((int)_n);
            
            // Element-wise multiplication of diagonal and v
            for (int i = 0; i < _n; i++)
            {
                result[i] = _diagonal[i] * v[i];
            }

            // Matricial product
            result[0] += _upperDiagonal[0] * v[1];
            for (Size j = 1; j <= _n - 2; j++)
            {
                result[j] += _lowerDiagonal[j - 1] * v[j - 1] +
                            _upperDiagonal[j] * v[j + 1];
            }
            result[_n - 1] += _lowerDiagonal[_n - 2] * v[_n - 2];

            return result;
        }

        /// <summary>
        /// Solve linear system for a given right-hand side
        /// </summary>
        /// <param name="rhs">The right-hand side vector</param>
        /// <returns>The solution vector</returns>
        public Array SolveFor(Array rhs)
        {
            var result = new Array(rhs.Count);
            SolveFor(rhs, result);
            return result;
        }

        /// <summary>
        /// Solve linear system for a given right-hand side without result Array allocation
        /// </summary>
        /// <param name="rhs">The right-hand side vector</param>
        /// <param name="result">The solution vector (can be the same as rhs)</param>
        public void SolveFor(Array rhs, Array result)
        {
            QL.Require(_n != 0, "uninitialized TridiagonalOperator");
            QL.Require(rhs.Count == _n,
                $"rhs vector of size {rhs.Count} instead of {_n}");

            Real bet = _diagonal[0];
            QL.Require(!Comparison.Close(bet, 0.0),
                $"diagonal's first element ({bet}) cannot be close to zero");
            
            result[0] = rhs[0] / bet;
            for (Size j = 1; j <= _n - 1; ++j)
            {
                _temp[j] = _upperDiagonal[j - 1] / bet;
                bet = _diagonal[j] - _lowerDiagonal[j - 1] * _temp[j];
                QL.Ensure(!Comparison.Close(bet, 0.0), "division by zero");
                result[j] = (rhs[j] - _lowerDiagonal[j - 1] * result[j - 1]) / bet;
            }
            
            // Backsubstitution - cannot be j >= 0 with Size j
            for (Size j = _n - 2; j > 0; --j)
            {
                result[j] -= _temp[j + 1] * result[j + 1];
            }
            result[0] -= _temp[1] * result[1];
        }

        /// <summary>
        /// Solve linear system with SOR (Successive Over-Relaxation) approach
        /// </summary>
        /// <param name="rhs">The right-hand side vector</param>
        /// <param name="tol">The tolerance for convergence</param>
        /// <returns>The solution vector</returns>
        public Array SOR(Array rhs, Real tol)
        {
            QL.Require(_n != 0, "uninitialized TridiagonalOperator");
            QL.Require(rhs.Count == _n,
                $"rhs vector of size {rhs.Count} instead of {_n}");

            // Initial guess
            var result = rhs.Clone();

            // Solve tridiagonal system with SOR technique
            Real omega = 1.5;
            Real err = 2.0 * tol;
            Real temp;
            
            for (Size sorIteration = 0; err > tol; ++sorIteration)
            {
                QL.Require(sorIteration < 100000,
                    $"tolerance ({tol}) not reached in {sorIteration} iterations. " +
                    $"The error still is {err}");

                temp = omega * (rhs[0] -
                               _upperDiagonal[0] * result[1] -
                               _diagonal[0] * result[0]) / _diagonal[0];
                err = temp * temp;
                result[0] += temp;
                
                Size i;
                for (i = 1; i < _n - 1; ++i)
                {
                    temp = omega * (rhs[i] -
                                   _upperDiagonal[i] * result[i + 1] -
                                   _diagonal[i] * result[i] -
                                   _lowerDiagonal[i - 1] * result[i - 1]) / _diagonal[i];
                    err += temp * temp;
                    result[i] += temp;
                }

                temp = omega * (rhs[i] -
                               _diagonal[i] * result[i] -
                               _lowerDiagonal[i - 1] * result[i - 1]) / _diagonal[i];
                err += temp * temp;
                result[i] += temp;
            }
            return result;
        }

        /// <summary>
        /// Creates an identity tridiagonal operator of given size
        /// </summary>
        /// <param name="size">The size of the identity operator</param>
        /// <returns>An identity tridiagonal operator</returns>
        public static TridiagonalOperator Identity(Size size)
        {
            return new TridiagonalOperator(
                new Array((int)size - 1, 0.0),  // lower diagonal
                new Array((int)size, 1.0),      // diagonal  
                new Array((int)size - 1, 0.0)); // upper diagonal
        }
        #endregion

        #region Modifiers
        /// <summary>
        /// Sets the first row of the operator
        /// </summary>
        /// <param name="valB">Diagonal element</param>
        /// <param name="valC">Upper diagonal element</param>
        public void SetFirstRow(Real valB, Real valC)
        {
            _diagonal[0] = valB;
            _upperDiagonal[0] = valC;
        }

        /// <summary>
        /// Sets a middle row of the operator
        /// </summary>
        /// <param name="i">Row index</param>
        /// <param name="valA">Lower diagonal element</param>
        /// <param name="valB">Diagonal element</param>
        /// <param name="valC">Upper diagonal element</param>
        public void SetMidRow(Size i, Real valA, Real valB, Real valC)
        {
            QL.Require(i >= 1 && i <= _n - 2,
                "out of range in TridiagonalOperator.SetMidRow");
            _lowerDiagonal[i - 1] = valA;
            _diagonal[i] = valB;
            _upperDiagonal[i] = valC;
        }

        /// <summary>
        /// Sets all middle rows of the operator to the same values
        /// </summary>
        /// <param name="valA">Lower diagonal element</param>
        /// <param name="valB">Diagonal element</param>
        /// <param name="valC">Upper diagonal element</param>
        public void SetMidRows(Real valA, Real valB, Real valC)
        {
            for (Size i = 1; i <= _n - 2; i++)
            {
                _lowerDiagonal[i - 1] = valA;
                _diagonal[i] = valB;
                _upperDiagonal[i] = valC;
            }
        }

        /// <summary>
        /// Sets the last row of the operator
        /// </summary>
        /// <param name="valA">Lower diagonal element</param>
        /// <param name="valB">Diagonal element</param>
        public void SetLastRow(Real valA, Real valB)
        {
            _lowerDiagonal[_n - 2] = valA;
            _diagonal[_n - 1] = valB;
        }

        /// <summary>
        /// Sets the time for time-dependent operators
        /// </summary>
        /// <param name="t">The time to set</param>
        public void SetTime(Time t)
        {
            _timeSetter?.SetTime(t, this);
        }
        #endregion

        #region Operator Overloads
        public static TridiagonalOperator operator +(TridiagonalOperator d)
        {
            return new TridiagonalOperator(d._lowerDiagonal.Clone(), 
                                         d._diagonal.Clone(), 
                                         d._upperDiagonal.Clone());
        }

        public static TridiagonalOperator operator -(TridiagonalOperator d)
        {
            var low = -d._lowerDiagonal;
            var mid = -d._diagonal;
            var high = -d._upperDiagonal;
            return new TridiagonalOperator(low, mid, high);
        }

        public static TridiagonalOperator operator +(TridiagonalOperator d1, TridiagonalOperator d2)
        {
            var low = d1._lowerDiagonal + d2._lowerDiagonal;
            var mid = d1._diagonal + d2._diagonal;
            var high = d1._upperDiagonal + d2._upperDiagonal;
            return new TridiagonalOperator(low, mid, high);
        }

        public static TridiagonalOperator operator -(TridiagonalOperator d1, TridiagonalOperator d2)
        {
            var low = d1._lowerDiagonal - d2._lowerDiagonal;
            var mid = d1._diagonal - d2._diagonal;
            var high = d1._upperDiagonal - d2._upperDiagonal;
            return new TridiagonalOperator(low, mid, high);
        }

        public static TridiagonalOperator operator *(Real a, TridiagonalOperator d)
        {
            var low = d._lowerDiagonal * a;
            var mid = d._diagonal * a;
            var high = d._upperDiagonal * a;
            return new TridiagonalOperator(low, mid, high);
        }

        public static TridiagonalOperator operator *(TridiagonalOperator d, Real a)
        {
            var low = d._lowerDiagonal * a;
            var mid = d._diagonal * a;
            var high = d._upperDiagonal * a;
            return new TridiagonalOperator(low, mid, high);
        }

        public static TridiagonalOperator operator /(TridiagonalOperator d, Real a)
        {
            var low = d._lowerDiagonal / a;
            var mid = d._diagonal / a;
            var high = d._upperDiagonal / a;
            return new TridiagonalOperator(low, mid, high);
        }
        #endregion
    }
}