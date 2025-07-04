// C# code for TanhSinhIntegral.cs

using System;

namespace Antares.Math.Integral
{
    /// <summary>
    /// The tanh-sinh quadrature routine is a rapidly convergent numerical integration 
    /// scheme for holomorphic integrands. The tolerance is used against the error 
    /// estimate for the L1 norm of the integral.
    /// </summary>
    /// <remarks>
    /// The tanh-sinh method uses the transformation x = tanh(π/2 * sinh(t)) to map
    /// the integration interval. This transformation is particularly effective for
    /// functions with endpoint singularities or infinite intervals.
    /// 
    /// This implementation provides a pure C# version of tanh-sinh quadrature,
    /// replacing the boost library dependency with a custom implementation.
    /// 
    /// Should check validity, as this is more complex, than what is beholden to the
    /// eye at first glance.
    /// </remarks>
    public class TanhSinhIntegral : Integrator
    {
        private readonly double _relativeTolerance;
        private readonly int _maxRefinements;
        private readonly double _minComplement;
        private double _absoluteError;

        /// <summary>
        /// Initializes a new instance of the TanhSinhIntegral class.
        /// </summary>
        /// <param name="relTolerance">The relative tolerance for convergence.</param>
        /// <param name="maxRefinements">The maximum number of refinement levels.</param>
        /// <param name="minComplement">The minimum complement to avoid underflow.</param>
        public TanhSinhIntegral(double relTolerance = 1e-8,
                               int maxRefinements = 15,
                               double minComplement = 4 * double.Epsilon)
            : base(double.MaxValue, int.MaxValue)
        {
            _relativeTolerance = relTolerance;
            _maxRefinements = maxRefinements;
            _minComplement = minComplement;
            _absoluteError = 0.0;
        }

        /// <summary>
        /// Gets the relative tolerance used by the integrator.
        /// </summary>
        public double RelativeTolerance => _relativeTolerance;

        /// <summary>
        /// Gets the maximum number of refinements.
        /// </summary>
        public int MaxRefinements => _maxRefinements;

        /// <summary>
        /// Gets the minimum complement parameter.
        /// </summary>
        public double MinComplement => _minComplement;

        /// <summary>
        /// Gets the absolute error from the last integration.
        /// </summary>
        public override double AbsoluteError => _absoluteError;

        /// <summary>
        /// Integrates the function f from a to b using tanh-sinh quadrature.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <param name="a">The lower limit of integration.</param>
        /// <param name="b">The upper limit of integration.</param>
        /// <returns>The approximate value of the integral.</returns>
        protected override double Integrate(Integrand f, double a, double b)
        {
            EvaluationNumber = 0;
            _absoluteError = 0.0;

            // Handle special cases
            if (double.IsInfinity(a) && double.IsInfinity(b))
            {
                // Integration over (-∞, ∞)
                return IntegrateInfinite(f);
            }
            else if (double.IsInfinity(a))
            {
                // Integration over (-∞, b) - transform to (0, ∞)
                Integrand g = t => f(b - t);
                return IntegrateSemiInfinitePositive(g);
            }
            else if (double.IsInfinity(b))
            {
                // Integration over (a, ∞) - transform to (0, ∞)
                Integrand g = t => f(a + t);
                return IntegrateSemiInfinitePositive(g);
            }
            else
            {
                // Integration over finite interval [a, b]
                return IntegrateFinite(f, a, b);
            }
        }

        /// <summary>
        /// Integrates over a finite interval [a, b].
        /// </summary>
        private double IntegrateFinite(Integrand f, double a, double b)
        {
            // For finite intervals, we can use the standard tanh-sinh transformation
            // The interval [a,b] is already handled by the TanhSinhTransform method
            return IntegrateCore(f, a, b);
        }

        /// <summary>
        /// Integrates over an infinite interval (-∞, ∞).
        /// </summary>
        private double IntegrateInfinite(Integrand f)
        {
            double h = 1.0;
            double sum = 0.0;
            double previous_sum = 0.0;
            
            // Central point (t=0): x = tanh(π/2 * sinh(0)) = tanh(0) = 0
            // Weight = π/2 * cosh(0) / cosh²(π/2 * sinh(0)) = π/2 * 1 / 1² = π/2
            sum = Math.PI * 0.5 * f(0.0);
            EvaluationNumber++;
            
            for (int level = 1; level <= _maxRefinements; level++)
            {
                previous_sum = sum;
                double new_sum = 0.0;
                
                for (int k = 1; k < 50; k += 2)
                {
                    double t_pos = k * h;
                    double t_neg = -k * h;
                    
                    if (Math.Abs(t_pos) > 3.5) break;
                    
                    double sinh_pos = Math.Sinh(t_pos);
                    double sinh_neg = Math.Sinh(t_neg);
                    double cosh_t = Math.Cosh(t_pos); // cosh is even function
                    
                    double arg_pos = Math.PI * 0.5 * sinh_pos;
                    double arg_neg = Math.PI * 0.5 * sinh_neg;
                    
                    // Avoid overflow
                    if (Math.Abs(arg_pos) > 15.0 || Math.Abs(arg_neg) > 15.0) break;
                    
                    double x_pos = Math.Tanh(arg_pos);
                    double x_neg = Math.Tanh(arg_neg);
                    
                    double cosh_arg_pos = Math.Cosh(arg_pos);
                    double cosh_arg_neg = Math.Cosh(arg_neg);
                    
                    double weight_pos = Math.PI * 0.5 * cosh_t / (cosh_arg_pos * cosh_arg_pos);
                    double weight_neg = Math.PI * 0.5 * cosh_t / (cosh_arg_neg * cosh_arg_neg);
                    
                    if (Math.Abs(weight_pos) > _minComplement)
                    {
                        new_sum += weight_pos * f(x_pos);
                        EvaluationNumber++;
                    }
                    
                    if (Math.Abs(weight_neg) > _minComplement)
                    {
                        new_sum += weight_neg * f(x_neg);
                        EvaluationNumber++;
                    }
                    
                    if (Math.Abs(weight_pos) < _minComplement && Math.Abs(weight_neg) < _minComplement)
                        break;
                }
                
                sum += new_sum;
                h *= 0.5;
                
                if (level > 1)
                {
                    double error_estimate = Math.Abs(sum - previous_sum - new_sum);
                    _absoluteError = error_estimate;
                    
                    if (error_estimate <= _relativeTolerance * Math.Abs(sum))
                        break;
                }
            }
            
            return sum;
        }

        /// <summary>
        /// Integrates over a semi-infinite interval (0, ∞).
        /// </summary>
        private double IntegrateSemiInfinitePositive(Integrand f)
        {
            // For (0,∞), use the transformation x = exp(sinh(t))
            // This maps (-∞,∞) to (0,∞)
            double h = 1.0;
            double sum = 0.0;
            double previous_sum = 0.0;
            
            // Central point (t=0): x = exp(sinh(0)) = exp(0) = 1
            // Weight = cosh(0) * exp(sinh(0)) = 1 * 1 = 1
            sum = f(1.0);
            EvaluationNumber++;
            
            for (int level = 1; level <= _maxRefinements; level++)
            {
                previous_sum = sum;
                double new_sum = 0.0;
                
                for (int k = 1; k < 50; k += 2)
                {
                    double t_pos = k * h;
                    double t_neg = -k * h;
                    
                    if (Math.Abs(t_pos) > 5.0) break;
                    
                    // x = exp(sinh(t)), dx/dt = cosh(t) * exp(sinh(t))
                    double sinh_pos = Math.Sinh(t_pos);
                    double sinh_neg = Math.Sinh(t_neg);
                    double cosh_pos = Math.Cosh(t_pos);
                    double cosh_neg = Math.Cosh(t_neg);
                    
                    double x_pos = Math.Exp(sinh_pos);
                    double x_neg = Math.Exp(sinh_neg);
                    
                    double weight_pos = cosh_pos * x_pos;
                    double weight_neg = cosh_neg * x_neg;
                    
                    if (Math.Abs(weight_pos) > _minComplement && x_pos < 1e10)
                    {
                        new_sum += weight_pos * f(x_pos);
                        EvaluationNumber++;
                    }
                    
                    if (Math.Abs(weight_neg) > _minComplement && x_neg > 1e-10)
                    {
                        new_sum += weight_neg * f(x_neg);
                        EvaluationNumber++;
                    }
                    
                    if (x_pos > 1e10 && x_neg < 1e-10) break;
                }
                
                sum += new_sum;
                h *= 0.5;
                
                if (level > 1)
                {
                    double error_estimate = Math.Abs(sum - previous_sum - new_sum);
                    _absoluteError = error_estimate;
                    
                    if (error_estimate <= _relativeTolerance * Math.Abs(sum))
                        break;
                }
            }
            
            return sum;
        }

        /// <summary>
        /// Core integration routine using adaptive tanh-sinh quadrature.
        /// </summary>
        private double IntegrateCore(Integrand f, double a, double b)
        {
            double h = 1.0; // Initial step size in t-space
            double sum = 0.0;
            double previous_sum = 0.0;
            
            // Compute central point (t=0)
            double center_x = (a + b) * 0.5;
            double center_weight = (b - a) * 0.5;
            sum = center_weight * f(center_x);
            EvaluationNumber++;
            
            for (int level = 1; level <= _maxRefinements; level++)
            {
                previous_sum = sum;
                double new_sum = 0.0;
                
                // Generate symmetric points: ±k*h for k = 1, 3, 5, ... (odd values)
                for (int k = 1; k < 50; k += 2) // Limit range to avoid overflow
                {
                    double t = k * h;
                    
                    // Apply tanh-sinh transformation for both +t and -t
                    var (x_pos, w_pos) = TanhSinhTransform(t, a, b);
                    var (x_neg, w_neg) = TanhSinhTransform(-t, a, b);
                    
                    if (double.IsFinite(x_pos) && double.IsFinite(w_pos) && Math.Abs(w_pos) > _minComplement)
                    {
                        new_sum += w_pos * f(x_pos);
                        EvaluationNumber++;
                    }
                    
                    if (double.IsFinite(x_neg) && double.IsFinite(w_neg) && Math.Abs(w_neg) > _minComplement)
                    {
                        new_sum += w_neg * f(x_neg);
                        EvaluationNumber++;
                    }
                    
                    // Stop if weights become negligible
                    if (Math.Abs(w_pos) < _minComplement && Math.Abs(w_neg) < _minComplement)
                        break;
                }
                
                sum += new_sum;
                h *= 0.5; // Halve step size for next level
                
                // Check for convergence
                if (level > 1)
                {
                    double error_estimate = Math.Abs(sum - previous_sum - new_sum);
                    _absoluteError = error_estimate;
                    
                    if (error_estimate <= _relativeTolerance * Math.Abs(sum))
                    {
                        break;
                    }
                }
            }
            
            return sum;
        }

        /// <summary>
        /// Applies the tanh-sinh transformation for a given t value.
        /// </summary>
        /// <param name="t">The parameter in the tanh-sinh space.</param>
        /// <param name="a">Lower integration limit.</param>
        /// <param name="b">Upper integration limit.</param>
        /// <returns>The transformed x value and its corresponding weight.</returns>
        private static (double x, double weight) TanhSinhTransform(double t, double a, double b)
        {
            if (Math.Abs(t) > 3.5) // Avoid overflow in sinh/cosh
                return (double.NaN, 0.0);
            
            double sinh_t = Math.Sinh(t);
            double cosh_t = Math.Cosh(t);
            double u = Math.Tanh(Math.PI * 0.5 * sinh_t);
            
            // Transform from [-1,1] to [a,b]
            double x = 0.5 * ((b - a) * u + (a + b));
            
            // Compute weight: (b-a)/2 * (π/2) * cosh(t) / cosh²(π/2 * sinh(t))
            double cosh_arg = Math.Cosh(Math.PI * 0.5 * sinh_t);
            double weight = 0.5 * (b - a) * Math.PI * 0.5 * cosh_t / (cosh_arg * cosh_arg);
            
            return (x, weight);
        }
    }
}