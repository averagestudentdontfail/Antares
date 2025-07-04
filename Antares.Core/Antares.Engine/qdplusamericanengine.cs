// C# code for QdPlusAmericanEngine.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Instrument;
using Antares.Process;
using Antares.Engine;
using Antares.Math.Distribution;
using Antares.Math.Solver;
using Antares.Math.Interpolation;
using Antares.Math.Integral;
using Antares.Math;
using Antares.Term.Yield;
using Antares.Time;

namespace Antares.Engine
{
    /// <summary>
    /// Boundary evaluator for the QD+ American option pricing method.
    /// </summary>
    internal class QdPlusBoundaryEvaluator
    {
        private readonly double _tau;
        private readonly double _K;
        private readonly double _sigma;
        private readonly double _sigma2;
        private readonly double _v;
        private readonly double _r;
        private readonly double _q;
        private readonly double _dr;
        private readonly double _dq;
        private readonly double _ddr;
        private readonly double _omega;
        private readonly double _lambda;
        private readonly double _lambdaPrime;
        private readonly double _alpha;
        private readonly double _beta;
        private readonly double _xMax;
        private readonly double _xMin;
        
        private readonly CumulativeNormalDistribution _Phi = new CumulativeNormalDistribution();
        private readonly NormalDistribution _phi = new NormalDistribution();
        
        private int _nrEvaluations = 0;
        private double _sc = double.NaN;
        private double _dp;
        private double _dm;
        private double _Phi_dp;
        private double _Phi_dm;
        private double _phi_dp;
        private double _npv;
        private double _theta;
        private double _charm;

        public QdPlusBoundaryEvaluator(double S, double strike, double rf, double dy, 
                                     double vol, double t, double T)
        {
            _tau = t;
            _K = strike;
            _sigma = vol;
            _sigma2 = _sigma * _sigma;
            _v = _sigma * Math.Sqrt(_tau);
            _r = rf;
            _q = dy;
            _dr = Math.Exp(-_r * _tau);
            _dq = Math.Exp(-_q * _tau);
            
            _ddr = (Math.Abs(_r * _tau) > 1e-5) 
                ? _r / (1 - _dr) 
                : 1 / (_tau * (1 - 0.5 * _r * _tau * (1 - _r * _tau / 3)));
                
            _omega = 2 * (_r - _q) / _sigma2;
            _lambda = 0.5 * (-(_omega - 1) - Math.Sqrt(Squared(_omega - 1) + 8 * _ddr / _sigma2));
            _lambdaPrime = 2 * _ddr * _ddr / (_sigma2 * Math.Sqrt(Squared(_omega - 1) + 8 * _ddr / _sigma2));
            _alpha = 2 * _dr / (_sigma2 * (2 * _lambda + _omega - 1));
            _beta = _alpha * (_ddr + _lambdaPrime / (2 * _lambda + _omega - 1)) - _lambda;
            _xMax = QdPlusAmericanEngine.XMax(strike, _r, _q);
            _xMin = QLDefines.EPSILON * 1e4 * Math.Min(0.5 * (strike + S), _xMax);
        }

        public double Evaluate(double S)
        {
            ++_nrEvaluations;

            if (S != _sc)
                PreCalculate(S);

            if (Comparison.CloseEnough(_K - S, _npv))
            {
                return (1 - _dq * _Phi_dp) * S + _alpha * _theta / _dr;
            }
            else
            {
                double c0 = -_beta - _lambda + _alpha * _theta / (_dr * (_K - S - _npv));
                return (1 - _dq * _Phi_dp) * S + (_lambda + c0) * (_K - S - _npv);
            }
        }

        public double Derivative(double S)
        {
            if (S != _sc)
                PreCalculate(S);

            return 1 - _dq * _Phi_dp + _dq / _v * _phi_dp + _beta * (1 - _dq * _Phi_dp)
                   + _alpha / _dr * _charm;
        }

        public double SecondDerivative(double S)
        {
            if (S != _sc)
                PreCalculate(S);

            double gamma = _phi_dp * _dq / (_v * S);
            double colour = gamma * (_q + (_r - _q) * _dp / _v + (1 - _dp * _dm) / (2 * _tau));

            return _dq * (_phi_dp / (S * _v) - _phi_dp * _dp / (S * _v * _v))
                   + _beta * gamma + _alpha / _dr * colour;
        }

        public double XMin => _xMin;
        public double XMax => _xMax;
        public int Evaluations => _nrEvaluations;

        private void PreCalculate(double S)
        {
            S = Math.Max(QLDefines.EPSILON, S);
            _sc = S;
            _dp = Math.Log(S * _dq / (_K * _dr)) / _v + 0.5 * _v;
            _dm = _dp - _v;
            _Phi_dp = _Phi.Value(-_dp);
            _Phi_dm = _Phi.Value(-_dm);
            _phi_dp = _phi.Value(_dp);

            _npv = _dr * _K * _Phi_dm - S * _dq * _Phi_dp;
            _theta = _r * _K * _dr * _Phi_dm - _q * S * _dq * _Phi_dp - _sigma2 * S / (2 * _v) * _dq * _phi_dp;
            _charm = -_dq * (_phi_dp * ((_r - _q) / _v - _dm / (2 * _tau)) + _q * _Phi_dp);
        }

        private static double Squared(double x) => x * x;
    }

    namespace Detail
    {
        /// <summary>
        /// Add-on value calculator for QD+ American option pricing.
        /// </summary>
        internal class QdPlusAddOnValue
        {
            private readonly double _T;
            private readonly double _S;
            private readonly double _K;
            private readonly double _xmax;
            private readonly double _r;
            private readonly double _q;
            private readonly double _vol;
            private readonly Interpolation _q_z;
            private readonly CumulativeNormalDistribution _Phi = new CumulativeNormalDistribution();

            public QdPlusAddOnValue(double T, double S, double K, double r, double q, 
                                  double vol, double xmax, Interpolation q_z)
            {
                _T = T;
                _S = S;
                _K = K;
                _xmax = xmax;
                _r = r;
                _q = q;
                _vol = vol;
                _q_z = q_z;
            }

            public double Evaluate(double z)
            {
                double t = z * z;
                double q = _q_z.Value(2 * Math.Sqrt(Math.Max(0.0, _T - t) / _T) - 1, true);
                double b_t = _xmax * Math.Exp(-Math.Sqrt(Math.Max(0.0, q)));

                double dr = Math.Exp(-_r * t);
                double dq = Math.Exp(-_q * t);
                double v = _vol * Math.Sqrt(t);

                double r;
                if (v >= QLDefines.EPSILON)
                {
                    if (b_t > QLDefines.EPSILON)
                    {
                        double dp = Math.Log(_S * dq / (b_t * dr)) / v + 0.5 * v;
                        r = 2 * z * (_r * _K * dr * _Phi.Value(-dp + v) - _q * _S * dq * _Phi.Value(-dp));
                    }
                    else
                        r = 0.0;
                }
                else if (Comparison.CloseEnough(_S * dq, b_t * dr))
                    r = z * (_r * _K * dr - _q * _S * dq);
                else if (b_t * dr > _S * dq)
                    r = 2 * z * (_r * _K * dr - _q * _S * dq);
                else
                    r = 0.0;

                return r;
            }
        }

        /// <summary>
        /// Base class implementing put-call parity for American option engines.
        /// </summary>
        public abstract class QdPutCallParityEngine : VanillaOption.Engine
        {
            protected readonly GeneralizedBlackScholesProcess _process;

            protected QdPutCallParityEngine(GeneralizedBlackScholesProcess process)
            {
                _process = process ?? throw new ArgumentNullException(nameof(process));
                RegisterWith(_process);
            }

            public override void Calculate()
            {
                QL.Require(_arguments.Exercise.Type == Exercise.ExerciseType.American,
                          "not an American option");

                var payoff = _arguments.Payoff as IStrikedTypePayoff;
                QL.Require(payoff != null, "non-striked payoff given");

                double spot = _process.StateVariable.Value.Value;
                QL.Require(spot >= 0.0, "negative underlying given");

                var maturity = _arguments.Exercise.LastDate;
                double T = _process.Time(maturity);
                double S = _process.StateVariable.Value.Value;
                double K = payoff.Strike;
                double r = -Math.Log(_process.RiskFreeRate.Value.Discount(maturity)) / T;
                double q = -Math.Log(_process.DividendYield.Value.Discount(maturity)) / T;
                double vol = _process.BlackVolatility.Value.BlackVol(T, K);

                QL.Require(S >= 0, "zero or positive underlying value is required");
                QL.Require(K >= 0, "zero or positive strike is required");
                QL.Require(vol >= 0, "zero or positive volatility is required");

                if (payoff.OptionType == Option.Type.Put)
                    _results.Value = CalculatePutWithEdgeCases(S, K, r, q, vol, T);
                else if (payoff.OptionType == Option.Type.Call)
                    _results.Value = CalculatePutWithEdgeCases(K, S, q, r, vol, T);
                else
                    QL.Fail("unknown option type");
            }

            protected abstract double CalculatePut(double S, double K, double r, double q, double vol, double T);

            private double CalculatePutWithEdgeCases(double S, double K, double r, double q, double vol, double T)
            {
                if (Comparison.Close(K, 0.0))
                    return 0.0;

                if (Comparison.Close(S, 0.0))
                    return Math.Max(K, K * Math.Exp(-r * T));

                if (r <= 0.0 && r <= q)
                    return Math.Max(0.0,
                        new BlackCalculator(Option.Type.Put, K, S * Math.Exp((r - q) * T),
                                          vol * Math.Sqrt(T), Math.Exp(-r * T)).Value());

                if (Comparison.Close(vol, 0.0))
                {
                    Func<double, double> intrinsic = t => Math.Max(0.0, K * Math.Exp(-r * t) - S * Math.Exp(-q * t));
                    double npv0 = intrinsic(0.0);
                    double npvT = intrinsic(T);
                    double extremT = Comparison.CloseEnough(r, q) ? QLDefines.MAX_REAL : Math.Log(r * K / (q * S)) / (r - q);

                    if (extremT > 0.0 && extremT < T)
                        return new[] { npv0, npvT, intrinsic(extremT) }.Max();
                    else
                        return Math.Max(npv0, npvT);
                }

                return CalculatePut(S, K, r, q, vol, T);
            }
        }
    }

    /// <summary>
    /// American engine based on the QD+ approximation to the exercise boundary.
    /// </summary>
    /// <remarks>
    /// The main purpose of this engine is to provide a good initial guess to the exercise
    /// boundary for the superior fixed point American engine QdFpAmericanEngine.
    /// 
    /// References:
    /// Li, M. (2009), "Analytical Approximations for the Critical Stock Prices
    ///                 of American Options: A Performance Comparison,"
    ///                 Working paper, Georgia Institute of Technology.
    /// 
    /// https://mpra.ub.uni-muenchen.de/15018/1/MPRA_paper_15018.pdf
    /// </remarks>
    public class QdPlusAmericanEngine : Detail.QdPutCallParityEngine
    {
        public enum SolverType
        {
            Brent,
            Newton,
            Ridder,
            Halley,
            SuperHalley
        }

        private readonly int _interpolationPoints;
        private readonly SolverType _solverType;
        private readonly double _eps;
        private readonly int _maxIter;

        public QdPlusAmericanEngine(GeneralizedBlackScholesProcess process,
                                   int interpolationPoints = 8,
                                   SolverType solverType = SolverType.Halley,
                                   double eps = 1e-6,
                                   int? maxIter = null)
            : base(process)
        {
            _interpolationPoints = interpolationPoints;
            _solverType = solverType;
            _eps = eps;
            _maxIter = maxIter ?? (solverType == SolverType.Newton || 
                                  solverType == SolverType.Brent || 
                                  solverType == SolverType.Ridder ? 100 : 10);
        }

        public (int, double) PutExerciseBoundaryAtTau(double S, double K, double r, double q,
                                                     double vol, double T, double tau)
        {
            if (tau < QLDefines.EPSILON)
                return (0, XMax(K, r, q));

            var eval = new QdPlusBoundaryEvaluator(S, K, r, q, vol, tau, T);

            double x;
            switch (_solverType)
            {
                case SolverType.Brent:
                    x = BuildInSolver(eval, new Brent(), S, K, _maxIter);
                    break;
                case SolverType.Newton:
                    x = BuildInSolver(eval, new Newton(), S, K, _maxIter);
                    break;
                case SolverType.Ridder:
                    x = BuildInSolver(eval, new Ridder(), S, K, _maxIter);
                    break;
                case SolverType.Halley:
                case SolverType.SuperHalley:
                    {
                        bool resultCloseEnough;
                        x = eval.XMax;
                        double xOld, fx;
                        double xmin = eval.XMin;

                        do
                        {
                            xOld = x;
                            fx = eval.Evaluate(x);
                            double fPrime = eval.Derivative(x);
                            double lf = fx * eval.SecondDerivative(x) / (fPrime * fPrime);
                            double step = (_solverType == SolverType.Halley)
                                ? 1 / (1 - 0.5 * lf) * fx / fPrime
                                : (1 + 0.5 * lf / (1 - lf)) * fx / fPrime;

                            x = Math.Max(xmin, x - step);
                            resultCloseEnough = Math.Abs(x - xOld) < 0.5 * _eps;
                        }
                        while (!resultCloseEnough && eval.Evaluations < _maxIter);

                        if (!resultCloseEnough && !Comparison.Close(Math.Abs(fx), 0.0))
                        {
                            x = BuildInSolver(eval, new Brent(), S, K, 10 * _maxIter, x);
                        }
                    }
                    break;
                default:
                    QL.Fail("unknown solver type");
                    break;
            }

            return (eval.Evaluations, x);
        }

        public ChebyshevInterpolation GetPutExerciseBoundary(double S, double K, double r, double q, 
                                                           double vol, double T)
        {
            double xmax = XMax(K, r, q);

            return new ChebyshevInterpolation(_interpolationPoints,
                z =>
                {
                    double x_sq = 0.25 * T * Squared(1 + z);
                    return Squared(Math.Log(PutExerciseBoundaryAtTau(S, K, r, q, vol, T, x_sq).Item2 / xmax));
                },
                ChebyshevInterpolation.PointsType.SecondKind);
        }

        public static double XMax(double K, double r, double q)
        {
            // Table 2 from Leif Andersen, Mark Lake (2021)
            // "Fast American Option Pricing: The Double-Boundary Case"

            if (r > 0.0 && q > 0.0)
                return K * Math.Min(1.0, r / q);
            else if (r > 0.0 && q <= 0.0)
                return K;
            else if (r == 0.0 && q < 0.0)
                return K;
            else if (r == 0.0 && q >= 0.0)
                return 0.0; // European case
            else if (r < 0.0 && q >= 0.0)
                return 0.0; // European case
            else if (r < 0.0 && q < r)
                return K; // double boundary case
            else if (r < 0.0 && r <= q && q < 0.0)
                return 0; // European case
            else
                QL.Fail("internal error");

            return 0.0; // Never reached
        }

        protected override double CalculatePut(double S, double K, double r, double q, double vol, double T)
        {
            if (r < 0.0 && q < r)
                QL.Fail("double-boundary case q<r<0 for a put option is given");

            var q_z = GetPutExerciseBoundary(S, K, r, q, vol, T);
            double xmax = XMax(K, r, q);
            var aov = new Detail.QdPlusAddOnValue(T, S, K, r, q, vol, xmax, q_z);

            double addOn;
            try
            {
                var integral = new TanhSinhIntegral(_eps);
                addOn = integral.Integrate(aov.Evaluate, 0.0, Math.Sqrt(T));
            }
            catch
            {
                var integral = new GaussLobattoIntegral(100000, QLDefines.MAX_REAL, 0.1 * _eps);
                addOn = integral.Integrate(aov.Evaluate, 0.0, Math.Sqrt(T));
            }

            QL.Require(addOn > -10 * _eps, $"negative early exercise value {addOn}");

            double europeanValue = Math.Max(0.0,
                new BlackCalculator(Option.Type.Put, K,
                                  S * Math.Exp((r - q) * T),
                                  vol * Math.Sqrt(T), Math.Exp(-r * T)).Value());

            return europeanValue + Math.Max(0.0, addOn);
        }

        private double BuildInSolver<T>(QdPlusBoundaryEvaluator eval, T solver, double S, double strike, 
                                      int maxIter, double? guess = null) where T : new()
        {
            // Set solver parameters using reflection-like approach based on actual solver types
            if (solver is Brent brent)
            {
                brent.SetMaxEvaluations(maxIter);
                brent.SetLowerBound(eval.XMin);
            }
            else if (solver is Newton newton)
            {
                newton.SetMaxEvaluations(maxIter);
                newton.SetLowerBound(eval.XMin);
            }
            else if (solver is Ridder ridder)
            {
                ridder.SetMaxEvaluations(maxIter);
                ridder.SetLowerBound(eval.XMin);
            }

            double fxmin = eval.Evaluate(eval.XMin);
            double xmax = Math.Max(0.5 * (eval.XMax + S), eval.XMax);
            while (eval.Evaluate(xmax) * fxmin > 0.0 && eval.Evaluations < _maxIter)
                xmax *= 2;

            if (!guess.HasValue)
                guess = 0.5 * (xmax + S);

            if (guess >= xmax)
                guess = xmax - Math.Max(QLDefines.EPSILON, Math.Abs(xmax) * QLDefines.EPSILON);
            else if (guess <= eval.XMin)
                guess = eval.XMin + Math.Max(QLDefines.EPSILON, Math.Abs(eval.XMin) * QLDefines.EPSILON);

            // Use appropriate solve method based on solver type
            if (solver is Brent brentSolver)
                return brentSolver.Solve(eval.Evaluate, _eps, guess.Value, eval.XMin, xmax);
            else if (solver is Newton newtonSolver)
                return newtonSolver.Solve(eval.Evaluate, eval.Derivative, _eps, guess.Value, eval.XMin, xmax);
            else if (solver is Ridder ridderSolver)
                return ridderSolver.Solve(eval.Evaluate, _eps, guess.Value, eval.XMin, xmax);
            else
                throw new ArgumentException($"Unsupported solver type: {typeof(T)}");
        }

        private static double Squared(double x) => x * x;
    }
}