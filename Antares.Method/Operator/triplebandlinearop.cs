// C# code for TripleBandLinearOp.cs

using System;
using System.Linq;
using Antares.Math;
using Antares.Math.Matrix;

namespace Antares.Method.Operator
{
    /// <summary>
    /// General triple band linear operator for finite difference methods.
    /// This operator represents a tridiagonal matrix structure for operations
    /// along a specific direction in a finite difference mesh.
    /// </summary>
    public class TripleBandLinearOp : FdmLinearOp
    {
        protected Size _direction;
        protected Size[] _i0, _i2;
        protected Size[] _reverseIndex;
        protected Real[] _lower, _diag, _upper;
        protected FdmMesher _mesher;

        /// <summary>
        /// Default constructor.
        /// </summary>
        protected TripleBandLinearOp() { }

        /// <summary>
        /// Constructs a triple band linear operator for the specified direction.
        /// </summary>
        /// <param name="direction">The direction along which the operator acts.</param>
        /// <param name="mesher">The finite difference mesher.</param>
        public TripleBandLinearOp(Size direction, FdmMesher mesher)
        {
            _direction = direction;
            _mesher = mesher;
            
            var size = mesher.Layout.Size;
            _i0 = new Size[size];
            _i2 = new Size[size];
            _reverseIndex = new Size[size];
            _lower = new Real[size];
            _diag = new Real[size];
            _upper = new Real[size];

            var newDim = mesher.Layout.Dim.ToArray();
            // Swap first element with direction element
            (newDim[0], newDim[direction]) = (newDim[direction], newDim[0]);
            
            var newSpacing = new FdmLinearOpLayout(newDim).Spacing.ToArray();
            (newSpacing[0], newSpacing[direction]) = (newSpacing[direction], newSpacing[0]);

            foreach (var iter in mesher.Layout)
            {
                var i = iter.Index;
                
                _i0[i] = mesher.Layout.Neighbourhood(iter, direction, -1);
                _i2[i] = mesher.Layout.Neighbourhood(iter, direction, 1);

                var coordinates = iter.Coordinates;
                var newIndex = coordinates.Zip(newSpacing, (coord, spacing) => coord * spacing).Sum();
                _reverseIndex[newIndex] = i;
            }
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="m">The operator to copy.</param>
        public TripleBandLinearOp(TripleBandLinearOp m)
        {
            _direction = m._direction;
            _mesher = m._mesher;
            
            var len = m._mesher.Layout.Size;
            _i0 = new Size[len];
            _i2 = new Size[len];
            _reverseIndex = new Size[len];
            _lower = new Real[len];
            _diag = new Real[len];
            _upper = new Real[len];

            Array.Copy(m._i0, _i0, len);
            Array.Copy(m._i2, _i2, len);
            Array.Copy(m._reverseIndex, _reverseIndex, len);
            Array.Copy(m._lower, _lower, len);
            Array.Copy(m._diag, _diag, len);
            Array.Copy(m._upper, _upper, len);
        }

        /// <summary>
        /// Swaps the contents of this operator with another.
        /// </summary>
        /// <param name="m">The operator to swap with.</param>
        public void Swap(TripleBandLinearOp m)
        {
            (_mesher, m._mesher) = (m._mesher, _mesher);
            (_direction, m._direction) = (m._direction, _direction);
            (_i0, m._i0) = (m._i0, _i0);
            (_i2, m._i2) = (m._i2, _i2);
            (_reverseIndex, m._reverseIndex) = (m._reverseIndex, _reverseIndex);
            (_lower, m._lower) = (m._lower, _lower);
            (_diag, m._diag) = (m._diag, _diag);
            (_upper, m._upper) = (m._upper, _upper);
        }

        /// <summary>
        /// Performs the linear combination operation: this = a*x + y + b.
        /// </summary>
        /// <param name="a">Coefficient array for x (can be empty for scalar 1).</param>
        /// <param name="x">First operator.</param>
        /// <param name="y">Second operator.</param>
        /// <param name="b">Additive term array (can be empty for zero).</param>
        public void Axpyb(Array a, TripleBandLinearOp x, TripleBandLinearOp y, Array b)
        {
            var size = _mesher.Layout.Size;

            if (a.IsEmpty)
            {
                if (b.IsEmpty)
                {
                    // this = y
                    for (Size i = 0; i < size; ++i)
                    {
                        _diag[i] = y._diag[i];
                        _lower[i] = y._lower[i];
                        _upper[i] = y._upper[i];
                    }
                }
                else
                {
                    // this = y + b
                    var binc = (b.Count > 1) ? 1 : 0;
                    for (Size i = 0; i < size; ++i)
                    {
                        _diag[i] = y._diag[i] + b[i * binc];
                        _lower[i] = y._lower[i];
                        _upper[i] = y._upper[i];
                    }
                }
            }
            else if (b.IsEmpty)
            {
                // this = a*x + y
                var ainc = (a.Count > 1) ? 1 : 0;
                for (Size i = 0; i < size; ++i)
                {
                    var s = a[i * ainc];
                    _diag[i] = y._diag[i] + s * x._diag[i];
                    _lower[i] = y._lower[i] + s * x._lower[i];
                    _upper[i] = y._upper[i] + s * x._upper[i];
                }
            }
            else
            {
                // this = a*x + y + b
                var binc = (b.Count > 1) ? 1 : 0;
                var ainc = (a.Count > 1) ? 1 : 0;
                for (Size i = 0; i < size; ++i)
                {
                    var s = a[i * ainc];
                    _diag[i] = y._diag[i] + s * x._diag[i] + b[i * binc];
                    _lower[i] = y._lower[i] + s * x._lower[i];
                    _upper[i] = y._upper[i] + s * x._upper[i];
                }
            }
        }

        /// <summary>
        /// Adds another triple band operator.
        /// </summary>
        /// <param name="m">The operator to add.</param>
        /// <returns>A new operator representing the sum.</returns>
        public TripleBandLinearOp Add(TripleBandLinearOp m)
        {
            var retVal = new TripleBandLinearOp(_direction, _mesher);
            var size = _mesher.Layout.Size;
            
            for (Size i = 0; i < size; ++i)
            {
                retVal._lower[i] = _lower[i] + m._lower[i];
                retVal._diag[i] = _diag[i] + m._diag[i];
                retVal._upper[i] = _upper[i] + m._upper[i];
            }

            return retVal;
        }

        /// <summary>
        /// Multiplies the operator by a scalar array (left multiplication).
        /// </summary>
        /// <param name="u">The scalar array.</param>
        /// <returns>A new operator representing the product.</returns>
        public TripleBandLinearOp Mult(Array u)
        {
            var retVal = new TripleBandLinearOp(_direction, _mesher);
            var size = _mesher.Layout.Size;
            
            for (Size i = 0; i < size; ++i)
            {
                var s = u[i];
                retVal._lower[i] = _lower[i] * s;
                retVal._diag[i] = _diag[i] * s;
                retVal._upper[i] = _upper[i] * s;
            }

            return retVal;
        }

        /// <summary>
        /// Multiplies the operator by a diagonal matrix on the right.
        /// Interprets u as the diagonal of a diagonal matrix, multiplied on RHS.
        /// </summary>
        /// <param name="u">The diagonal elements.</param>
        /// <returns>A new operator representing the product.</returns>
        public TripleBandLinearOp MultR(Array u)
        {
            var size = _mesher.Layout.Size;
            QL.Require(u.Count == size, "inconsistent size of rhs");
            var retVal = new TripleBandLinearOp(_direction, _mesher);

            for (int i = 0; i < size; ++i)
            {
                var sm1 = i > 0 ? u[i - 1] : 1.0;
                var s0 = u[i];
                var sp1 = i < size - 1 ? u[i + 1] : 1.0;
                retVal._lower[i] = _lower[i] * sm1;
                retVal._diag[i] = _diag[i] * s0;
                retVal._upper[i] = _upper[i] * sp1;
            }

            return retVal;
        }

        /// <summary>
        /// Adds a diagonal matrix to the operator.
        /// </summary>
        /// <param name="u">The diagonal elements to add.</param>
        /// <returns>A new operator representing the sum.</returns>
        public TripleBandLinearOp Add(Array u)
        {
            var retVal = new TripleBandLinearOp(_direction, _mesher);
            var size = _mesher.Layout.Size;
            
            for (Size i = 0; i < size; ++i)
            {
                retVal._lower[i] = _lower[i];
                retVal._upper[i] = _upper[i];
                retVal._diag[i] = _diag[i] + u[i];
            }

            return retVal;
        }

        /// <summary>
        /// Applies the operator to an array.
        /// </summary>
        /// <param name="r">The input array.</param>
        /// <returns>The result of applying the operator.</returns>
        public override Array Apply(Array r)
        {
            QL.Require(r.Count == _mesher.Layout.Size, "inconsistent length of r");

            var retVal = new Array(r.Count);
            for (Size i = 0; i < _mesher.Layout.Size; ++i)
            {
                retVal[i] = r[_i0[i]] * _lower[i] + r[i] * _diag[i] + r[_i2[i]] * _upper[i];
            }

            return retVal;
        }

        /// <summary>
        /// Converts the operator to a sparse matrix representation.
        /// </summary>
        /// <returns>The sparse matrix representation.</returns>
        public override SparseMatrix ToMatrix()
        {
            var n = _mesher.Layout.Size;

            var retVal = new SparseMatrix(n, n);
            for (Size i = 0; i < n; ++i)
            {
                retVal[i, _i0[i]] += _lower[i];
                retVal[i, i] += _diag[i];
                retVal[i, _i2[i]] += _upper[i];
            }

            return retVal;
        }

        /// <summary>
        /// Solves a tridiagonal system using operator splitting.
        /// Uses the Thomas algorithm to solve the system.
        /// </summary>
        /// <param name="r">The right-hand side vector.</param>
        /// <param name="a">Scaling factor for the operator.</param>
        /// <param name="b">Scaling factor for the identity (default 1.0).</param>
        /// <returns>The solution vector.</returns>
        public Array SolveSplitting(Array r, Real a, Real b = 1.0)
        {
            QL.Require(r.Count == _mesher.Layout.Size, "inconsistent size of rhs");

#if QL_EXTRA_SAFETY_CHECKS
            foreach (var iter in _mesher.Layout)
            {
                var coordinates = iter.Coordinates;
                QL.Require(coordinates[_direction] != 0 || _lower[iter.Index] == 0,
                          "removing non zero entry!");
                QL.Require(coordinates[_direction] != _mesher.Layout.Dim[_direction] - 1 || 
                          _upper[iter.Index] == 0,
                          "removing non zero entry!");
            }
#endif

            var retVal = new Array(r.Count);
            var tmp = new Array(r.Count);

            // Thomas algorithm to solve a tridiagonal system
            var rim1 = _reverseIndex[0];
            var bet = 1.0 / (a * _diag[rim1] + b);
            QL.Require(bet != 0.0, "division by zero");
            retVal[_reverseIndex[0]] = r[rim1] * bet;

            for (Size j = 1; j <= _mesher.Layout.Size - 1; j++)
            {
                var ri = _reverseIndex[j];
                tmp[j] = a * _upper[rim1] * bet;

                bet = b + a * (_diag[ri] - tmp[j] * _lower[ri]);
                QL.Ensure(bet != 0.0, "division by zero");
                bet = 1.0 / bet;

                retVal[ri] = (r[ri] - a * _lower[ri] * retVal[rim1]) * bet;
                rim1 = ri;
            }

            // Backward substitution
            for (Size j = _mesher.Layout.Size - 2; j > 0; --j)
                retVal[_reverseIndex[j]] -= tmp[j + 1] * retVal[_reverseIndex[j + 1]];
            retVal[_reverseIndex[0]] -= tmp[1] * retVal[_reverseIndex[1]];

            return retVal;
        }
    }
}