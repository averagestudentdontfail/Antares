// StochasticProcess.cs

using System;
using MathNet.Numerics.LinearAlgebra;
using QLNet.Time;
using Antares.Pattern;

// Type aliases for clarity and consistency with QuantLib's naming
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;

namespace Antares
{
    /// <summary>
    /// Multi-dimensional stochastic process class.
    /// </summary>
    /// <remarks>
    /// This class describes a stochastic process governed by
    /// d(x_t) = mu(t, x_t)dt + sigma(t, x_t) d(W_t).
    /// </remarks>
    public abstract class StochasticProcess : IObserver, IObservable
    {
        private readonly Observable _observable = new Observable();

        /// <summary>
        /// Discretization of a stochastic process over a given time interval.
        /// </summary>
        public interface IDiscretization
        {
            Vector Drift(StochasticProcess process, Time t0, Vector x0, Time dt);
            Matrix Diffusion(StochasticProcess process, Time t0, Vector x0, Time dt);
            Matrix Covariance(StochasticProcess process, Time t0, Vector x0, Time dt);
        }

        protected readonly IDiscretization _discretization;

        protected StochasticProcess() { }

        protected StochasticProcess(IDiscretization discretization)
        {
            _discretization = discretization;
        }

        #region Stochastic process interface
        /// <summary>
        /// Returns the number of dimensions of the stochastic process.
        /// </summary>
        public abstract int Size { get; }

        /// <summary>
        /// Returns an array of the initial values of the state variables.
        /// </summary>
        public abstract Vector InitialValues { get; }

        /// <summary>
        /// Returns the drift part of the equation, i.e., μ(t, x_t).
        /// </summary>
        public abstract Vector Drift(Time t, Vector x);

        /// <summary>
        /// Returns the diffusion part of the equation, i.e., σ(t, x_t).
        /// </summary>
        public abstract Matrix Diffusion(Time t, Vector x);

        /// <summary>
        /// Returns the expectation E[x_T | x_t = x0] of the process after a time interval Δt.
        /// </summary>
        public abstract Vector Expectation(Time t0, Vector x0, Time dt);

        /// <summary>
        /// Returns the standard deviation S of the process after a time interval Δt.
        /// </summary>
        public abstract Matrix StdDeviation(Time t0, Vector x0, Time dt);

        /// <summary>
        /// Returns the covariance Cov[x_T, x_T] of the process after a time interval Δt.
        /// </summary>
        public abstract Matrix Covariance(Time t0, Vector x0, Time dt);

        /// <summary>
        /// Returns the asset value after a time interval Δt according to the given discretization.
        /// </summary>
        public abstract Vector Evolve(Time t0, Vector x0, Time dt, Vector dw);

        /// <summary>
        /// Applies a change to the asset value.
        /// </summary>
        public abstract Vector Apply(Vector x0, Vector dx);

        /// <summary>
        /// Returns the current time.
        /// </summary>
        public virtual Time Time => 0.0;
        #endregion

        #region IObserver implementation
        public virtual void Update()
        {
            NotifyObservers();
        }
        #endregion

        #region IObservable implementation
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
        #endregion
    }

    /// <summary>
    /// One-dimensional stochastic process.
    /// </summary>
    public abstract class StochasticProcess1D : StochasticProcess
    {
        /// <summary>
        /// Discretization interface for 1-D stochastic processes.
        /// </summary>
        public interface IStochasticProcess1DDiscretization
        {
            Real Drift(StochasticProcess1D process, Time t0, Real x0, Time dt);
            Real Diffusion(StochasticProcess1D process, Time t0, Real x0, Time dt);
            Real Variance(StochasticProcess1D process, Time t0, Real x0, Time dt);
        }

        private readonly IStochasticProcess1DDiscretization _discretization1D;

        protected StochasticProcess1D() { }

        protected StochasticProcess1D(IStochasticProcess1DDiscretization discretization)
        {
            _discretization1D = discretization;
        }

        #region 1-D stochastic process interface
        /// <summary>
        /// Returns the initial value of the state variable.
        /// </summary>
        public abstract Real X0 { get; }

        /// <summary>
        /// Returns the drift part of the equation, i.e., μ(t, x_t).
        /// </summary>
        public abstract Real Drift(Time t, Real x);

        /// <summary>
        /// Returns the diffusion part of the equation, i.e., σ(t, x_t).
        /// </summary>
        public abstract Real Diffusion(Time t, Real x);

        /// <summary>
        /// Returns the expectation E[x_T | x_t = x0] of the process after a time interval dt.
        /// </summary>
        public virtual Real Expectation(Time t0, Real x0, Time dt)
        {
            return _discretization1D.Drift(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the standard deviation S of the process after a time interval dt.
        /// </summary>
        public virtual Real StdDeviation(Time t0, Real x0, Time dt)
        {
            return Math.Sqrt(Variance(t0, x0, dt));
        }

        /// <summary>
        /// Returns the variance V = S^2 of the process after a time interval dt.
        /// </summary>
        public virtual Real Variance(Time t0, Real x0, Time dt)
        {
            return _discretization1D.Variance(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the asset value after a time interval dt.
        /// </summary>
        public virtual Real Evolve(Time t0, Real x0, Time dt, Real dw)
        {
            return Apply(Expectation(t0, x0, dt), StdDeviation(t0, x0, dt) * dw);
        }

        /// <summary>
        /// Applies a change to the asset value.
        /// </summary>
        public virtual Real Apply(Real x0, Real dx)
        {
            return x0 + dx;
        }
        #endregion

        #region StochasticProcess interface implementation (Adapter)
        public override int Size => 1;
        public override Vector InitialValues => Vector.Build.Dense(1, X0);

        public override Vector Drift(Time t, Vector x)
        {
            if (x.Count != 1) throw new ArgumentException("1-D vector required", nameof(x));
            return Vector.Build.Dense(1, Drift(t, x[0]));
        }

        public override Matrix Diffusion(Time t, Vector x)
        {
            if (x.Count != 1) throw new ArgumentException("1-D vector required", nameof(x));
            return Matrix.Build.Dense(1, 1, Diffusion(t, x[0]));
        }

        public override Vector Expectation(Time t0, Vector x0, Time dt)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            return Vector.Build.Dense(1, Expectation(t0, x0[0], dt));
        }

        public override Matrix StdDeviation(Time t0, Vector x0, Time dt)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            return Matrix.Build.Dense(1, 1, StdDeviation(t0, x0[0], dt));
        }

        public override Matrix Covariance(Time t0, Vector x0, Time dt)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            return Matrix.Build.Dense(1, 1, Variance(t0, x0[0], dt));
        }

        public override Vector Evolve(Time t0, Vector x0, Time dt, Vector dw)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            if (dw.Count != 1) throw new ArgumentException("1-D vector required", nameof(dw));
            return Vector.Build.Dense(1, Evolve(t0, x0[0], dt, dw[0]));
        }

        public override Vector Apply(Vector x0, Vector dx)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            if (dx.Count != 1) throw new ArgumentException("1-D vector required", nameof(dx));
            return Vector.Build.Dense(1, Apply(x0[0], dx[0]));
        }
        #endregion
    }
}