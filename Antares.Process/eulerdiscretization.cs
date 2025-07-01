// Eulerdiscretization.cs

using System;
using MathNet.Numerics.LinearAlgebra;

// Type aliases for clarity and consistency with QuantLib's naming
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;

namespace Antares.Process
{
    /// <summary>
    /// Euler discretization for stochastic processes.
    /// </summary>
    public class EulerDiscretization : IStochasticProcessDiscretization, IStochasticProcess1DDiscretization
    {
        #region IStochasticProcessDiscretization (N-dimensional)

        /// <summary>
        /// Returns an approximation of the drift defined as mu(t0, x0) * dt.
        /// </summary>
        public Vector Drift(StochasticProcess process, Time t0, Vector x0, Time dt)
        {
            return process.Drift(t0, x0) * dt;
        }

        /// <summary>
        /// Returns an approximation of the diffusion defined as sigma(t0, x0) * sqrt(dt).
        /// </summary>
        public Matrix Diffusion(StochasticProcess process, Time t0, Vector x0, Time dt)
        {
            return process.Diffusion(t0, x0) * Math.Sqrt(dt);
        }

        /// <summary>
        /// Returns an approximation of the covariance defined as sigma(t0, x0) * sigma(t0, x0)^T * dt.
        /// </summary>
        public Matrix Covariance(StochasticProcess process, Time t0, Vector x0, Time dt)
        {
            Matrix sigma = process.Diffusion(t0, x0);
            return sigma.Multiply(sigma.Transpose()) * dt;
        }

        #endregion

        #region IStochasticProcess1DDiscretization (1-dimensional)

        /// <summary>
        /// Returns an approximation of the drift defined as mu(t0, x0) * dt.
        /// </summary>
        public Real Drift(StochasticProcess1D process, Time t0, Real x0, Time dt)
        {
            return process.Drift(t0, x0) * dt;
        }

        /// <summary>
        /// Returns an approximation of the diffusion defined as sigma(t0, x0) * sqrt(dt).
        /// </summary>
        public Real Diffusion(StochasticProcess1D process, Time t0, Real x0, Time dt)
        {
            return process.Diffusion(t0, x0) * Math.Sqrt(dt);
        }

        /// <summary>
        /// Returns an approximation of the variance defined as sigma(t0, x0)^2 * dt.
        /// </summary>
        public Real Variance(StochasticProcess1D process, Time t0, Real x0, Time dt)
        {
            Real sigma = process.Diffusion(t0, x0);
            return sigma * sigma * dt;
        }

        #endregion
    }
}

// Supporting interfaces - these would typically be defined alongside StochasticProcess.cs
// They are included here to make the file self-contained.
namespace Antares.Process
{
    /// <summary>
    /// Interface for discretization of a multi-dimensional stochastic process.
    /// </summary>
    public interface IStochasticProcessDiscretization
    {
        Vector Drift(StochasticProcess process, Time t0, Vector x0, Time dt);
        Matrix Diffusion(StochasticProcess process, Time t0, Vector x0, Time dt);
        Matrix Covariance(StochasticProcess process, Time t0, Vector x0, Time dt);
    }

    /// <summary>
    /// Interface for discretization of a 1-dimensional stochastic process.
    /// </summary>
    public interface IStochasticProcess1DDiscretization
    {
        Real Drift(StochasticProcess1D process, Time t0, Real x0, Time dt);
        Real Diffusion(StochasticProcess1D process, Time t0, Real x0, Time dt);
        Real Variance(StochasticProcess1D process, Time t0, Real x0, Time dt);
    }
}