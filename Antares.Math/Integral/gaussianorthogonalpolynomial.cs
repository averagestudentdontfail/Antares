// C# code for GaussianOrthogonalPolynomial.cs

using System;
using Antares.Math.Distribution;

namespace Antares.Math.Integral
{
    /// <summary>
    /// Orthogonal polynomial for Gaussian quadratures.
    /// </summary>
    /// <remarks>
    /// References:
    /// Gauss quadratures and orthogonal polynomials
    /// 
    /// G.H. Gloub and J.H. Welsch: Calculation of Gauss quadrature rule.
    /// Math. Comput. 23 (1986), 221-230
    /// 
    /// "Numerical Recipes in C", 2nd edition,
    /// Press, Teukolsky, Vetterling, Flannery,
    /// 
    /// The polynomials are defined by the three-term recurrence relation
    /// P_{k+1}(x) = (x - α_k) P_k(x) - β_k P_{k-1}(x)
    /// and
    /// μ_0 = ∫ w(x) dx
    /// </remarks>
    public abstract class GaussianOrthogonalPolynomial
    {
        /// <summary>
        /// Returns the zeroth moment μ_0 = ∫ w(x) dx.
        /// </summary>
        public abstract Real Mu0 { get; }

        /// <summary>
        /// Returns the coefficient α_i in the three-term recurrence relation.
        /// </summary>
        /// <param name="i">The index.</param>
        /// <returns>The coefficient α_i.</returns>
        public abstract Real Alpha(Size i);

        /// <summary>
        /// Returns the coefficient β_i in the three-term recurrence relation.
        /// </summary>
        /// <param name="i">The index.</param>
        /// <returns>The coefficient β_i.</returns>
        public abstract Real Beta(Size i);

        /// <summary>
        /// Returns the weight function w(x).
        /// </summary>
        /// <param name="x">The point at which to evaluate the weight function.</param>
        /// <returns>The value of the weight function at x.</returns>
        public abstract Real W(Real x);

        /// <summary>
        /// Evaluates the n-th polynomial at point x using the three-term recurrence relation.
        /// </summary>
        /// <param name="n">The degree of the polynomial.</param>
        /// <param name="x">The point at which to evaluate the polynomial.</param>
        /// <returns>The value of the n-th polynomial at x.</returns>
        public Real Value(Size n, Real x)
        {
            if (n > 1)
            {
                return (x - Alpha(n - 1)) * Value(n - 1, x) - Beta(n - 1) * Value(n - 2, x);
            }
            else if (n == 1)
            {
                return x - Alpha(0);
            }

            return 1.0;
        }

        /// <summary>
        /// Evaluates the weighted n-th polynomial at point x.
        /// </summary>
        /// <param name="n">The degree of the polynomial.</param>
        /// <param name="x">The point at which to evaluate the weighted polynomial.</param>
        /// <returns>The value of sqrt(w(x)) * P_n(x).</returns>
        public Real WeightedValue(Size n, Real x)
        {
            return Math.Sqrt(W(x)) * Value(n, x);
        }
    }

    /// <summary>
    /// Gauss-Laguerre polynomial.
    /// </summary>
    /// <remarks>
    /// The Laguerre polynomials are orthogonal on [0, ∞) with weight function w(x) = x^s * e^(-x).
    /// </remarks>
    public class GaussLaguerrePolynomial : GaussianOrthogonalPolynomial
    {
        private readonly Real _s;

        /// <summary>
        /// Initializes a new instance of the GaussLaguerrePolynomial class.
        /// </summary>
        /// <param name="s">The parameter s. Must be greater than -1.</param>
        public GaussLaguerrePolynomial(Real s = 0.0)
        {
            QL.Require(s > -1.0, "s must be bigger than -1");
            _s = s;
        }

        /// <summary>
        /// Returns the zeroth moment μ_0 = Γ(s+1).
        /// </summary>
        public override Real Mu0 => Math.Exp(new GammaFunction().LogValue(_s + 1));

        /// <summary>
        /// Returns the coefficient α_i = 2i + 1 + s.
        /// </summary>
        public override Real Alpha(Size i) => 2 * i + 1 + _s;

        /// <summary>
        /// Returns the coefficient β_i = i(i + s).
        /// </summary>
        public override Real Beta(Size i) => i * (i + _s);

        /// <summary>
        /// Returns the weight function w(x) = x^s * e^(-x).
        /// </summary>
        public override Real W(Real x) => Math.Pow(x, _s) * Math.Exp(-x);
    }

    /// <summary>
    /// Gauss-Hermite polynomial.
    /// </summary>
    /// <remarks>
    /// The Hermite polynomials are orthogonal on (-∞, ∞) with weight function w(x) = |x|^(2μ) * e^(-x²).
    /// </remarks>
    public class GaussHermitePolynomial : GaussianOrthogonalPolynomial
    {
        private readonly Real _mu;

        /// <summary>
        /// Initializes a new instance of the GaussHermitePolynomial class.
        /// </summary>
        /// <param name="mu">The parameter μ. Must be greater than -0.5.</param>
        public GaussHermitePolynomial(Real mu = 0.0)
        {
            QL.Require(mu > -0.5, "mu must be bigger than -0.5");
            _mu = mu;
        }

        /// <summary>
        /// Returns the zeroth moment μ_0 = Γ(μ + 0.5).
        /// </summary>
        public override Real Mu0 => Math.Exp(new GammaFunction().LogValue(_mu + 0.5));

        /// <summary>
        /// Returns the coefficient α_i = 0.
        /// </summary>
        public override Real Alpha(Size i) => 0.0;

        /// <summary>
        /// Returns the coefficient β_i.
        /// </summary>
        public override Real Beta(Size i)
        {
            return (i % 2) != 0 ? i / 2.0 + _mu : i / 2.0;
        }

        /// <summary>
        /// Returns the weight function w(x) = |x|^(2μ) * e^(-x²).
        /// </summary>
        public override Real W(Real x) => Math.Pow(Math.Abs(x), 2 * _mu) * Math.Exp(-x * x);
    }

    /// <summary>
    /// Gauss-Jacobi polynomial.
    /// </summary>
    /// <remarks>
    /// The Jacobi polynomials are orthogonal on [-1, 1] with weight function w(x) = (1-x)^α * (1+x)^β.
    /// </remarks>
    public class GaussJacobiPolynomial : GaussianOrthogonalPolynomial
    {
        private readonly Real _alpha;
        private readonly Real _beta;

        /// <summary>
        /// Initializes a new instance of the GaussJacobiPolynomial class.
        /// </summary>
        /// <param name="alpha">The parameter α. Must be greater than -1.</param>
        /// <param name="beta">The parameter β. Must be greater than -1.</param>
        public GaussJacobiPolynomial(Real alpha, Real beta)
        {
            QL.Require(alpha + beta > -2.0, "alpha+beta must be bigger than -2");
            QL.Require(alpha > -1.0, "alpha must be bigger than -1");
            QL.Require(beta > -1.0, "beta must be bigger than -1");
            _alpha = alpha;
            _beta = beta;
        }

        /// <summary>
        /// Returns the zeroth moment μ_0.
        /// </summary>
        public override Real Mu0
        {
            get
            {
                return Math.Pow(2.0, _alpha + _beta + 1)
                       * Math.Exp(new GammaFunction().LogValue(_alpha + 1)
                                  + new GammaFunction().LogValue(_beta + 1)
                                  - new GammaFunction().LogValue(_alpha + _beta + 2));
            }
        }

        /// <summary>
        /// Returns the coefficient α_i.
        /// </summary>
        public override Real Alpha(Size i)
        {
            Real num = _beta * _beta - _alpha * _alpha;
            Real denom = (2.0 * i + _alpha + _beta) * (2.0 * i + _alpha + _beta + 2);

            if (Comparison.CloseEnough(denom, 0.0))
            {
                if (!Comparison.CloseEnough(num, 0.0))
                {
                    QL.Fail("can't compute a_k for jacobi integration");
                }
                else
                {
                    // L'Hôpital's rule
                    num = 2 * _beta;
                    denom = 2 * (2.0 * i + _alpha + _beta + 1);

                    QL.Assert(!Comparison.CloseEnough(denom, 0.0), "can't compute a_k for jacobi integration");
                }
            }

            return num / denom;
        }

        /// <summary>
        /// Returns the coefficient β_i.
        /// </summary>
        public override Real Beta(Size i)
        {
            Real num = 4.0 * i * (i + _alpha) * (i + _beta) * (i + _alpha + _beta);
            Real denom = (2.0 * i + _alpha + _beta) * (2.0 * i + _alpha + _beta)
                         * ((2.0 * i + _alpha + _beta) * (2.0 * i + _alpha + _beta) - 1);

            if (Comparison.CloseEnough(denom, 0.0))
            {
                if (!Comparison.CloseEnough(num, 0.0))
                {
                    QL.Fail("can't compute b_k for jacobi integration");
                }
                else
                {
                    // L'Hôpital's rule
                    num = 4.0 * i * (i + _beta) * (2.0 * i + 2 * _alpha + _beta);
                    denom = 2.0 * (2.0 * i + _alpha + _beta);
                    denom *= denom - 1;
                    QL.Assert(!Comparison.CloseEnough(denom, 0.0), "can't compute b_k for jacobi integration");
                }
            }

            return num / denom;
        }

        /// <summary>
        /// Returns the weight function w(x) = (1-x)^α * (1+x)^β.
        /// </summary>
        public override Real W(Real x) => Math.Pow(1 - x, _alpha) * Math.Pow(1 + x, _beta);
    }

    /// <summary>
    /// Gauss-Legendre polynomial.
    /// </summary>
    /// <remarks>
    /// The Legendre polynomials are orthogonal on [-1, 1] with weight function w(x) = 1.
    /// This is a special case of Jacobi polynomials with α = β = 0.
    /// </remarks>
    public class GaussLegendrePolynomial : GaussJacobiPolynomial
    {
        /// <summary>
        /// Initializes a new instance of the GaussLegendrePolynomial class.
        /// </summary>
        public GaussLegendrePolynomial() : base(0.0, 0.0) { }
    }

    /// <summary>
    /// Gauss-Chebyshev polynomial of the first kind.
    /// </summary>
    /// <remarks>
    /// The Chebyshev polynomials of the first kind are orthogonal on [-1, 1] 
    /// with weight function w(x) = (1-x²)^(-1/2).
    /// This is a special case of Jacobi polynomials with α = β = -0.5.
    /// </remarks>
    public class GaussChebyshevPolynomial : GaussJacobiPolynomial
    {
        /// <summary>
        /// Initializes a new instance of the GaussChebyshevPolynomial class.
        /// </summary>
        public GaussChebyshevPolynomial() : base(-0.5, -0.5) { }
    }

    /// <summary>
    /// Gauss-Chebyshev polynomial of the second kind.
    /// </summary>
    /// <remarks>
    /// The Chebyshev polynomials of the second kind are orthogonal on [-1, 1] 
    /// with weight function w(x) = (1-x²)^(1/2).
    /// This is a special case of Jacobi polynomials with α = β = 0.5.
    /// </remarks>
    public class GaussChebyshev2ndPolynomial : GaussJacobiPolynomial
    {
        /// <summary>
        /// Initializes a new instance of the GaussChebyshev2ndPolynomial class.
        /// </summary>
        public GaussChebyshev2ndPolynomial() : base(0.5, 0.5) { }
    }

    /// <summary>
    /// Gauss-Gegenbauer polynomial.
    /// </summary>
    /// <remarks>
    /// The Gegenbauer polynomials (also called ultraspherical polynomials) are orthogonal on [-1, 1] 
    /// with weight function w(x) = (1-x²)^(λ-1/2).
    /// This is a special case of Jacobi polynomials with α = β = λ - 0.5.
    /// </remarks>
    public class GaussGegenbauerPolynomial : GaussJacobiPolynomial
    {
        /// <summary>
        /// Initializes a new instance of the GaussGegenbauerPolynomial class.
        /// </summary>
        /// <param name="lambda">The parameter λ.</param>
        public GaussGegenbauerPolynomial(Real lambda) : base(lambda - 0.5, lambda - 0.5) { }
    }

    /// <summary>
    /// Gauss hyperbolic polynomial.
    /// </summary>
    /// <remarks>
    /// The hyperbolic polynomials are orthogonal on (-∞, ∞) with weight function w(x) = sech(x) = 1/cosh(x).
    /// </remarks>
    public class GaussHyperbolicPolynomial : GaussianOrthogonalPolynomial
    {
        /// <summary>
        /// Returns the zeroth moment μ_0 = π.
        /// </summary>
        public override Real Mu0 => MathConstants.PI;

        /// <summary>
        /// Returns the coefficient α_i = 0.
        /// </summary>
        public override Real Alpha(Size i) => 0.0;

        /// <summary>
        /// Returns the coefficient β_i.
        /// </summary>
        public override Real Beta(Size i)
        {
            return i != 0 ? MathConstants.PiOver2 * MathConstants.PiOver2 * i * i : MathConstants.PI;
        }

        /// <summary>
        /// Returns the weight function w(x) = sech(x) = 1/cosh(x).
        /// </summary>
        public override Real W(Real x) => 1.0 / Math.Cosh(x);
    }
}