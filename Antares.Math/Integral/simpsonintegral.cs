// C# code for SimpsonIntegral.cs

using System;

namespace Antares.Math.Integral
{
    /// <summary>
    /// Integral of a one-dimensional function using Simpson's rule.
    /// </summary>
    /// <remarks>
    /// Simpson's rule is derived from the trapezoidal rule using Richardson extrapolation.
    /// The algorithm starts with a coarse trapezoidal approximation and refines it by
    /// successively doubling the number of intervals, then applies Richardson extrapolation
    /// to achieve the higher-order accuracy of Simpson's rule.
    /// 
    /// The correctness of the result is tested by checking it against known good values.
    /// </remarks>
    public class SimpsonIntegral : Integrator
    {
        /// <summary>
        /// Initializes a new instance of the SimpsonIntegral class.
        /// </summary>
        /// <param name="accuracy">The required absolute accuracy.</param>
        /// <param name="maxIterations">The maximum number of iterations.</param>
        public SimpsonIntegral(double accuracy, int maxIterations)
            : base(accuracy, maxIterations)
        {
        }

        /// <summary>
        /// Integrates the function f from a to b using Simpson's rule with adaptive refinement.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        /// <returns>The approximate value of the integral.</returns>
        protected override double Integrate(Integrand f, double a, double b)
        {
            // Start from the coarsest trapezoid
            int N = 1;
            double I = (f(a) + f(b)) * (b - a) / 2.0;
            double newI;
            EvaluationNumber += 2;

            double adjI = I, newAdjI;
            
            // Refine the trapezoidal approximation and apply Richardson extrapolation
            int i = 1;
            do
            {
                newI = TrapezoidalStep(f, a, b, I, N);
                EvaluationNumber += N;
                N *= 2;
                
                // Apply Richardson extrapolation: Simpson = (4*T_2h - T_h) / 3
                newAdjI = (4.0 * newI - I) / 3.0;
                
                // Check for convergence (but don't exit too early)
                if (Math.Abs(adjI - newAdjI) <= AbsoluteAccuracy && i > 5)
                {
                    return newAdjI;
                }
                
                // Prepare for next iteration
                I = newI;
                adjI = newAdjI;
                i++;
            } while (i < MaxEvaluations);
            
            QL.Fail("max number of iterations reached");
            return 0; // Unreachable
        }

        /// <summary>
        /// Performs one step of trapezoidal rule refinement.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        /// <param name="s">The previous trapezoidal approximation.</param>
        /// <param name="n">The number of intervals to add.</param>
        /// <returns>The refined trapezoidal approximation.</returns>
        private static double TrapezoidalStep(Integrand f, double a, double b, double s, int n)
        {
            if (n == 1)
            {
                // First refinement: add the midpoint
                return 0.5 * (s + (b - a) * f((a + b) / 2.0));
            }
            else
            {
                // General refinement: add n equally spaced interior points
                double h = (b - a) / n;
                double sum = 0.0;
                double x = a + 0.5 * h;
                
                for (int i = 0; i < n; i++)
                {
                    sum += f(x);
                    x += h;
                }
                
                return 0.5 * (s + h * sum);
            }
        }
    }
}