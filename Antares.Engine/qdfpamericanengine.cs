// C# code for QdFpAmericanEngine.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Instrument;
using Antares.Process;
using Antares.Engine;
using Antares.Math.Distribution;
using Antares.Math.Interpolation;
using Antares.Math.Integral;
using Antares.Math;
using Antares.Term.Yield;
using Antares.Time;

namespace Antares.Engine
{
    /// <summary>
    /// Abstract base class for QD fixed-point iteration schemes.
    /// </summary>
    public abstract class QdFpIterationScheme
    {
        public abstract int GetNumberOfChebyshevInterpolationNodes();
        public abstract int GetNumberOfNaiveFixedPointSteps();
        public abstract int GetNumberOfJacobiNewtonFixedPointSteps();
        public abstract Integrator GetFixedPointIntegrator();
        public abstract Integrator GetExerciseBoundaryToPriceIntegrator();
    }

    /// <summary>
    /// Gauss-Legendre (l,m,n)-p Scheme.
    /// </summary>
    /// <remarks>
    /// <param name="l">Order of Gauss-Legendre integration within every fixed point iteration step.</param>
    /// <param name="m">Fixed point iteration steps, first step is a partial Jacobi-Newton,
    ///                the rest are naive Richardson fixed point iterations.</param>
    /// <param name="n">Number of Chebyshev nodes to interpolate the exercise boundary.</param>
    /// <param name="p">Order of Gauss-Legendre integration in final conversion of the
    ///                exercise boundary into option prices.</param>
    /// </remarks>
    public class QdFpLegendreScheme : QdFpIterationScheme
    {
        protected readonly int _m;
        protected readonly int _n;
        protected readonly Integrator _fpIntegrator;
        protected readonly Integrator _exerciseBoundaryIntegrator;

        public QdFpLegendreScheme(int l, int m, int n, int p)
        {
            _m = m;
            _n = n;
            _fpIntegrator = new GaussLegendreIntegrator(l);
            _exerciseBoundaryIntegrator = new GaussLegendreIntegrator(p);

            QL.Require(m > 0, "at least one fixed point iteration step is needed");
            QL.Require(n > 0, "at least one interpolation point is needed");
        }

        public override int GetNumberOfChebyshevInterpolationNodes() => _n;
        public override int GetNumberOfNaiveFixedPointSteps() => _m - 1;
        public override int GetNumberOfJacobiNewtonFixedPointSteps() => 1;
        public override Integrator GetFixedPointIntegrator() => _fpIntegrator;
        public override Integrator GetExerciseBoundaryToPriceIntegrator() => _exerciseBoundaryIntegrator;
    }

    /// <summary>
    /// Legendre-Tanh-Sinh (l,m,n)-eps Scheme.
    /// </summary>
    /// <remarks>
    /// <param name="l">Order of Gauss-Legendre integration within every fixed point iteration step.</param>
    /// <param name="m">Fixed point iteration steps, first step is a partial Jacobi-Newton,
    ///                the rest are naive Richardson fixed point iterations.</param>
    /// <param name="n">Number of Chebyshev nodes to interpolate the exercise boundary.</param>
    /// <param name="eps">Final conversion of the exercise boundary into option prices
    ///                  is carried out by a tanh-sinh integration with accuracy eps.</param>
    /// </remarks>
    public class QdFpLegendreTanhSinhScheme : QdFpLegendreScheme
    {
        private readonly double _eps;

        public QdFpLegendreTanhSinhScheme(int l, int m, int n, double eps)
            : base(l, m, n, 1)
        {
            _eps = eps;
        }

        public override Integrator GetExerciseBoundaryToPriceIntegrator()
        {
            try
            {
                return new TanhSinhIntegral(_eps);
            }
            catch
            {
                return new GaussLobattoIntegral(100000, QLDefines.MAX_REAL, 0.1 * _eps);
            }
        }
    }

    /// <summary>
    /// Tanh-sinh (m,n)-eps Scheme.
    /// </summary>
    /// <remarks>
    /// <param name="m">Fixed point iteration steps, first step is a partial Jacobi-Newton,
    ///                the rest are naive Richardson fixed point iterations.</param>
    /// <param name="n">Number of Chebyshev nodes to interpolate the exercise boundary.</param>
    /// <param name="eps">Tanh-sinh integration precision.</param>
    /// </remarks>
    public class QdFpTanhSinhIterationScheme : QdFpIterationScheme
    {
        private readonly int _m;
        private readonly int _n;
        private readonly Integrator _integrator;

        public QdFpTanhSinhIterationScheme(int m, int n, double eps)
        {
            _m = m;
            _n = n;
            try
            {
                _integrator = new TanhSinhIntegral(eps);
            }
            catch
            {
                _integrator = new GaussLobattoIntegral(100000, QLDefines.MAX_REAL, 0.1 * eps);
            }
        }

        public override int GetNumberOfChebyshevInterpolationNodes() => _n;
        public override int GetNumberOfNaiveFixedPointSteps() => _m - 1;
        public override int GetNumberOfJacobiNewtonFixedPointSteps() => 1;
        public override Integrator GetFixedPointIntegrator() => _integrator;
        public override Integrator GetExerciseBoundaryToPriceIntegrator() => _integrator;
    }

    /// <summary>
    /// Abstract base class for fixed-point equation implementations.
    /// </summary>
    internal abstract class DqFpEquation
    {
        protected readonly double _r;
        protected readonly double _q;
        protected readonly double _vol;
        protected readonly Func<double, double> _B;
        protected readonly Integrator _integrator;
        protected readonly Array _x_i;
        protected readonly Array _w_i;
        protected readonly NormalDistribution _phi = new NormalDistribution();
        protected readonly CumulativeNormalDistribution _Phi = new CumulativeNormalDistribution();

        protected DqFpEquation(double r, double q, double vol, Func<double, double> B, Integrator integrator)
        {
            _r = r;
            _q = q;
            _vol = vol;
            _B = B;
            _integrator = integrator;

            // Try to extract Gauss-Legendre points and weights if available
            if (integrator is GaussLegendreIntegrator legendreIntegrator)
            {
                _x_i = legendreIntegrator.Integration.X;
                _w_i = legendreIntegrator.Integration.Weights;
            }
            else
            {
                _x_i = new Array(0);
                _w_i = new Array(0);
            }
        }

        public abstract (double, double) NDd(double tau, double b);
        public abstract (double, double, double) F(double tau, double b);

        protected (double, double) D(double t, double z)
        {
            double v = _vol * Math.Sqrt(t);
            double m = (Math.Log(z) + (_r - _q) * t) / v + 0.5 * v;
            return (m, m - v);
        }
    }

    /// <summary>
    /// Fixed-point equation implementation A.
    /// </summary>
    internal class DqFpEquation_A : DqFpEquation
    {
        private readonly double _K;

        public DqFpEquation_A(double K, double r, double q, double vol, Func<double, double> B, Integrator integrator)
            : base(r, q, vol, B, integrator)
        {
            _K = K;
        }

        public override (double, double, double) F(double tau, double b)
        {
            double v = _vol * Math.Sqrt(tau);

            double N, D;
            if (tau < Functional.Squared(QLDefines.EPSILON))
            {
                if (Comparison.CloseEnough(b, _K))
                {
                    N = 1 / (MathConstants.Sqrt2 * MathConstants.SqrtPi * v);
                    D = N + 0.5;
                }
                else
                {
                    N = 0.0;
                    D = (b > _K) ? 1.0 : 0.0;
                }
            }
            else
            {
                double stv = Math.Sqrt(tau) / _vol;

                double K12, K3;
                if (_x_i.Count > 0)
                {
                    K12 = K3 = 0.0;

                    for (int i = _x_i.Count - 1; i >= 0; --i)
                    {
                        double y = _x_i[i];
                        double m = 0.25 * tau * Functional.Squared(1 + y);
                        var dpm = D(m, b / _B(tau - m));

                        K12 += _w_i[i] * Math.Exp(_q * tau - _q * m)
                            * (0.5 * tau * (y + 1) * _Phi.Value(dpm.Item1) + stv * _phi.Value(dpm.Item1));
                        K3 += _w_i[i] * stv * Math.Exp(_r * tau - _r * m) * _phi.Value(dpm.Item2);
                    }
                }
                else
                {
                    K12 = _integrator.Integrate(y =>
                    {
                        double m = 0.25 * tau * Functional.Squared(1 + y);
                        double df = Math.Exp(_q * tau - _q * m);

                        if (y <= 5 * QLDefines.EPSILON - 1)
                        {
                            if (Comparison.CloseEnough(b, _B(tau - m)))
                                return df * stv / (MathConstants.Sqrt2 * MathConstants.SqrtPi);
                            else
                                return 0.0;
                        }
                        else
                        {
                            double dp = D(m, b / _B(tau - m)).Item1;
                            return df * (0.5 * tau * (y + 1) * _Phi.Value(dp) + stv * _phi.Value(dp));
                        }
                    }, -1, 1);

                    K3 = _integrator.Integrate(y =>
                    {
                        double m = 0.25 * tau * Functional.Squared(1 + y);
                        double df = Math.Exp(_r * tau - _r * m);

                        if (y <= 5 * QLDefines.EPSILON - 1)
                        {
                            if (Comparison.CloseEnough(b, _B(tau - m)))
                                return df * stv / (MathConstants.Sqrt2 * MathConstants.SqrtPi);
                            else
                                return 0.0;
                        }
                        else
                            return df * stv * _phi.Value(D(m, b / _B(tau - m)).Item2);
                    }, -1, 1);
                }

                var dpm = D(tau, b / _K);
                N = _phi.Value(dpm.Item2) / v + _r * K3;
                D = _phi.Value(dpm.Item1) / v + _Phi.Value(dpm.Item1) + _q * K12;
            }

            double alpha = _K * Math.Exp(-(_r - _q) * tau);
            double fv;
            if (tau < Functional.Squared(QLDefines.EPSILON))
            {
                if (Comparison.CloseEnough(b, _K))
                    fv = alpha;
                else if (b > _K)
                    fv = 0.0;
                else
                {
                    if (Comparison.CloseEnough(_q, 0.0))
                        fv = alpha * _r * ((_q < 0) ? -1.0 : 1.0) / QLDefines.EPSILON;
                    else
                        fv = alpha * _r / _q;
                }
            }
            else
                fv = alpha * N / D;

            return (N, D, fv);
        }

        public override (double, double) NDd(double tau, double b)
        {
            double Dd, Nd;

            if (tau < Functional.Squared(QLDefines.EPSILON))
            {
                if (Comparison.CloseEnough(b, _K))
                {
                    double sqTau = Math.Sqrt(tau);
                    double vol2 = _vol * _vol;
                    Dd = MathConstants.OneOverSqrtPi * MathConstants.Sqrt2 * (
                        -(0.5 * vol2 + _r - _q) / (b * _vol * vol2 * sqTau) + 1 / (b * _vol * sqTau));
                    Nd = MathConstants.OneOverSqrtPi * MathConstants.Sqrt2 * (-0.5 * vol2 + _r - _q) / (b * _vol * vol2 * sqTau);
                }
                else
                    Dd = Nd = 0.0;
            }
            else
            {
                var dpm = D(tau, b / _K);
                Dd = -_phi.Value(dpm.Item1) * dpm.Item1 / (b * _vol * _vol * tau) +
                      _phi.Value(dpm.Item1) / (b * _vol * Math.Sqrt(tau));
                Nd = -_phi.Value(dpm.Item2) * dpm.Item2 / (b * _vol * _vol * tau);
            }

            return (Nd, Dd);
        }
    }

    /// <summary>
    /// Fixed-point equation implementation B.
    /// </summary>
    internal class DqFpEquation_B : DqFpEquation
    {
        private readonly double _K;

        public DqFpEquation_B(double K, double r, double q, double vol, Func<double, double> B, Integrator integrator)
            : base(r, q, vol, B, integrator)
        {
            _K = K;
        }

        public override (double, double, double) F(double tau, double b)
        {
            double N, D;
            if (tau < Functional.Squared(QLDefines.EPSILON))
            {
                if (Comparison.CloseEnough(b, _K))
                    N = D = 0.5;
                else if (b < _K)
                    N = D = 0.0;
                else
                    N = D = 1.0;
            }
            else
            {
                double ni, di;
                if (_x_i.Count > 0)
                {
                    double c = 0.5 * tau;

                    ni = di = 0.0;
                    for (int i = _x_i.Count - 1; i >= 0; --i)
                    {
                        double u = c * _x_i[i] + c;
                        var dpm = D(tau - u, b / _B(u));
                        ni += _w_i[i] * Math.Exp(_r * u) * _Phi.Value(dpm.Item2);
                        di += _w_i[i] * Math.Exp(_q * u) * _Phi.Value(dpm.Item1);
                    }
                    ni *= c;
                    di *= c;
                }
                else
                {
                    ni = _integrator.Integrate(u =>
                    {
                        double df = Math.Exp(_r * u);
                        if (u >= tau * (1 - 5 * QLDefines.EPSILON))
                        {
                            if (Comparison.CloseEnough(b, _B(u)))
                                return 0.5 * df;
                            else
                                return df * ((b < _B(u) ? 0.0 : 1.0));
                        }
                        else
                            return df * _Phi.Value(D(tau - u, b / _B(u)).Item2);
                    }, 0, tau);

                    di = _integrator.Integrate(u =>
                    {
                        double df = Math.Exp(_q * u);
                        if (u >= tau * (1 - 5 * QLDefines.EPSILON))
                        {
                            if (Comparison.CloseEnough(b, _B(u)))
                                return 0.5 * df;
                            else
                                return df * ((b < _B(u) ? 0.0 : 1.0));
                        }
                        else
                            return df * _Phi.Value(D(tau - u, b / _B(u)).Item1);
                    }, 0, tau);
                }

                var dpm = D(tau, b / _K);
                N = _Phi.Value(dpm.Item2) + _r * ni;
                D = _Phi.Value(dpm.Item1) + _q * di;
            }

            double fv;
            double alpha = _K * Math.Exp(-(_r - _q) * tau);
            if (tau < Functional.Squared(QLDefines.EPSILON))
            {
                if (Comparison.CloseEnough(b, _K) || b > _K)
                    fv = alpha;
                else
                {
                    if (Comparison.CloseEnough(_q, 0.0))
                        fv = alpha * _r * ((_q < 0) ? -1.0 : 1.0) / QLDefines.EPSILON;
                    else
                        fv = alpha * _r / _q;
                }
            }
            else
                fv = alpha * N / D;

            return (N, D, fv);
        }

        public override (double, double) NDd(double tau, double b)
        {
            var dpm = D(tau, b / _K);
            return (_phi.Value(dpm.Item2) / (b * _vol * Math.Sqrt(tau)),
                    _phi.Value(dpm.Item1) / (b * _vol * Math.Sqrt(tau)));
        }
    }

    /// <summary>
    /// High performance/precision American engine based on fixed point iteration for the exercise boundary.
    /// </summary>
    /// <remarks>
    /// References:
    /// Leif Andersen, Mark Lake and Dimitri Offengenden (2015)
    /// "High Performance American Option Pricing",
    /// https://papers.ssrn.com/sol3/papers.cfm?abstract_id=2547027
    /// 
    /// Leif Andersen, Mark Lake (2021)
    /// "Fast American Option Pricing: The Double-Boundary Case"
    /// 
    /// https://onlinelibrary.wiley.com/doi/abs/10.1002/wilm.10969
    /// </remarks>
    public class QdFpAmericanEngine : Detail.QdPutCallParityEngine
    {
        public enum FixedPointEquation
        {
            FP_A,
            FP_B,
            Auto
        }

        private readonly QdFpIterationScheme _iterationScheme;
        private readonly FixedPointEquation _fpEquation;

        public QdFpAmericanEngine(GeneralizedBlackScholesProcess bsProcess,
                                 QdFpIterationScheme iterationScheme = null,
                                 FixedPointEquation fpEquation = FixedPointEquation.Auto)
            : base(bsProcess)
        {
            _iterationScheme = iterationScheme ?? AccurateScheme();
            _fpEquation = fpEquation;
        }

        public static QdFpIterationScheme FastScheme()
        {
            return new QdFpLegendreScheme(7, 2, 7, 27);
        }

        public static QdFpIterationScheme AccurateScheme()
        {
            return new QdFpLegendreTanhSinhScheme(25, 5, 13, 1e-8);
        }

        public static QdFpIterationScheme HighPrecisionScheme()
        {
            return new QdFpTanhSinhIterationScheme(10, 30, 1e-10);
        }

        protected override double CalculatePut(double S, double K, double r, double q, double vol, double T)
        {
            if (r < 0.0 && q < r)
                QL.Fail("double-boundary case q<r<0 for a put option is given");

            double xmax = QdPlusAmericanEngine.XMax(K, r, q);
            int n = _iterationScheme.GetNumberOfChebyshevInterpolationNodes();

            var interp = new QdPlusAmericanEngine(_process, n + 1, QdPlusAmericanEngine.SolverType.Halley, 1e-8)
                .GetPutExerciseBoundary(S, K, r, q, vol, T);

            var z = interp.Nodes();
            var x = 0.5 * Math.Sqrt(T) * (1.0 + z);

            Func<double, double> B = tau =>
            {
                double zValue = 2 * Math.Sqrt(Math.Abs(tau) / T) - 1;
                return xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, interp.Value(zValue, true))));
            };

            Func<double, double> h = fv => Functional.Squared(Math.Log(fv / xmax));

            DqFpEquation eqn;
            if (_fpEquation == FixedPointEquation.FP_A || 
                (_fpEquation == FixedPointEquation.Auto && Math.Abs(r - q) < 0.001))
            {
                eqn = new DqFpEquation_A(K, r, q, vol, B, _iterationScheme.GetFixedPointIntegrator());
            }
            else
            {
                eqn = new DqFpEquation_B(K, r, q, vol, B, _iterationScheme.GetFixedPointIntegrator());
            }

            var y = new Array(x.Count);
            y[0] = 0.0;

            int n_newton = _iterationScheme.GetNumberOfJacobiNewtonFixedPointSteps();
            for (int k = 0; k < n_newton; ++k)
            {
                for (int i = 1; i < x.Count; ++i)
                {
                    double tau = Functional.Squared(x[i]);
                    double b = B(tau);

                    var results = eqn.F(tau, b);
                    double N = results.Item1;
                    double D = results.Item2;
                    double fv = results.Item3;

                    if (tau < QLDefines.EPSILON)
                        y[i] = h(fv);
                    else
                    {
                        var ndd = eqn.NDd(tau, b);
                        double Nd = ndd.Item1;
                        double Dd = ndd.Item2;

                        double fd = K * Math.Exp(-(r - q) * tau) * (Nd / D - Dd * N / (D * D));

                        y[i] = h(b - (fv - b) / (fd - 1));
                    }
                }
                interp.UpdateY(y);
            }

            int n_fp = _iterationScheme.GetNumberOfNaiveFixedPointSteps();
            for (int k = 0; k < n_fp; ++k)
            {
                for (int i = 1; i < x.Count; ++i)
                {
                    double tau = Functional.Squared(x[i]);
                    double fv = eqn.F(tau, B(tau)).Item3;

                    y[i] = h(fv);
                }
                interp.UpdateY(y);
            }

            var aov = new Detail.QdPlusAddOnValue(T, S, K, r, q, vol, xmax, interp);
            double addOn = _iterationScheme.GetExerciseBoundaryToPriceIntegrator()
                .Integrate(aov.Evaluate, 0.0, Math.Sqrt(T));

            double europeanValue = new BlackCalculator(Option.Type.Put, K, S * Math.Exp((r - q) * T),
                                                      vol * Math.Sqrt(T), Math.Exp(-r * T)).Value();

            return Math.Max(europeanValue, 0.0) + Math.Max(0.0, addOn);
        }
    }
}