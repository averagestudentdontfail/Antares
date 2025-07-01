using Antares.Integrator;
using Antares.Integrator.Integrators;

namespace Antares.Engine
{
    /// <summary>
    /// A scheme using Gauss-Legendre integrators of fixed order.
    /// </summary>
    public class QdFpLegendreScheme : IQdFpIterationScheme
    {
        private readonly int _m, _n;
        private readonly IIntegrator _fpIntegrator;
        private readonly IIntegrator _exerciseBoundaryIntegrator;

        public QdFpLegendreScheme(int l, int m, int n, int p)
        {
            _m = m;
            _n = n;
            _fpIntegrator = new GaussLegendreIntegrator(l);
            _exerciseBoundaryIntegrator = new GaussLegendreIntegrator(p);
        }

        public int GetNumberOfChebyshevInterpolationNodes() => _n;
        public int GetNumberOfNaiveFixedPointSteps() => _m > 0 ? _m - 1 : 0;
        public int GetNumberOfJacobiNewtonFixedPointSteps() => _m > 0 ? 1 : 0;
        public IIntegrator GetFixedPointIntegrator() => _fpIntegrator;
        public IIntegrator GetExerciseBoundaryToPriceIntegrator() => _exerciseBoundaryIntegrator;
    }

    /// <summary>
    /// A high-precision scheme using Gauss-Legendre for iterations and an adaptive
    /// Gauss-Lobatto integrator for the final price calculation.
    /// </summary>
    public class QdFpLegendreLobattoScheme : IQdFpIterationScheme
    {
        private readonly int _l;
        private readonly int _m;
        private readonly int _n;
        private readonly double _finalAccuracy;
        private readonly IIntegrator _fpIntegrator;
        private readonly IIntegrator _finalIntegrator;

        public QdFpLegendreLobattoScheme(int l, int m, int n, double finalAccuracy)
        {
            _l = l;
            _m = m;
            _n = n;
            _finalAccuracy = finalAccuracy;
            _fpIntegrator = new GaussLobattoIntegrator(l);
            _finalIntegrator = new GaussLobattoIntegrator(1000, finalAccuracy);
        }

        public int GetNumberOfChebyshevInterpolationNodes() => _n;
        public int GetNumberOfNaiveFixedPointSteps() => _m > 0 ? _m - 1 : 0;
        public int GetNumberOfJacobiNewtonFixedPointSteps() => _m > 0 ? 1 : 0;
        public IIntegrator GetFixedPointIntegrator() => _fpIntegrator;
        public IIntegrator GetExerciseBoundaryToPriceIntegrator() => _finalIntegrator;
    }

    /// <summary>
    /// A scheme designed for maximum precision, using a Tanh-Sinh (Double Exponential)
    /// integrator for both the fixed-point iterations and the final price calculation.
    /// </summary>
    public class QdFpTanhSinhScheme : IQdFpIterationScheme
    {
        private readonly int _m, _n;
        private readonly IIntegrator _integrator;

        public QdFpTanhSinhScheme(int m, int n, double accuracy)
        {
            _m = m;
            _n = n;
            _integrator = new TanhSinhIntegrator(accuracy);
        }

        public int GetNumberOfChebyshevInterpolationNodes() => _n;
        public int GetNumberOfNaiveFixedPointSteps() => _m > 0 ? _m - 1 : 0;
        public int GetNumberOfJacobiNewtonFixedPointSteps() => _m > 0 ? 1 : 0;
        public IIntegrator GetFixedPointIntegrator() => _integrator;
        public IIntegrator GetExerciseBoundaryToPriceIntegrator() => _integrator;
    }
}
