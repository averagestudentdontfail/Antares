// C# code for TrapezoidIntegral.cs

using System;

namespace Antares.Math.Integral
{
    /// <summary>
    /// Interface for integration policies used by the trapezoidal integrator.
    /// </summary>
    public interface IIntegrationPolicy
    {
        /// <summary>
        /// Performs one integration step.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">Lower integration limit.</param>
        /// <param name="b">Upper integration limit.</param>
        /// <param name="I">Previous integral approximation.</param>
        /// <param name="N">Number of intervals.</param>
        /// <returns>New integral approximation.</returns>
        double Integrate(Integrand f, double a, double b, double I, int N);

        /// <summary>
        /// Returns the number of function evaluations per refinement step.
        /// </summary>
        int NumberOfEvaluations { get; }
    }

    /// <summary>
    /// Integral of a one-dimensional function using the trapezoid formula.
    /// </summary>
    /// <remarks>
    /// Given a target accuracy ε, the integral of a function f between a and b is
    /// calculated by means of the trapezoid formula:
    /// 
    /// ∫[a,b] f dx = 1/2 f(x₀) + f(x₁) + f(x₂) + ... + f(xₙ₋₁) + 1/2 f(xₙ)
    /// 
    /// where x₀ = a, xₙ = b, and xᵢ = a + i·Δx with Δx = (b-a)/N.
    /// The number N of intervals is repeatedly increased until the target accuracy is reached.
    /// 
    /// The correctness of the result is tested by checking it against known good values.
    /// </remarks>
    /// <typeparam name="TPolicy">The integration policy to use.</typeparam>
    public class TrapezoidIntegral<TPolicy> : Integrator where TPolicy : IIntegrationPolicy, new()
    {
        private readonly TPolicy _policy;

        /// <summary>
        /// Initializes a new instance of the TrapezoidIntegral class.
        /// </summary>
        /// <param name="accuracy">The required absolute accuracy.</param>
        /// <param name="maxIterations">The maximum number of iterations.</param>
        public TrapezoidIntegral(double accuracy, int maxIterations)
            : base(accuracy, maxIterations)
        {
            _policy = new TPolicy();
        }

        /// <summary>
        /// Gets the integration policy being used.
        /// </summary>
        public TPolicy Policy => _policy;

        /// <summary>
        /// Integrates the function f from a to b using the trapezoidal rule with refinement.
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

            // Refine it
            int i = 1;
            do
            {
                newI = _policy.Integrate(f, a, b, I, N);
                EvaluationNumber += N * (_policy.NumberOfEvaluations - 1);
                N *= _policy.NumberOfEvaluations;

                // Good enough? Also, don't run away immediately
                if (Math.Abs(I - newI) <= AbsoluteAccuracy && i > 5)
                {
                    // Ok, exit
                    return newI;
                }

                // Oh well. Another step.
                I = newI;
                i++;
            } while (i < MaxEvaluations);

            QL.Fail("max number of iterations reached");
            return 0; // Unreachable
        }
    }

    /// <summary>
    /// Default integration policy for trapezoidal integration.
    /// </summary>
    /// <remarks>
    /// This policy refines the trapezoidal approximation by evaluating the function
    /// at the midpoints of the existing intervals, effectively doubling the number
    /// of intervals with each refinement step.
    /// </remarks>
    public class DefaultIntegrationPolicy : IIntegrationPolicy
    {
        /// <summary>
        /// Performs one integration step using the default trapezoidal refinement.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">Lower integration limit.</param>
        /// <param name="b">Upper integration limit.</param>
        /// <param name="I">Previous integral approximation.</param>
        /// <param name="N">Number of intervals in the previous approximation.</param>
        /// <returns>New integral approximation with 2N intervals.</returns>
        public double Integrate(Integrand f, double a, double b, double I, int N)
        {
            double sum = 0.0;
            double dx = (b - a) / N;
            double x = a + dx / 2.0;
            
            for (int i = 0; i < N; i++, x += dx)
                sum += f(x);
            
            return (I + dx * sum) / 2.0;
        }

        /// <summary>
        /// Returns the number of function evaluations per refinement step.
        /// Each refinement doubles the number of intervals.
        /// </summary>
        public int NumberOfEvaluations => 2;
    }

    /// <summary>
    /// Midpoint integration policy for trapezoidal integration.
    /// </summary>
    /// <remarks>
    /// This policy refines the trapezoidal approximation using a different sampling
    /// strategy that triples the number of intervals with each refinement step.
    /// It evaluates the function at two points within each existing interval.
    /// </remarks>
    public class MidPointIntegrationPolicy : IIntegrationPolicy
    {
        /// <summary>
        /// Performs one integration step using the midpoint refinement.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">Lower integration limit.</param>
        /// <param name="b">Upper integration limit.</param>
        /// <param name="I">Previous integral approximation.</param>
        /// <param name="N">Number of intervals in the previous approximation.</param>
        /// <returns>New integral approximation with 3N intervals.</returns>
        public double Integrate(Integrand f, double a, double b, double I, int N)
        {
            double sum = 0.0;
            double dx = (b - a) / N;
            double x = a + dx / 6.0;
            double D = 2.0 * dx / 3.0;
            
            for (int i = 0; i < N; i++, x += dx)
                sum += f(x) + f(x + D);
            
            return (I + dx * sum) / 3.0;
        }

        /// <summary>
        /// Returns the number of function evaluations per refinement step.
        /// Each refinement triples the number of intervals.
        /// </summary>
        public int NumberOfEvaluations => 3;
    }

    /// <summary>
    /// Convenience class for default trapezoidal integration.
    /// </summary>
    /// <remarks>
    /// This class provides a non-generic interface to trapezoidal integration
    /// using the default refinement policy (doubling intervals each step).
    /// </remarks>
    public class TrapezoidIntegral : TrapezoidIntegral<DefaultIntegrationPolicy>
    {
        /// <summary>
        /// Initializes a new instance of the TrapezoidIntegral class.
        /// </summary>
        /// <param name="accuracy">The required absolute accuracy.</param>
        /// <param name="maxIterations">The maximum number of iterations.</param>
        public TrapezoidIntegral(double accuracy, int maxIterations)
            : base(accuracy, maxIterations) { }
    }

    /// <summary>
    /// Convenience class for midpoint trapezoidal integration.
    /// </summary>
    /// <remarks>
    /// This class provides a non-generic interface to trapezoidal integration
    /// using the midpoint refinement policy (tripling intervals each step).
    /// </remarks>
    public class MidPointTrapezoidIntegral : TrapezoidIntegral<MidPointIntegrationPolicy>
    {
        /// <summary>
        /// Initializes a new instance of the MidPointTrapezoidIntegral class.
        /// </summary>
        /// <param name="accuracy">The required absolute accuracy.</param>
        /// <param name="maxIterations">The maximum number of iterations.</param>
        public MidPointTrapezoidIntegral(double accuracy, int maxIterations)
            : base(accuracy, maxIterations) { }
    }
}