using Anderson.Integrator;

namespace Anderson.Engine
{
    /// <summary>
    /// Defines the strategy for the fixed-point iteration process, including
    /// the number of steps, interpolation nodes, and integration methods.
    /// </summary>
    public interface IQdFpIterationScheme
    {
        int GetNumberOfChebyshevInterpolationNodes();
        int GetNumberOfNaiveFixedPointSteps();
        int GetNumberOfJacobiNewtonFixedPointSteps();
        IIntegrator GetFixedPointIntegrator();
        IIntegrator GetExerciseBoundaryToPriceIntegrator();
    }
}