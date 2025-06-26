using System;

namespace Anderson.Integrator.Integrators
{
    /// <summary>
    /// Implements an adaptive Gauss-Lobatto quadrature for numerical integration.
    /// This integrator is suitable for high-precision requirements where the function
    /// behavior may vary, as it adaptively refines the integration interval.
    /// This is a C# port of the QuantLib C++ implementation.
    /// </summary>
    public class GaussLobattoIntegral : Integrator
    {
        private readonly double _relativeAccuracy;
        private readonly bool _useConvergenceEstimate;

        // Constants from the Gander and Gautschi paper, used in the algorithm
        private const double Alpha = 0.44721359549995793928183473374626; // sqrt(2.0/10.0)
        private const double Beta = 0.89442719099991587856366946749251;  // sqrt(4.0/5.0)
        private const double X1 = 0.94288241569547971905634033403363;
        private const double X2 = 0.64185334234578131343102179924996;
        private const double X3 = 0.23638319966214988079223344685324;

        public GaussLobattoIntegral(
            int maxEvaluations,
            double absoluteAccuracy,
            double relativeAccuracy = 0.0,
            bool useConvergenceEstimate = true)
            : base(absoluteAccuracy, maxEvaluations)
        {
            _relativeAccuracy = relativeAccuracy;
            _useConvergenceEstimate = useConvergenceEstimate;
        }

        protected override double IntegrateImpl(Func<double, double> f, double a, double b)
        {
            double fa = f(a);
            double fb = f(b);
            NumberOfEvaluations = 2;

            double tolerance = CalculateAbsTolerance(f, a, b);
            
            return AdaptiveGaussLobattoStep(f, a, b, fa, fb, tolerance);
        }

        private double AdaptiveGaussLobattoStep(Func<double, double> f, double a, double b, double fa, double fb, double acc)
        {
            if (NumberOfEvaluations > MaxEvaluations)
            {
                // We should log this or handle it as per QC's best practices
                return double.NaN; // Or throw
            }

            double h = (b - a) / 2.0;
            double m = (a + b) / 2.0;

            double mll = m - Alpha * h;
            double ml = m - Beta * h;
            double mr = m + Beta * h;
            double mrr = m + Alpha * h;

            double fmll = f(mll);
            double fml = f(ml);
            double fm = f(m);
            double fmr = f(mr);
            double fmrr = f(mrr);
            NumberOfEvaluations += 5;

            double i2 = h / 6.0 * (fa + fb + 5.0 * (fml + fmr));
            double i1 = h / 14700.0 * (77.0 * (fa + fb)
                                       + 432.0 * (fmll + fmrr)
                                       + 625.0 * (fml + fmr)
                                       + 672.0 * fm);

            double dist = h * (1024.0 * (fm - fmll - fmrr)
                               - 432.0 * (fb - fa)
                               + 735.0 * (fml - fmr));

            double convergence = Math.Pow(dist / 14700.0, 2);
            double error = Math.Abs(i1 - i2);
            double result = i1;

            if (_useConvergenceEstimate)
            {
                result = i1 / (1.0 - convergence);
                error = Math.Abs((i1-i2)/(1.0-convergence));
            }

            if (error < acc * Math.Sqrt(h))
            {
                AbsoluteError += error;
                return result;
            }

            if (NumberOfEvaluations > MaxEvaluations)
            {
                return double.NaN;
            }
            
            double m1 = AdaptiveGaussLobattoStep(f, a, mll, fa, fmll, acc);
            double m2 = AdaptiveGaussLobattoStep(f, mll, ml, fmll, fml, acc);
            double m3 = AdaptiveGaussLobattoStep(f, ml, m, fml, fm, acc);
            double m4 = AdaptiveGaussLobattoStep(f, m, mr, fm, fmr, acc);
            double m5 = AdaptiveGaussLobattoStep(f, mr, mrr, fmr, fmrr, acc);
            double m6 = AdaptiveGaussLobattoStep(f, mrr, b, fmrr, fb, acc);

            return m1 + m2 + m3 + m4 + m5 + m6;
        }

        private double CalculateAbsTolerance(Func<double, double> f, double a, double b)
        {
            if (_relativeAccuracy > 0.0)
            {
                double i1 = f(a);
                double i2 = f((a+b)*0.5);
                double i3 = f(b);

                double integral = (i1+i2+i3)*(b-a)/3.0;
                return Math.Max(AbsoluteAccuracy, _relativeAccuracy * Math.Abs(integral));
            }
            return AbsoluteAccuracy;
        }
    }
}