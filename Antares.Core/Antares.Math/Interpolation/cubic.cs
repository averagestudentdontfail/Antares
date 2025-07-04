using System;
using System.Collections.Generic;
using System.Linq;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Antares.Math.Interpolation
{
    namespace Detail
    {
        public class CoefficientHolder
        {
            public CoefficientHolder(int n)
            {
                N = n;
                PrimitiveConst = new double[n - 1];
                A = new double[n - 1];
                B = new double[n - 1];
                C = new double[n - 1];
                MonotonicityAdjustments = new bool[n];
            }

            public int N { get; }
            
            // P[i](x) = y[i] + 
            //           a[i]*(x-x[i]) + 
            //           b[i]*(x-x[i])^2 + 
            //           c[i]*(x-x[i])^3
            public double[] PrimitiveConst { get; }
            public double[] A { get; }
            public double[] B { get; }
            public double[] C { get; }
            public bool[] MonotonicityAdjustments { get; }
        }
    }

    /// <summary>
    /// Cubic interpolation between discrete points.
    /// </summary>
    public class CubicInterpolation : Interpolation
    {
        public enum DerivativeApprox
        {
            /// <summary>
            /// Spline approximation (non-local, non-monotonic, linear).
            /// Different boundary conditions can be used on the left and right boundaries.
            /// </summary>
            Spline,

            /// <summary>Overshooting minimization 1st derivative</summary>
            SplineOM1,

            /// <summary>Overshooting minimization 2nd derivative</summary>
            SplineOM2,

            /// <summary>Fourth-order approximation (local, non-monotonic, linear)</summary>
            FourthOrder,

            /// <summary>Parabolic approximation (local, non-monotonic, linear)</summary>
            Parabolic,

            /// <summary>Fritsch-Butland approximation (local, monotonic, non-linear)</summary>
            FritschButland,

            /// <summary>Akima approximation (local, non-monotonic, non-linear)</summary>
            Akima,

            /// <summary>Kruger approximation (local, monotonic, non-linear)</summary>
            Kruger,

            /// <summary>Weighted harmonic mean approximation (local, monotonic, non-linear)</summary>
            Harmonic
        }

        public enum BoundaryCondition
        {
            /// <summary>Make second(-last) point an inactive knot</summary>
            NotAKnot,

            /// <summary>Match value of end-slope</summary>
            FirstDerivative,

            /// <summary>Match value of second derivative at end</summary>
            SecondDerivative,

            /// <summary>Match first and second derivative at either end</summary>
            Periodic,

            /// <summary>
            /// Match end-slope to the slope of the cubic that matches
            /// the first four data at the respective end
            /// </summary>
            Lagrange
        }

        private readonly Detail.CoefficientHolder _coeffs;

        public CubicInterpolation(double[] xValues,
                                 double[] yValues,
                                 DerivativeApprox derivativeApprox,
                                 bool monotonic,
                                 BoundaryCondition leftCondition,
                                 double leftConditionValue,
                                 BoundaryCondition rightCondition,
                                 double rightConditionValue)
            : base(xValues, yValues)
        {
            if (xValues.Length != yValues.Length)
                throw new ArgumentException("X and Y arrays must have the same length");
            
            if (xValues.Length < 2)
                throw new ArgumentException("At least 2 points are required");

            if ((leftCondition == BoundaryCondition.Lagrange || rightCondition == BoundaryCondition.Lagrange) 
                && xValues.Length < 4)
            {
                throw new ArgumentException($"Lagrange boundary condition requires at least 4 points ({xValues.Length} are given)");
            }

            _coeffs = new Detail.CoefficientHolder(xValues.Length);
            
            var impl = new Detail.CubicInterpolationImpl(
                xValues, yValues,
                derivativeApprox, monotonic,
                leftCondition, leftConditionValue,
                rightCondition, rightConditionValue,
                _coeffs);
            
            impl.Update();
        }

        public double[] PrimitiveConstants => _coeffs.PrimitiveConst;
        public double[] ACoefficients => _coeffs.A;
        public double[] BCoefficients => _coeffs.B;
        public double[] CCoefficients => _coeffs.C;
        public bool[] MonotonicityAdjustments => _coeffs.MonotonicityAdjustments;

        public override double Value(double x)
        {
            int j = Locate(x);
            double dx = x - XValues[j];
            return YValues[j] + dx * (_coeffs.A[j] + dx * (_coeffs.B[j] + dx * _coeffs.C[j]));
        }

        public override double Derivative(double x)
        {
            int j = Locate(x);
            double dx = x - XValues[j];
            return _coeffs.A[j] + (2.0 * _coeffs.B[j] + 3.0 * _coeffs.C[j] * dx) * dx;
        }

        public override double SecondDerivative(double x)
        {
            int j = Locate(x);
            double dx = x - XValues[j];
            return 2.0 * _coeffs.B[j] + 6.0 * _coeffs.C[j] * dx;
        }

        public override double Primitive(double x)
        {
            int j = Locate(x);
            double dx = x - XValues[j];
            return _coeffs.PrimitiveConst[j] + 
                   dx * (YValues[j] + dx * (_coeffs.A[j] / 2.0 + 
                         dx * (_coeffs.B[j] / 3.0 + dx * _coeffs.C[j] / 4.0)));
        }
    }

    // Convenience classes
    public class CubicNaturalSpline : CubicInterpolation
    {
        public CubicNaturalSpline(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.Spline, false,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class MonotonicCubicNaturalSpline : CubicInterpolation
    {
        public MonotonicCubicNaturalSpline(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.Spline, true,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class CubicSplineOvershootingMinimization1 : CubicInterpolation
    {
        public CubicSplineOvershootingMinimization1(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.SplineOM1, false,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class CubicSplineOvershootingMinimization2 : CubicInterpolation
    {
        public CubicSplineOvershootingMinimization2(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.SplineOM2, false,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class AkimaCubicInterpolation : CubicInterpolation
    {
        public AkimaCubicInterpolation(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.Akima, false,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class KrugerCubic : CubicInterpolation
    {
        public KrugerCubic(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.Kruger, false,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class HarmonicCubic : CubicInterpolation
    {
        public HarmonicCubic(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.Harmonic, false,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class FritschButlandCubic : CubicInterpolation
    {
        public FritschButlandCubic(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.FritschButland, true,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class Parabolic : CubicInterpolation
    {
        public Parabolic(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.Parabolic, false,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    public class MonotonicParabolic : CubicInterpolation
    {
        public MonotonicParabolic(double[] xValues, double[] yValues)
            : base(xValues, yValues,
                   DerivativeApprox.Parabolic, true,
                   BoundaryCondition.SecondDerivative, 0.0,
                   BoundaryCondition.SecondDerivative, 0.0)
        {
        }
    }

    /// <summary>
    /// Cubic interpolation factory and traits
    /// </summary>
    public class Cubic
    {
        public static bool Global => true;
        public static int RequiredPoints => 2;

        private readonly CubicInterpolation.DerivativeApprox _derivativeApprox;
        private readonly bool _monotonic;
        private readonly CubicInterpolation.BoundaryCondition _leftType;
        private readonly CubicInterpolation.BoundaryCondition _rightType;
        private readonly double _leftValue;
        private readonly double _rightValue;

        public Cubic(CubicInterpolation.DerivativeApprox derivativeApprox = CubicInterpolation.DerivativeApprox.Kruger,
                     bool monotonic = false,
                     CubicInterpolation.BoundaryCondition leftCondition = CubicInterpolation.BoundaryCondition.SecondDerivative,
                     double leftConditionValue = 0.0,
                     CubicInterpolation.BoundaryCondition rightCondition = CubicInterpolation.BoundaryCondition.SecondDerivative,
                     double rightConditionValue = 0.0)
        {
            _derivativeApprox = derivativeApprox;
            _monotonic = monotonic;
            _leftType = leftCondition;
            _rightType = rightCondition;
            _leftValue = leftConditionValue;
            _rightValue = rightConditionValue;
        }

        public Interpolation Interpolate(double[] xValues, double[] yValues)
        {
            return new CubicInterpolation(xValues, yValues,
                                        _derivativeApprox, _monotonic,
                                        _leftType, _leftValue,
                                        _rightType, _rightValue);
        }
    }

    namespace Detail
    {
        internal class CubicInterpolationImpl
        {
            private readonly double[] _xValues;
            private readonly double[] _yValues;
            private readonly int _n;
            private readonly CubicInterpolation.DerivativeApprox _derivativeApprox;
            private readonly bool _monotonic;
            private readonly CubicInterpolation.BoundaryCondition _leftType;
            private readonly CubicInterpolation.BoundaryCondition _rightType;
            private readonly double _leftValue;
            private readonly double _rightValue;
            private readonly CoefficientHolder _coeffs;

            private readonly double[] _tmp;
            private readonly double[] _dx;
            private readonly double[] _s;
            private TridiagonalOperator _l;

            public CubicInterpolationImpl(double[] xValues,
                                        double[] yValues,
                                        CubicInterpolation.DerivativeApprox derivativeApprox,
                                        bool monotonic,
                                        CubicInterpolation.BoundaryCondition leftCondition,
                                        double leftConditionValue,
                                        CubicInterpolation.BoundaryCondition rightCondition,
                                        double rightConditionValue,
                                        CoefficientHolder coeffs)
            {
                _xValues = xValues;
                _yValues = yValues;
                _n = xValues.Length;
                _derivativeApprox = derivativeApprox;
                _monotonic = monotonic;
                _leftType = leftCondition;
                _rightType = rightCondition;
                _leftValue = leftConditionValue;
                _rightValue = rightConditionValue;
                _coeffs = coeffs;

                _tmp = new double[_n];
                _dx = new double[_n - 1];
                _s = new double[_n - 1];
                _l = new TridiagonalOperator(_n);
            }

            public void Update()
            {
                // Calculate differences and slopes
                for (int i = 0; i < _n - 1; ++i)
                {
                    _dx[i] = _xValues[i + 1] - _xValues[i];
                    _s[i] = (_yValues[i + 1] - _yValues[i]) / _dx[i];
                }

                // First derivative approximation
                if (_derivativeApprox == CubicInterpolation.DerivativeApprox.Spline)
                {
                    CalculateSplineDerivatives();
                }
                else if (_derivativeApprox == CubicInterpolation.DerivativeApprox.SplineOM1)
                {
                    CalculateSplineOM1Derivatives();
                }
                else if (_derivativeApprox == CubicInterpolation.DerivativeApprox.SplineOM2)
                {
                    CalculateSplineOM2Derivatives();
                }
                else
                {
                    CalculateLocalDerivatives();
                }

                // Apply monotonicity constraints if requested
                if (_monotonic)
                {
                    ApplyMonotonicityConstraints();
                }

                // Calculate cubic coefficients
                CalculateCubicCoefficients();

                // Calculate primitive constants
                CalculatePrimitiveConstants();
            }

            private void CalculateSplineDerivatives()
            {
                // Set up tridiagonal system for spline
                for (int i = 1; i < _n - 1; ++i)
                {
                    _l.SetMidRow(i, _dx[i], 2.0 * (_dx[i] + _dx[i - 1]), _dx[i - 1]);
                    _tmp[i] = 3.0 * (_dx[i] * _s[i - 1] + _dx[i - 1] * _s[i]);
                }

                // Left boundary condition
                SetLeftBoundaryCondition();

                // Right boundary condition  
                SetRightBoundaryCondition();

                // Solve the system
                _l.SolveFor(_tmp, _tmp);
            }

            private void SetLeftBoundaryCondition()
            {
                switch (_leftType)
                {
                    case CubicInterpolation.BoundaryCondition.NotAKnot:
                        _l.SetFirstRow(_dx[1] * (_dx[1] + _dx[0]),
                                      (_dx[0] + _dx[1]) * (_dx[0] + _dx[1]));
                        _tmp[0] = _s[0] * _dx[1] * (2.0 * _dx[1] + 3.0 * _dx[0]) +
                                 _s[1] * _dx[0] * _dx[0];
                        break;
                    case CubicInterpolation.BoundaryCondition.FirstDerivative:
                        _l.SetFirstRow(1.0, 0.0);
                        _tmp[0] = _leftValue;
                        break;
                    case CubicInterpolation.BoundaryCondition.SecondDerivative:
                        _l.SetFirstRow(2.0, 1.0);
                        _tmp[0] = 3.0 * _s[0] - _leftValue * _dx[0] / 2.0;
                        break;
                    case CubicInterpolation.BoundaryCondition.Periodic:
                        throw new NotImplementedException("Periodic boundary condition is not implemented yet");
                    case CubicInterpolation.BoundaryCondition.Lagrange:
                        _l.SetFirstRow(1.0, 0.0);
                        _tmp[0] = CubicInterpolatingPolynomialDerivative(
                            _xValues[0], _xValues[1], _xValues[2], _xValues[3],
                            _yValues[0], _yValues[1], _yValues[2], _yValues[3],
                            _xValues[0]);
                        break;
                    default:
                        throw new ArgumentException("Unknown boundary condition");
                }
            }

            private void SetRightBoundaryCondition()
            {
                switch (_rightType)
                {
                    case CubicInterpolation.BoundaryCondition.NotAKnot:
                        _l.SetLastRow(-(_dx[_n - 2] + _dx[_n - 3]) * (_dx[_n - 2] + _dx[_n - 3]),
                                     -_dx[_n - 3] * (_dx[_n - 3] + _dx[_n - 2]));
                        _tmp[_n - 1] = -_s[_n - 3] * _dx[_n - 2] * _dx[_n - 2] -
                                      _s[_n - 2] * _dx[_n - 3] * (3.0 * _dx[_n - 2] + 2.0 * _dx[_n - 3]);
                        break;
                    case CubicInterpolation.BoundaryCondition.FirstDerivative:
                        _l.SetLastRow(0.0, 1.0);
                        _tmp[_n - 1] = _rightValue;
                        break;
                    case CubicInterpolation.BoundaryCondition.SecondDerivative:
                        _l.SetLastRow(1.0, 2.0);
                        _tmp[_n - 1] = 3.0 * _s[_n - 2] + _rightValue * _dx[_n - 2] / 2.0;
                        break;
                    case CubicInterpolation.BoundaryCondition.Periodic:
                        throw new NotImplementedException("Periodic boundary condition is not implemented yet");
                    case CubicInterpolation.BoundaryCondition.Lagrange:
                        _l.SetLastRow(0.0, 1.0);
                        _tmp[_n - 1] = CubicInterpolatingPolynomialDerivative(
                            _xValues[_n - 4], _xValues[_n - 3], _xValues[_n - 2], _xValues[_n - 1],
                            _yValues[_n - 4], _yValues[_n - 3], _yValues[_n - 2], _yValues[_n - 1],
                            _xValues[_n - 1]);
                        break;
                    default:
                        throw new ArgumentException("Unknown boundary condition");
                }
            }

            private void CalculateSplineOM1Derivatives()
            {
                // Complex matrix operations for overshooting minimization
                var T = Matrix<double>.Build.Dense(_n - 2, _n);
                for (int i = 0; i < _n - 2; ++i)
                {
                    T[i, i] = _dx[i] / 6.0;
                    T[i, i + 1] = (_dx[i + 1] + _dx[i]) / 3.0;
                    T[i, i + 2] = _dx[i + 1] / 6.0;
                }

                var S = Matrix<double>.Build.Dense(_n - 2, _n);
                for (int i = 0; i < _n - 2; ++i)
                {
                    S[i, i] = 1.0 / _dx[i];
                    S[i, i + 1] = -(1.0 / _dx[i + 1] + 1.0 / _dx[i]);
                    S[i, i + 2] = 1.0 / _dx[i + 1];
                }

                var Up = Matrix<double>.Build.Dense(_n, 2);
                Up[0, 0] = 1;
                Up[_n - 1, 1] = 1;

                var Us = Matrix<double>.Build.Dense(_n, _n - 2);
                for (int i = 0; i < _n - 2; ++i)
                    Us[i + 1, i] = 1;

                var Z = Us * (T * Us).Inverse();
                var I = Matrix<double>.Build.DenseIdentity(_n);
                var V = (I - Z * T) * Up;
                var W = Z * S;

                var Q = Matrix<double>.Build.Dense(_n, _n);
                Q[0, 0] = 1.0 / (_n - 1) * _dx[0] * _dx[0] * _dx[0];
                Q[0, 1] = 7.0 / 8 * 1.0 / (_n - 1) * _dx[0] * _dx[0] * _dx[0];

                for (int i = 1; i < _n - 1; ++i)
                {
                    Q[i, i - 1] = 7.0 / 8 * 1.0 / (_n - 1) * _dx[i - 1] * _dx[i - 1] * _dx[i - 1];
                    Q[i, i] = 1.0 / (_n - 1) * _dx[i] * _dx[i] * _dx[i] + 1.0 / (_n - 1) * _dx[i - 1] * _dx[i - 1] * _dx[i - 1];
                    Q[i, i + 1] = 7.0 / 8 * 1.0 / (_n - 1) * _dx[i] * _dx[i] * _dx[i];
                }

                Q[_n - 1, _n - 2] = 7.0 / 8 * 1.0 / (_n - 1) * _dx[_n - 2] * _dx[_n - 2] * _dx[_n - 2];
                Q[_n - 1, _n - 1] = 1.0 / (_n - 1) * _dx[_n - 2] * _dx[_n - 2] * _dx[_n - 2];

                var J = (I - V * (V.Transpose() * Q * V).Inverse() * V.Transpose() * Q) * W;
                var Y = Vector<double>.Build.DenseOfArray(_yValues);
                var D = J * Y;

                for (int i = 0; i < _n - 1; ++i)
                    _tmp[i] = (Y[i + 1] - Y[i]) / _dx[i] - (2.0 * D[i] + D[i + 1]) * _dx[i] / 6.0;
                _tmp[_n - 1] = _tmp[_n - 2] + D[_n - 2] * _dx[_n - 2] + (D[_n - 1] - D[_n - 2]) * _dx[_n - 2] / 2.0;
            }

            private void CalculateSplineOM2Derivatives()
            {
                // Similar to OM1 but with different Q matrix
                var T = Matrix<double>.Build.Dense(_n - 2, _n);
                for (int i = 0; i < _n - 2; ++i)
                {
                    T[i, i] = _dx[i] / 6.0;
                    T[i, i + 1] = (_dx[i] + _dx[i + 1]) / 3.0;
                    T[i, i + 2] = _dx[i + 1] / 6.0;
                }

                var S = Matrix<double>.Build.Dense(_n - 2, _n);
                for (int i = 0; i < _n - 2; ++i)
                {
                    S[i, i] = 1.0 / _dx[i];
                    S[i, i + 1] = -(1.0 / _dx[i + 1] + 1.0 / _dx[i]);
                    S[i, i + 2] = 1.0 / _dx[i + 1];
                }

                var Up = Matrix<double>.Build.Dense(_n, 2);
                Up[0, 0] = 1;
                Up[_n - 1, 1] = 1;

                var Us = Matrix<double>.Build.Dense(_n, _n - 2);
                for (int i = 0; i < _n - 2; ++i)
                    Us[i + 1, i] = 1;

                var Z = Us * (T * Us).Inverse();
                var I = Matrix<double>.Build.DenseIdentity(_n);
                var V = (I - Z * T) * Up;
                var W = Z * S;

                var Q = Matrix<double>.Build.Dense(_n, _n);
                Q[0, 0] = 1.0 / (_n - 1) * _dx[0];
                Q[0, 1] = 1.0 / 2 * 1.0 / (_n - 1) * _dx[0];

                for (int i = 1; i < _n - 1; ++i)
                {
                    Q[i, i - 1] = 1.0 / 2 * 1.0 / (_n - 1) * _dx[i - 1];
                    Q[i, i] = 1.0 / (_n - 1) * _dx[i] + 1.0 / (_n - 1) * _dx[i - 1];
                    Q[i, i + 1] = 1.0 / 2 * 1.0 / (_n - 1) * _dx[i];
                }

                Q[_n - 1, _n - 2] = 1.0 / 2 * 1.0 / (_n - 1) * _dx[_n - 2];
                Q[_n - 1, _n - 1] = 1.0 / (_n - 1) * _dx[_n - 2];

                var J = (I - V * (V.Transpose() * Q * V).Inverse() * V.Transpose() * Q) * W;
                var Y = Vector<double>.Build.DenseOfArray(_yValues);
                var D = J * Y;

                for (int i = 0; i < _n - 1; ++i)
                    _tmp[i] = (Y[i + 1] - Y[i]) / _dx[i] - (2.0 * D[i] + D[i + 1]) * _dx[i] / 6.0;
                _tmp[_n - 1] = _tmp[_n - 2] + D[_n - 2] * _dx[_n - 2] + (D[_n - 1] - D[_n - 2]) * _dx[_n - 2] / 2.0;
            }

            private void CalculateLocalDerivatives()
            {
                if (_n == 2)
                {
                    _tmp[0] = _tmp[1] = _s[0];
                }
                else
                {
                    switch (_derivativeApprox)
                    {
                        case CubicInterpolation.DerivativeApprox.FourthOrder:
                            throw new NotImplementedException("FourthOrder not implemented yet");

                        case CubicInterpolation.DerivativeApprox.Parabolic:
                            CalculateParabolicDerivatives();
                            break;

                        case CubicInterpolation.DerivativeApprox.FritschButland:
                            CalculateFritschButlandDerivatives();
                            break;

                        case CubicInterpolation.DerivativeApprox.Akima:
                            CalculateAkimaDerivatives();
                            break;

                        case CubicInterpolation.DerivativeApprox.Kruger:
                            CalculateKrugerDerivatives();
                            break;

                        case CubicInterpolation.DerivativeApprox.Harmonic:
                            CalculateHarmonicDerivatives();
                            break;

                        default:
                            throw new ArgumentException("Unknown derivative approximation scheme");
                    }
                }
            }

            private void CalculateParabolicDerivatives()
            {
                // Intermediate points
                for (int i = 1; i < _n - 1; ++i)
                    _tmp[i] = (_dx[i - 1] * _s[i] + _dx[i] * _s[i - 1]) / (_dx[i] + _dx[i - 1]);

                // End points
                _tmp[0] = ((2.0 * _dx[0] + _dx[1]) * _s[0] - _dx[0] * _s[1]) / (_dx[0] + _dx[1]);
                _tmp[_n - 1] = ((2.0 * _dx[_n - 2] + _dx[_n - 3]) * _s[_n - 2] - _dx[_n - 2] * _s[_n - 3]) / (_dx[_n - 2] + _dx[_n - 3]);
            }

            private void CalculateFritschButlandDerivatives()
            {
                // Intermediate points
                for (int i = 1; i < _n - 1; ++i)
                {
                    double sMin = System.Math.Min(_s[i - 1], _s[i]);
                    double sMax = System.Math.Max(_s[i - 1], _s[i]);
                    
                    if (sMax + 2.0 * sMin == 0)
                    {
                        if (sMin * sMax < 0)
                            _tmp[i] = double.MinValue;
                        else if (sMin * sMax == 0)
                            _tmp[i] = 0;
                        else
                            _tmp[i] = double.MaxValue;
                    }
                    else
                    {
                        _tmp[i] = 3.0 * sMin * sMax / (sMax + 2.0 * sMin);
                    }
                }

                // End points
                _tmp[0] = ((2.0 * _dx[0] + _dx[1]) * _s[0] - _dx[0] * _s[1]) / (_dx[0] + _dx[1]);
                _tmp[_n - 1] = ((2.0 * _dx[_n - 2] + _dx[_n - 3]) * _s[_n - 2] - _dx[_n - 2] * _s[_n - 3]) / (_dx[_n - 2] + _dx[_n - 3]);
            }

            private void CalculateAkimaDerivatives()
            {
                _tmp[0] = (System.Math.Abs(_s[1] - _s[0]) * 2 * _s[0] * _s[1] + System.Math.Abs(2 * _s[0] * _s[1] - 4 * _s[0] * _s[0] * _s[1]) * _s[0]) / 
                         (System.Math.Abs(_s[1] - _s[0]) + System.Math.Abs(2 * _s[0] * _s[1] - 4 * _s[0] * _s[0] * _s[1]));

                _tmp[1] = (System.Math.Abs(_s[2] - _s[1]) * _s[0] + System.Math.Abs(_s[0] - 2 * _s[0] * _s[1]) * _s[1]) / 
                         (System.Math.Abs(_s[2] - _s[1]) + System.Math.Abs(_s[0] - 2 * _s[0] * _s[1]));

                for (int i = 2; i < _n - 2; ++i)
                {
                    if ((_s[i - 2] == _s[i - 1]) && (_s[i] != _s[i + 1]))
                        _tmp[i] = _s[i - 1];
                    else if ((_s[i - 2] != _s[i - 1]) && (_s[i] == _s[i + 1]))
                        _tmp[i] = _s[i];
                    else if (_s[i] == _s[i - 1])
                        _tmp[i] = _s[i];
                    else if ((_s[i - 2] == _s[i - 1]) && (_s[i - 1] != _s[i]) && (_s[i] == _s[i + 1]))
                        _tmp[i] = (_s[i - 1] + _s[i]) / 2.0;
                    else
                        _tmp[i] = (System.Math.Abs(_s[i + 1] - _s[i]) * _s[i - 1] + System.Math.Abs(_s[i - 1] - _s[i - 2]) * _s[i]) / 
                                 (System.Math.Abs(_s[i + 1] - _s[i]) + System.Math.Abs(_s[i - 1] - _s[i - 2]));
                }

                _tmp[_n - 2] = (System.Math.Abs(2 * _s[_n - 2] * _s[_n - 3] - _s[_n - 2]) * _s[_n - 3] + System.Math.Abs(_s[_n - 3] - _s[_n - 4]) * _s[_n - 2]) / 
                              (System.Math.Abs(2 * _s[_n - 2] * _s[_n - 3] - _s[_n - 2]) + System.Math.Abs(_s[_n - 3] - _s[_n - 4]));

                _tmp[_n - 1] = (System.Math.Abs(4 * _s[_n - 2] * _s[_n - 2] * _s[_n - 3] - 2 * _s[_n - 2] * _s[_n - 3]) * _s[_n - 2] + 
                               System.Math.Abs(_s[_n - 2] - _s[_n - 3]) * 2 * _s[_n - 2] * _s[_n - 3]) / 
                              (System.Math.Abs(4 * _s[_n - 2] * _s[_n - 2] * _s[_n - 3] - 2 * _s[_n - 2] * _s[_n - 3]) + System.Math.Abs(_s[_n - 2] - _s[_n - 3]));
            }

            private void CalculateKrugerDerivatives()
            {
                // Intermediate points
                for (int i = 1; i < _n - 1; ++i)
                {
                    if (_s[i - 1] * _s[i] < 0.0)
                        // slope changes sign at point
                        _tmp[i] = 0.0;
                    else
                        // slope will be between the slopes of the adjacent
                        // straight lines and should approach zero if the
                        // slope of either line approaches zero
                        _tmp[i] = 2.0 / (1.0 / _s[i - 1] + 1.0 / _s[i]);
                }

                // End points
                _tmp[0] = (3.0 * _s[0] - _tmp[1]) / 2.0;
                _tmp[_n - 1] = (3.0 * _s[_n - 2] - _tmp[_n - 2]) / 2.0;
            }

            private void CalculateHarmonicDerivatives()
            {
                // Intermediate points
                for (int i = 1; i < _n - 1; ++i)
                {
                    double w1 = 2 * _dx[i] + _dx[i - 1];
                    double w2 = _dx[i] + 2 * _dx[i - 1];
                    
                    if (_s[i - 1] * _s[i] <= 0.0)
                        // slope changes sign at point
                        _tmp[i] = 0.0;
                    else
                        // weighted harmonic mean of S_[i] and S_[i-1] if they
                        // have the same sign; otherwise 0
                        _tmp[i] = (w1 + w2) / (w1 / _s[i - 1] + w2 / _s[i]);
                }

                // End points [0]
                _tmp[0] = ((2 * _dx[0] + _dx[1]) * _s[0] - _dx[0] * _s[1]) / (_dx[1] + _dx[0]);
                if (_tmp[0] * _s[0] < 0.0)
                {
                    _tmp[0] = 0;
                }
                else if (_s[0] * _s[1] < 0)
                {
                    if (System.Math.Abs(_tmp[0]) > System.Math.Abs(3 * _s[0]))
                    {
                        _tmp[0] = 3 * _s[0];
                    }
                }

                // End points [n-1]
                _tmp[_n - 1] = ((2 * _dx[_n - 2] + _dx[_n - 3]) * _s[_n - 2] - _dx[_n - 2] * _s[_n - 3]) / (_dx[_n - 3] + _dx[_n - 2]);
                if (_tmp[_n - 1] * _s[_n - 2] < 0.0)
                {
                    _tmp[_n - 1] = 0;
                }
                else if (_s[_n - 2] * _s[_n - 3] < 0)
                {
                    if (System.Math.Abs(_tmp[_n - 1]) > System.Math.Abs(3 * _s[_n - 2]))
                    {
                        _tmp[_n - 1] = 3 * _s[_n - 2];
                    }
                }
            }

            private void ApplyMonotonicityConstraints()
            {
                Array.Fill(_coeffs.MonotonicityAdjustments, false);

                // Hyman monotonicity constrained filter
                for (int i = 0; i < _n; ++i)
                {
                    double correction;
                    
                    if (i == 0)
                    {
                        if (_tmp[i] * _s[0] > 0.0)
                        {
                            correction = _tmp[i] / System.Math.Abs(_tmp[i]) *
                                        System.Math.Min(System.Math.Abs(_tmp[i]), System.Math.Abs(3.0 * _s[0]));
                        }
                        else
                        {
                            correction = 0.0;
                        }
                        
                        if (correction != _tmp[i])
                        {
                            _tmp[i] = correction;
                            _coeffs.MonotonicityAdjustments[i] = true;
                        }
                    }
                    else if (i == _n - 1)
                    {
                        if (_tmp[i] * _s[_n - 2] > 0.0)
                        {
                            correction = _tmp[i] / System.Math.Abs(_tmp[i]) *
                                        System.Math.Min(System.Math.Abs(_tmp[i]), System.Math.Abs(3.0 * _s[_n - 2]));
                        }
                        else
                        {
                            correction = 0.0;
                        }
                        
                        if (correction != _tmp[i])
                        {
                            _tmp[i] = correction;
                            _coeffs.MonotonicityAdjustments[i] = true;
                        }
                    }
                    else
                    {
                        double pm = (_s[i - 1] * _dx[i] + _s[i] * _dx[i - 1]) / (_dx[i - 1] + _dx[i]);
                        double M = 3.0 * System.Math.Min(System.Math.Min(System.Math.Abs(_s[i - 1]), System.Math.Abs(_s[i])), System.Math.Abs(pm));
                        
                        if (i > 1)
                        {
                            if ((_s[i - 1] - _s[i - 2]) * (_s[i] - _s[i - 1]) > 0.0)
                            {
                                double pd = (_s[i - 1] * (2.0 * _dx[i - 1] + _dx[i - 2]) - _s[i - 2] * _dx[i - 1]) / (_dx[i - 2] + _dx[i - 1]);
                                if (pm * pd > 0.0 && pm * (_s[i - 1] - _s[i - 2]) > 0.0)
                                {
                                    M = System.Math.Max(M, 1.5 * System.Math.Min(System.Math.Abs(pm), System.Math.Abs(pd)));
                                }
                            }
                        }
                        
                        if (i < _n - 2)
                        {
                            if ((_s[i] - _s[i - 1]) * (_s[i + 1] - _s[i]) > 0.0)
                            {
                                double pu = (_s[i] * (2.0 * _dx[i] + _dx[i + 1]) - _s[i + 1] * _dx[i]) / (_dx[i] + _dx[i + 1]);
                                if (pm * pu > 0.0 && -pm * (_s[i] - _s[i - 1]) > 0.0)
                                {
                                    M = System.Math.Max(M, 1.5 * System.Math.Min(System.Math.Abs(pm), System.Math.Abs(pu)));
                                }
                            }
                        }
                        
                        if (_tmp[i] * pm > 0.0)
                        {
                            correction = _tmp[i] / System.Math.Abs(_tmp[i]) * System.Math.Min(System.Math.Abs(_tmp[i]), M);
                        }
                        else
                        {
                            correction = 0.0;
                        }
                        
                        if (correction != _tmp[i])
                        {
                            _tmp[i] = correction;
                            _coeffs.MonotonicityAdjustments[i] = true;
                        }
                    }
                }
            }

            private void CalculateCubicCoefficients()
            {
                // Cubic coefficients
                for (int i = 0; i < _n - 1; ++i)
                {
                    _coeffs.A[i] = _tmp[i];
                    _coeffs.B[i] = (3.0 * _s[i] - _tmp[i + 1] - 2.0 * _tmp[i]) / _dx[i];
                    _coeffs.C[i] = (_tmp[i + 1] + _tmp[i] - 2.0 * _s[i]) / (_dx[i] * _dx[i]);
                }
            }

            private void CalculatePrimitiveConstants()
            {
                _coeffs.PrimitiveConst[0] = 0.0;
                for (int i = 1; i < _n - 1; ++i)
                {
                    _coeffs.PrimitiveConst[i] = _coeffs.PrimitiveConst[i - 1] +
                        _dx[i - 1] * (_yValues[i - 1] + _dx[i - 1] *
                                     (_coeffs.A[i - 1] / 2.0 + _dx[i - 1] *
                                      (_coeffs.B[i - 1] / 3.0 + _dx[i - 1] * _coeffs.C[i - 1] / 4.0)));
                }
            }

            private double CubicInterpolatingPolynomialDerivative(
                double a, double b, double c, double d,
                double u, double v, double w, double z, double x)
            {
                return (-((((a - c) * (b - c) * (c - x) * z - (a - d) * (b - d) * (d - x) * w) * (a - x + b - x)
                           + ((a - c) * (b - c) * z - (a - d) * (b - d) * w) * (a - x) * (b - x)) * (a - b) +
                          ((a - c) * (a - d) * v - (b - c) * (b - d) * u) * (c - d) * (c - x) * (d - x)
                          + ((a - c) * (a - d) * (a - x) * v - (b - c) * (b - d) * (b - x) * u)
                          * (c - x + d - x) * (c - d))) /
                    ((a - b) * (a - c) * (a - d) * (b - c) * (b - d) * (c - d));
            }
        }
    }
}