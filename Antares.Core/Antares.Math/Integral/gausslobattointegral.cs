// C# code for GaussLobattoIntegral.cs

using System;

namespace Antares.Math.Integral
{
    /// <summary>
    /// Integral of a one-dimensional function using the adaptive Gauss-Lobatto integral.
    /// </summary>
    /// <remarks>
    /// Given a target accuracy ε, the integral of a function f between a and b is
    /// calculated by means of the Gauss-Lobatto formula.
    /// 
    /// References:
    /// This algorithm is a C# implementation of the algorithm outlined in
    /// 
    /// W. Gander and W. Gautschi, Adaptive Quadrature - Revisited.
    /// BIT, 40(1):84-101, March 2000. CS technical report:
    /// ftp.inf.ethz.ch/pub/publications/tech-reports/3xx/306.ps.gz
    /// 
    /// The original MATLAB version can be downloaded here
    /// http://www.inf.ethz.ch/personal/gander/adaptlob.m
    /// </remarks>
    public class GaussLobattoIntegral : Integrator
    {
        private readonly double? _relativeAccuracy;
        private readonly bool _useConvergenceEstimate;

        // Gauss-Lobatto quadrature constants
        private static readonly double Alpha = Math.Sqrt(2.0 / 3.0);
        private static readonly double Beta = 1.0 / Math.Sqrt(5.0);
        private static readonly double X1 = 0.94288241569547971906;
        private static readonly double X2 = 0.64185334234578130578;
        private static readonly double X3 = 0.23638319966214988028;

        /// <summary>
        /// Initializes a new instance of the GaussLobattoIntegral class.
        /// </summary>
        /// <param name="maxIterations">The maximum number of iterations.</param>
        /// <param name="absAccuracy">The required absolute accuracy.</param>
        /// <param name="relAccuracy">The required relative accuracy. If null, only absolute accuracy is used.</param>
        /// <param name="useConvergenceEstimate">Whether to use convergence estimation for improved accuracy control.</param>
        public GaussLobattoIntegral(int maxIterations,
                                   double absAccuracy,
                                   double? relAccuracy = null,
                                   bool useConvergenceEstimate = true)
            : base(absAccuracy, maxIterations)
        {
            _relativeAccuracy = relAccuracy;
            _useConvergenceEstimate = useConvergenceEstimate;
        }

        /// <summary>
        /// Gets the relative accuracy setting.
        /// </summary>
        public double? RelativeAccuracy => _relativeAccuracy;

        /// <summary>
        /// Gets whether convergence estimation is enabled.
        /// </summary>
        public bool UseConvergenceEstimate => _useConvergenceEstimate;

        /// <summary>
        /// Integrates the function f from a to b using adaptive Gauss-Lobatto quadrature.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        /// <returns>The approximate value of the integral.</returns>
        protected override double Integrate(Integrand f, double a, double b)
        {
            EvaluationNumber = 0;
            double calcAbsTolerance = CalculateAbsTolerance(f, a, b);

            EvaluationNumber += 2;
            return AdaptiveGaussLobattoStep(f, a, b, f(a), f(b), calcAbsTolerance);
        }

        /// <summary>
        /// Calculates the absolute tolerance for the adaptive algorithm.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        /// <returns>The calculated absolute tolerance.</returns>
        private double CalculateAbsTolerance(Integrand f, double a, double b)
        {
            double relTol = Math.Max(_relativeAccuracy ?? 0.0, QLDefines.EPSILON);

            double m = (a + b) / 2;
            double h = (b - a) / 2;
            double y1 = f(a);
            double y3 = f(m - Alpha * h);
            double y5 = f(m - Beta * h);
            double y7 = f(m);
            double y9 = f(m + Beta * h);
            double y11 = f(m + Alpha * h);
            double y13 = f(b);

            double f1 = f(m - X1 * h);
            double f2 = f(m + X1 * h);
            double f3 = f(m - X2 * h);
            double f4 = f(m + X2 * h);
            double f5 = f(m - X3 * h);
            double f6 = f(m + X3 * h);

            double acc = h * (0.0158271919734801831 * (y1 + y13)
                            + 0.0942738402188500455 * (f1 + f2)
                            + 0.1550719873365853963 * (y3 + y11)
                            + 0.1888215739601824544 * (f3 + f4)
                            + 0.1997734052268585268 * (y5 + y9)
                            + 0.2249264653333395270 * (f5 + f6)
                            + 0.2426110719014077338 * y7);

            EvaluationNumber += 13;

            if (acc == 0.0 && (f1 != 0.0 || f2 != 0.0 || f3 != 0.0
                              || f4 != 0.0 || f5 != 0.0 || f6 != 0.0))
            {
                QL.Fail("can not calculate absolute accuracy from relative accuracy");
            }

            double r = 1.0;
            if (_useConvergenceEstimate)
            {
                double integral2 = (h / 6) * (y1 + y13 + 5 * (y5 + y9));
                double integral1 = (h / 1470) * (77 * (y1 + y13) + 432 * (y3 + y11) +
                                                 625 * (y5 + y9) + 672 * y7);

                if (Math.Abs(integral2 - acc) != 0.0)
                    r = Math.Abs(integral1 - acc) / Math.Abs(integral2 - acc);
                if (r == 0.0 || r > 1.0)
                    r = 1.0;
            }

            if (_relativeAccuracy.HasValue)
                return Math.Min(AbsoluteAccuracy, acc * relTol) / (r * QLDefines.EPSILON);
            else
                return AbsoluteAccuracy / (r * QLDefines.EPSILON);
        }

        /// <summary>
        /// Performs one step of the adaptive Gauss-Lobatto integration.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of the current interval.</param>
        /// <param name="b">The upper limit of the current interval.</param>
        /// <param name="fa">The function value at a.</param>
        /// <param name="fb">The function value at b.</param>
        /// <param name="acc">The accuracy tolerance for this interval.</param>
        /// <returns>The integral approximation for the interval [a, b].</returns>
        private double AdaptiveGaussLobattoStep(Integrand f,
                                               double a, double b, double fa, double fb,
                                               double acc)
        {
            QL.Require(EvaluationNumber < MaxEvaluations,
                      "max number of iterations reached");

            double h = (b - a) / 2;
            double m = (a + b) / 2;

            double mll = m - Alpha * h;
            double ml = m - Beta * h;
            double mr = m + Beta * h;
            double mrr = m + Alpha * h;

            double fmll = f(mll);
            double fml = f(ml);
            double fm = f(m);
            double fmr = f(mr);
            double fmrr = f(mrr);
            EvaluationNumber += 5;

            double integral2 = (h / 6) * (fa + fb + 5 * (fml + fmr));
            double integral1 = (h / 1470) * (77 * (fa + fb)
                                            + 432 * (fmll + fmrr) + 625 * (fml + fmr) + 672 * fm);

            // Avoid 80-bit logic on x86 cpu
            double dist = acc + (integral1 - integral2);
            if (dist == acc || mll <= a || b <= mrr)
            {
                QL.Require(m > a && b > m, "Interval contains no more machine number");
                return integral1;
            }
            else
            {
                return AdaptiveGaussLobattoStep(f, a, mll, fa, fmll, acc)
                     + AdaptiveGaussLobattoStep(f, mll, ml, fmll, fml, acc)
                     + AdaptiveGaussLobattoStep(f, ml, m, fml, fm, acc)
                     + AdaptiveGaussLobattoStep(f, m, mr, fm, fmr, acc)
                     + AdaptiveGaussLobattoStep(f, mr, mrr, fmr, fmrr, acc)
                     + AdaptiveGaussLobattoStep(f, mrr, b, fmrr, fb, acc);
            }
        }
    }
}