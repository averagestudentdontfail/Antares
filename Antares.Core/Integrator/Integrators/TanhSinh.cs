using System;
using MathNet.Numerics.Integration;

namespace Antares.Integrator.Integrators
{
    /// <summary>
    /// Implements a high-precision adaptive integrator using the Tanh-Sinh quadrature method.
    /// In the .NET ecosystem, this is provided by MathNet.Numerics' DoubleExponentialTransformation,
    /// which is a robust method for a wide range of integrands.
    /// This is used for the "HighPrecision" scheme in the QdFpAmericanEngine.
    /// </summary>
    public class TanhSinhIntegrator : Integrator
    {
        // The Math.NET implementation internally manages evaluation counts and refinement levels.
        private const int DefaultMaxLevels = 15;

        /// <summary>
        /// Initializes a new instance of the TanhSinhIntegrator.
        /// </summary>
        /// <param name="absoluteAccuracy">The target absolute accuracy for the integration.</param>
        public TanhSinhIntegrator(double absoluteAccuracy)
            : base(absoluteAccuracy, int.MaxValue) // MaxEvaluations is controlled internally by Math.NET
        {
        }

        protected override double IntegrateImpl(Func<double, double> f, double a, double b)
        {
            // We wrap the function to count evaluations, although Math.NET doesn't expose its internal count.
            // This is primarily for maintaining the interface contract.
            Func<double, double> wrappedFunc = x =>
            {
                NumberOfEvaluations++;
                return f(x);
            };
            
            // DoubleExponentialTransformation.Integrate with correct signature
            // The MathNet.Numerics API signature is: Integrate(Func<double, double>, double, double, double)
            double result = DoubleExponentialTransformation.Integrate(wrappedFunc, a, b, AbsoluteAccuracy);
            
            // For this implementation, we estimate the error based on the target accuracy
            AbsoluteError = AbsoluteAccuracy;

            return result;
        }
    }
}