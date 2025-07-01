using Antares.Integrator;

namespace Antares.Engine
{
    public interface IQdFpIterationScheme
    {
        int GetNumberOfChebyshevInterpolationNodes();
        int GetNumberOfNaiveFixedPointSteps();
        int GetNumberOfJacobiNewtonFixedPointSteps();
        IIntegrator GetFixedPointIntegrator();
        IIntegrator GetExerciseBoundaryToPriceIntegrator();
    }
}
