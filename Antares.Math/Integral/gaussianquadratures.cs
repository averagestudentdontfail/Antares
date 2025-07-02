// C# code for GaussianQuadratures.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math.Matrix;

namespace Antares.Math.Integral
{
    /// <summary>
    /// Integral of a 1-dimensional function using the Gauss quadratures method.
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
    /// The correctness of the result is tested by checking it
    /// against known good values.
    /// </remarks>
    public class GaussianQuadrature
    {
        protected readonly Array _x;
        protected readonly Array _w;

        /// <summary>
        /// Initializes a new instance of the GaussianQuadrature class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        /// <param name="orthPoly">The orthogonal polynomial to use for generating points and weights.</param>
        public GaussianQuadrature(Size n, GaussianOrthogonalPolynomial orthPoly)
        {
            _x = new Array(n);
            _w = new Array(n);

            // Set up matrix to compute the roots and the weights
            var e = new Array(n - 1);

            Size i;
            for (i = 1; i < n; ++i)
            {
                _x[i] = orthPoly.Alpha(i);
                e[i - 1] = System.Math.Sqrt(orthPoly.Beta(i));
            }
            _x[0] = orthPoly.Alpha(0);

            var tqr = new TqrEigenDecomposition(
                _x, e,
                TqrEigenDecomposition.EigenVectorCalculation.OnlyFirstRowEigenVector,
                TqrEigenDecomposition.ShiftStrategy.Overrelaxation);

            _x = tqr.Eigenvalues;
            var ev = tqr.Eigenvectors;

            Real mu0 = orthPoly.Mu0;
            for (i = 0; i < n; ++i)
            {
                _w[i] = mu0 * ev[0, i] * ev[0, i] / orthPoly.W(_x[i]);
            }
        }

        /// <summary>
        /// Integrates the given function using Gaussian quadrature.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <returns>The approximate value of the integral.</returns>
        public Real Integrate(Func<Real, Real> f)
        {
            Real sum = 0.0;
            for (int i = Order - 1; i >= 0; --i)
            {
                sum += _w[i] * f(_x[i]);
            }
            return sum;
        }

        /// <summary>
        /// Gets the order (number of points) of the quadrature.
        /// </summary>
        public Size Order => (Size)_x.Count;

        /// <summary>
        /// Gets the quadrature weights.
        /// </summary>
        public Array Weights => _w;

        /// <summary>
        /// Gets the quadrature points (abscissas).
        /// </summary>
        public Array X => _x;
    }

    /// <summary>
    /// Multi-dimensional Gaussian integration.
    /// </summary>
    public class MultiDimGaussianIntegration
    {
        private readonly Array _weights;
        private readonly List<Array> _x;

        /// <summary>
        /// Initializes a new instance of the MultiDimGaussianIntegration class.
        /// </summary>
        /// <param name="ns">The number of quadrature points for each dimension.</param>
        /// <param name="genQuad">A function that generates a GaussianQuadrature for a given order.</param>
        public MultiDimGaussianIntegration(
            IList<Size> ns,
            Func<Size, GaussianQuadrature> genQuad)
        {
            int totalPoints = ns.Aggregate(1, (acc, n) => acc * (int)n);
            _weights = new Array(totalPoints, 1.0);
            _x = new List<Array>(totalPoints);

            for (int i = 0; i < totalPoints; i++)
            {
                _x.Add(new Array(ns.Count));
            }

            Size m = (Size)ns.Count;
            Size n = (Size)_x.Count;

            var spacing = new List<Size>(m);
            spacing.Add(1);
            for (int i = 1; i < m; i++)
            {
                spacing.Add(spacing[i - 1] * ns[i - 1]);
            }

            var n2weights = new Dictionary<Size, Array>();
            var n2x = new Dictionary<Size, Array>();

            foreach (var order in ns)
            {
                if (!n2x.ContainsKey(order))
                {
                    var quad = genQuad(order);
                    n2x[order] = quad.X;
                    n2weights[order] = quad.Weights;
                }
            }

            for (Size i = 0; i < n; ++i)
            {
                for (Size j = 0; j < m; ++j)
                {
                    Size order = ns[(int)j];
                    Size nx = (i / spacing[(int)j]) % ns[(int)j];
                    _weights[i] *= n2weights[order][nx];
                    _x[(int)i][j] = n2x[order][nx];
                }
            }
        }

        /// <summary>
        /// Integrates the given multi-dimensional function.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <returns>The approximate value of the integral.</returns>
        public Real Integrate(Func<Array, Real> f)
        {
            Real s = 0.0;
            Size n = (Size)_x.Count;
            for (Size i = 0; i < n; ++i)
                s += _weights[i] * f(_x[(int)i]);

            return s;
        }

        /// <summary>
        /// Gets the integration weights.
        /// </summary>
        public Array Weights => _weights;

        /// <summary>
        /// Gets the integration points.
        /// </summary>
        public IReadOnlyList<Array> X => _x;
    }

    /// <summary>
    /// Generalized Gauss-Laguerre integration.
    /// </summary>
    /// <remarks>
    /// This class performs a 1-dimensional Gauss-Laguerre integration.
    /// ∫₀^∞ f(x) dx
    /// The weighting function is w(x;s) = x^s * exp(-x) where s > -1.
    /// </remarks>
    public class GaussLaguerreIntegration : GaussianQuadrature
    {
        /// <summary>
        /// Initializes a new instance of the GaussLaguerreIntegration class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        /// <param name="s">The parameter s. Must be greater than -1.</param>
        public GaussLaguerreIntegration(Size n, Real s = 0.0)
            : base(n, new GaussLaguerrePolynomial(s)) { }
    }

    /// <summary>
    /// Generalized Gauss-Hermite integration.
    /// </summary>
    /// <remarks>
    /// This class performs a 1-dimensional Gauss-Hermite integration.
    /// ∫₋∞^∞ f(x) dx
    /// The weighting function is w(x;μ) = |x|^(2μ) * exp(-x²) where μ > -0.5.
    /// </remarks>
    public class GaussHermiteIntegration : GaussianQuadrature
    {
        /// <summary>
        /// Initializes a new instance of the GaussHermiteIntegration class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        /// <param name="mu">The parameter μ. Must be greater than -0.5.</param>
        public GaussHermiteIntegration(Size n, Real mu = 0.0)
            : base(n, new GaussHermitePolynomial(mu)) { }
    }

    /// <summary>
    /// Gauss-Jacobi integration.
    /// </summary>
    /// <remarks>
    /// This class performs a 1-dimensional Gauss-Jacobi integration.
    /// ∫₋₁¹ f(x) dx
    /// The weighting function is w(x;α,β) = (1-x)^α * (1+x)^β.
    /// </remarks>
    public class GaussJacobiIntegration : GaussianQuadrature
    {
        /// <summary>
        /// Initializes a new instance of the GaussJacobiIntegration class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        /// <param name="alpha">The parameter α.</param>
        /// <param name="beta">The parameter β.</param>
        public GaussJacobiIntegration(Size n, Real alpha, Real beta)
            : base(n, new GaussJacobiPolynomial(alpha, beta)) { }
    }

    /// <summary>
    /// Gauss-Hyperbolic integration.
    /// </summary>
    /// <remarks>
    /// This class performs a 1-dimensional Gauss-Hyperbolic integration.
    /// ∫₋∞^∞ f(x) dx
    /// The weighting function is w(x) = 1/cosh(x).
    /// </remarks>
    public class GaussHyperbolicIntegration : GaussianQuadrature
    {
        /// <summary>
        /// Initializes a new instance of the GaussHyperbolicIntegration class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        public GaussHyperbolicIntegration(Size n)
            : base(n, new GaussHyperbolicPolynomial()) { }
    }

    /// <summary>
    /// Gauss-Legendre integration.
    /// </summary>
    /// <remarks>
    /// This class performs a 1-dimensional Gauss-Legendre integration.
    /// ∫₋₁¹ f(x) dx
    /// The weighting function is w(x) = 1.
    /// </remarks>
    public class GaussLegendreIntegration : GaussianQuadrature
    {
        /// <summary>
        /// Initializes a new instance of the GaussLegendreIntegration class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        public GaussLegendreIntegration(Size n)
            : base(n, new GaussLegendrePolynomial()) { }
    }

    /// <summary>
    /// Gauss-Chebyshev integration.
    /// </summary>
    /// <remarks>
    /// This class performs a 1-dimensional Gauss-Chebyshev integration.
    /// ∫₋₁¹ f(x) dx
    /// The weighting function is w(x) = (1-x²)^(-1/2).
    /// </remarks>
    public class GaussChebyshevIntegration : GaussianQuadrature
    {
        /// <summary>
        /// Initializes a new instance of the GaussChebyshevIntegration class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        public GaussChebyshevIntegration(Size n)
            : base(n, new GaussChebyshevPolynomial()) { }
    }

    /// <summary>
    /// Gauss-Chebyshev integration (second kind).
    /// </summary>
    /// <remarks>
    /// This class performs a 1-dimensional Gauss-Chebyshev integration.
    /// ∫₋₁¹ f(x) dx
    /// The weighting function is w(x) = (1-x²)^(1/2).
    /// </remarks>
    public class GaussChebyshev2ndIntegration : GaussianQuadrature
    {
        /// <summary>
        /// Initializes a new instance of the GaussChebyshev2ndIntegration class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        public GaussChebyshev2ndIntegration(Size n)
            : base(n, new GaussChebyshev2ndPolynomial()) { }
    }

    /// <summary>
    /// Gauss-Gegenbauer integration.
    /// </summary>
    /// <remarks>
    /// This class performs a 1-dimensional Gauss-Gegenbauer integration.
    /// ∫₋₁¹ f(x) dx
    /// The weighting function is w(x) = (1-x²)^(λ-1/2).
    /// </remarks>
    public class GaussGegenbauerIntegration : GaussianQuadrature
    {
        /// <summary>
        /// Initializes a new instance of the GaussGegenbauerIntegration class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        /// <param name="lambda">The parameter λ.</param>
        public GaussGegenbauerIntegration(Size n, Real lambda)
            : base(n, new GaussGegenbauerPolynomial(lambda)) { }
    }

    /// <summary>
    /// Gaussian quadrature integrator that adapts to arbitrary intervals [a,b].
    /// </summary>
    /// <typeparam name="T">The type of Gaussian integration to use.</typeparam>
    public class GaussianQuadratureIntegrator<T> : Integrator where T : GaussianQuadrature
    {
        private readonly T _integration;

        /// <summary>
        /// Initializes a new instance of the GaussianQuadratureIntegrator class.
        /// </summary>
        /// <param name="integration">The Gaussian quadrature instance to use.</param>
        public GaussianQuadratureIntegrator(T integration)
            : base(Integration.DefaultAbsoluteAccuracy, Integration.DefaultMaxEvaluations)
        {
            _integration = integration;
        }

        /// <summary>
        /// Gets the underlying integration object.
        /// </summary>
        public T Integration => _integration;

        /// <summary>
        /// Integrates the function f from a to b by transforming to the standard interval.
        /// </summary>
        protected override double Integrate(Integrand f, double a, double b)
        {
            Real c1 = 0.5 * (b - a);
            Real c2 = 0.5 * (a + b);

            return c1 * _integration.Integrate(x => f(c1 * x + c2));
        }
    }

    /// <summary>
    /// Gauss-Legendre integrator for arbitrary intervals.
    /// </summary>
    public class GaussLegendreIntegrator : GaussianQuadratureIntegrator<GaussLegendreIntegration>
    {
        /// <summary>
        /// Initializes a new instance of the GaussLegendreIntegrator class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        public GaussLegendreIntegrator(Size n)
            : base(new GaussLegendreIntegration(n)) { }
    }

    /// <summary>
    /// Gauss-Chebyshev integrator for arbitrary intervals.
    /// </summary>
    public class GaussChebyshevIntegrator : GaussianQuadratureIntegrator<GaussChebyshevIntegration>
    {
        /// <summary>
        /// Initializes a new instance of the GaussChebyshevIntegrator class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        public GaussChebyshevIntegrator(Size n)
            : base(new GaussChebyshevIntegration(n)) { }
    }

    /// <summary>
    /// Gauss-Chebyshev (second kind) integrator for arbitrary intervals.
    /// </summary>
    public class GaussChebyshev2ndIntegrator : GaussianQuadratureIntegrator<GaussChebyshev2ndIntegration>
    {
        /// <summary>
        /// Initializes a new instance of the GaussChebyshev2ndIntegrator class.
        /// </summary>
        /// <param name="n">The number of quadrature points.</param>
        public GaussChebyshev2ndIntegrator(Size n)
            : base(new GaussChebyshev2ndIntegration(n)) { }
    }

    /// <summary>
    /// Tabulated Gauss-Legendre quadratures.
    /// </summary>
    /// <remarks>
    /// This class provides pre-computed abscissas and weights for common orders
    /// of Gauss-Legendre quadrature, taken from Abramowitz and Stegun.
    /// </remarks>
    public class TabulatedGaussLegendre
    {
        private Size _order;
        private Real[] _w;
        private Real[] _x;
        private Size _n;

        #region Tabulated values from Abramowitz and Stegun

        // Order 6
        private static readonly Real[] W6 = { 0.467913934572691, 0.360761573048139, 0.171324492379170 };
        private static readonly Real[] X6 = { 0.238619186083197, 0.661209386466265, 0.932469514203152 };
        private const Size N6 = 3;

        // Order 7
        private static readonly Real[] W7 = { 0.417959183673469, 0.381830050505119, 0.279705391489277, 0.129484966168870 };
        private static readonly Real[] X7 = { 0.000000000000000, 0.405845151377397, 0.741531185599394, 0.949107912342759 };
        private const Size N7 = 4;

        // Order 12
        private static readonly Real[] W12 = { 0.249147045813403, 0.233492536538355, 0.203167426723066, 
                                               0.160078328543346, 0.106939325995318, 0.047175336386512 };
        private static readonly Real[] X12 = { 0.125233408511469, 0.367831498998180, 0.587317954286617, 
                                               0.769902674194305, 0.904117256370475, 0.981560634246719 };
        private const Size N12 = 6;

        // Order 20
        private static readonly Real[] W20 = { 0.152753387130726, 0.149172986472604, 0.142096109318382, 0.131688638449177, 0.118194531961518,
                                               0.101930119817240, 0.083276741576704, 0.062672048334109, 0.040601429800387, 0.017614007139152 };
        private static readonly Real[] X20 = { 0.076526521133497, 0.227785851141645, 0.373706088715420, 0.510867001950827, 0.636053680726515,
                                               0.746331906460151, 0.839116971822219, 0.912234428251326, 0.963971927277914, 0.993128599185095 };
        private const Size N20 = 10;

        #endregion

        /// <summary>
        /// Initializes a new instance of the TabulatedGaussLegendre class.
        /// </summary>
        /// <param name="n">The order of the quadrature. Supported values: 6, 7, 12, 20.</param>
        public TabulatedGaussLegendre(Size n = 20)
        {
            SetOrder(n);
        }

        /// <summary>
        /// Integrates the given function using tabulated Gauss-Legendre quadrature.
        /// </summary>
        /// <param name="f">The function to integrate.</param>
        /// <returns>The approximate value of the integral over [-1, 1].</returns>
        public Real Integrate(Func<Real, Real> f)
        {
            QL.Assert(_w != null, "Null weights");
            QL.Assert(_x != null, "Null abscissas");

            Size isOrderOdd = _order & 1;
            Real val;
            Size startIdx;

            if (isOrderOdd != 0)
            {
                QL.Assert(_n > 0, "assume at least 1 point in quadrature");
                val = _w[0] * f(_x[0]);
                startIdx = 1;
            }
            else
            {
                val = 0.0;
                startIdx = 0;
            }

            for (Size i = startIdx; i < _n; ++i)
            {
                val += _w[i] * f(_x[i]);
                val += _w[i] * f(-_x[i]);
            }

            return val;
        }

        /// <summary>
        /// Sets the order of the quadrature.
        /// </summary>
        /// <param name="order">The order. Supported values: 6, 7, 12, 20.</param>
        public void SetOrder(Size order)
        {
            switch (order)
            {
                case 6:
                    _order = order; _x = X6; _w = W6; _n = N6;
                    break;
                case 7:
                    _order = order; _x = X7; _w = W7; _n = N7;
                    break;
                case 12:
                    _order = order; _x = X12; _w = W12; _n = N12;
                    break;
                case 20:
                    _order = order; _x = X20; _w = W20; _n = N20;
                    break;
                default:
                    QL.Fail($"order {order} not supported");
                    break;
            }
        }

        /// <summary>
        /// Gets the current order of the quadrature.
        /// </summary>
        public Size Order => _order;
    }
}