// StochasticProcess.cs

using System;
using MathNet.Numerics.LinearAlgebra;
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

        protected readonly IDiscretization? _discretization;

        protected StochasticProcess() 
        {
            _discretization = null;
        }

        protected StochasticProcess(IDiscretization discretization)
        {
            _discretization = discretization ?? throw new ArgumentNullException(nameof(discretization));
        }

        #region Stochastic process interface
        /// <summary>
        /// Returns the number of dimensions of the stochastic process.
        /// </summary>
        public abstract Size size();

        /// <summary>
        /// Returns the number of independent factors of the process.
        /// </summary>
        public virtual Size factors() => size();

        /// <summary>
        /// Returns an array of the initial values of the state variables.
        /// </summary>
        public abstract Vector initialValues();

        /// <summary>
        /// Returns the drift part of the equation, i.e., μ(t, x_t).
        /// </summary>
        public abstract Vector drift(Time t, Vector x);

        /// <summary>
        /// Returns the diffusion part of the equation, i.e., σ(t, x_t).
        /// </summary>
        public abstract Matrix diffusion(Time t, Vector x);

        /// <summary>
        /// Returns the expectation E[x_T | x_t = x0] of the process after a time interval Δt.
        /// </summary>
        public virtual Vector expectation(Time t0, Vector x0, Time dt)
        {
            if (_discretization == null)
                throw new InvalidOperationException("Discretization not set");
            return apply(x0, _discretization.Drift(this, t0, x0, dt));
        }

        /// <summary>
        /// Returns the standard deviation S of the process after a time interval Δt.
        /// </summary>
        public virtual Matrix stdDeviation(Time t0, Vector x0, Time dt)
        {
            if (_discretization == null)
                throw new InvalidOperationException("Discretization not set");
            return _discretization.Diffusion(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the covariance Cov[x_T, x_T] of the process after a time interval Δt.
        /// </summary>
        public virtual Matrix covariance(Time t0, Vector x0, Time dt)
        {
            if (_discretization == null)
                throw new InvalidOperationException("Discretization not set");
            return _discretization.Covariance(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the asset value after a time interval Δt according to the given discretization.
        /// </summary>
        public virtual Vector evolve(Time t0, Vector x0, Time dt, Vector dw)
        {
            return apply(expectation(t0, x0, dt), stdDeviation(t0, x0, dt) * dw);
        }

        /// <summary>
        /// Applies a change to the asset value.
        /// </summary>
        public virtual Vector apply(Vector x0, Vector dx)
        {
            return x0 + dx;
        }

        /// <summary>
        /// Returns the time value corresponding to the given date in the reference system of the stochastic process.
        /// </summary>
        public virtual Time time(Date d)
        {
            throw new NotSupportedException("date/time conversion not supported");
        }
        #endregion

        #region IObserver implementation
        public virtual void update()
        {
            notifyObservers();
        }

        // For C++ compatibility
        public void Update() => update();
        #endregion

        #region IObservable implementation
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void notifyObservers() => _observable.NotifyObservers();
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
        public new interface IDiscretization
        {
            Real Drift(StochasticProcess1D process, Time t0, Real x0, Time dt);
            Real Diffusion(StochasticProcess1D process, Time t0, Real x0, Time dt);
            Real Variance(StochasticProcess1D process, Time t0, Real x0, Time dt);
        }

        private readonly new IDiscretization? _discretization;

        protected StochasticProcess1D() 
        {
            _discretization = null;
        }

        protected StochasticProcess1D(IDiscretization discretization)
        {
            _discretization = discretization ?? throw new ArgumentNullException(nameof(discretization));
        }

        #region 1-D stochastic process interface
        /// <summary>
        /// Returns the initial value of the state variable.
        /// </summary>
        public abstract Real x0();

        /// <summary>
        /// Returns the drift part of the equation, i.e., μ(t, x_t).
        /// </summary>
        public abstract Real drift(Time t, Real x);

        /// <summary>
        /// Returns the diffusion part of the equation, i.e., σ(t, x_t).
        /// </summary>
        public abstract Real diffusion(Time t, Real x);

        /// <summary>
        /// Returns the expectation E[x_T | x_t = x0] of the process after a time interval dt.
        /// </summary>
        public virtual Real expectation(Time t0, Real x0, Time dt)
        {
            if (_discretization == null)
                throw new InvalidOperationException("Discretization not set");
            return apply(x0, _discretization.Drift(this, t0, x0, dt));
        }

        /// <summary>
        /// Returns the standard deviation S of the process after a time interval dt.
        /// </summary>
        public virtual Real stdDeviation(Time t0, Real x0, Time dt)
        {
            if (_discretization == null)
                throw new InvalidOperationException("Discretization not set");
            return _discretization.Diffusion(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the variance V = S^2 of the process after a time interval dt.
        /// </summary>
        public virtual Real variance(Time t0, Real x0, Time dt)
        {
            if (_discretization == null)
                throw new InvalidOperationException("Discretization not set");
            return _discretization.Variance(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the asset value after a time interval dt.
        /// </summary>
        public virtual Real evolve(Time t0, Real x0, Time dt, Real dw)
        {
            return apply(expectation(t0, x0, dt), stdDeviation(t0, x0, dt) * dw);
        }

        /// <summary>
        /// Applies a change to the asset value.
        /// </summary>
        public virtual Real apply(Real x0, Real dx)
        {
            return x0 + dx;
        }
        #endregion

        #region StochasticProcess interface implementation (Adapter)
        public override Size size() => 1;
        public override Vector initialValues() => Vector.Build.Dense(1, x0());

        public override Vector drift(Time t, Vector x)
        {
            if (x.Count != 1) throw new ArgumentException("1-D vector required", nameof(x));
            return Vector.Build.Dense(1, drift(t, x[0]));
        }

        public override Matrix diffusion(Time t, Vector x)
        {
            if (x.Count != 1) throw new ArgumentException("1-D vector required", nameof(x));
            return Matrix.Build.Dense(1, 1, diffusion(t, x[0]));
        }

        public override Vector expectation(Time t0, Vector x0, Time dt)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            return Vector.Build.Dense(1, expectation(t0, x0[0], dt));
        }

        public override Matrix stdDeviation(Time t0, Vector x0, Time dt)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            return Matrix.Build.Dense(1, 1, stdDeviation(t0, x0[0], dt));
        }

        public override Matrix covariance(Time t0, Vector x0, Time dt)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            return Matrix.Build.Dense(1, 1, variance(t0, x0[0], dt));
        }

        public override Vector evolve(Time t0, Vector x0, Time dt, Vector dw)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            if (dw.Count != 1) throw new ArgumentException("1-D vector required", nameof(dw));
            return Vector.Build.Dense(1, evolve(t0, x0[0], dt, dw[0]));
        }

        public override Vector apply(Vector x0, Vector dx)
        {
            if (x0.Count != 1) throw new ArgumentException("1-D vector required", nameof(x0));
            if (dx.Count != 1) throw new ArgumentException("1-D vector required", nameof(dx));
            return Vector.Build.Dense(1, apply(x0[0], dx[0]));
        }
        #endregion
    }
}