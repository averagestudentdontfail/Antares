// StochasticProcess.cs

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using QLNet.Time;

// Type aliases for clarity and consistency with QuantLib's naming
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;
using Matrix = MathNet.Numerics.LinearAlgebra.Matrix<double>;

namespace Antares
{
    #region Supporting Infrastructure (Normally in separate files)
    // This infrastructure is included to make the file self-contained and compilable.
    // In a real project, these would be in their own files.

    /// <summary>
    /// Observer interface for the observer pattern.
    /// </summary>
    public interface IObserver
    {
        void Update();
    }

    /// <summary>
    /// Observable interface for the observer pattern.
    /// </summary>
    public interface IObservable
    {
        void RegisterWith(IObserver observer);
        void UnregisterWith(IObserver observer);
    }

    /// <summary>
    /// Concrete implementation of IObservable to be used via composition.
    /// </summary>
    public class Observable : IObservable
    {
        private readonly List<IObserver> _observers = new List<IObserver>();

        public void RegisterWith(IObserver observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
        }

        public void UnregisterWith(IObserver observer) => _observers.Remove(observer);

        public void NotifyObservers()
        {
            var observersCopy = new List<IObserver>(_observers);
            foreach (var observer in observersCopy)
                observer.Update();
        }
    }
    #endregion

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
        /// Returns the number of independent factors of the process.
        /// </summary>
        public virtual int Factors => Size;

        /// <summary>
        /// Returns the initial values of the state variables.
        /// </summary>
        public abstract Vector InitialValues { get; }

        /// <summary>
        /// Returns the drift part of the equation, i.e., mu(t, x_t).
        /// </summary>
        public abstract Vector Drift(Time t, Vector x);

        /// <summary>
        /// Returns the diffusion part of the equation, i.e. sigma(t, x_t).
        /// </summary>
        public abstract Matrix Diffusion(Time t, Vector x);

        /// <summary>
        /// Returns the expectation E(x_{t_0 + dt} | x_{t_0} = x_0) of the process
        /// after a time interval dt according to the given discretization.
        /// </summary>
        public virtual Vector Expectation(Time t0, Vector x0, Time dt)
        {
            return Apply(x0, _discretization.Drift(this, t0, x0, dt));
        }

        /// <summary>
        /// Returns the standard deviation S(x_{t_0 + dt} | x_{t_0} = x_0) of the process
        /// after a time interval dt according to the given discretization.
        /// </summary>
        public virtual Matrix StdDeviation(Time t0, Vector x0, Time dt)
        {
            return _discretization.Diffusion(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the covariance V(x_{t_0 + dt} | x_{t_0} = x_0) of the process
        /// after a time interval dt according to the given discretization.
        /// </summary>
        public virtual Matrix Covariance(Time t0, Vector x0, Time dt)
        {
            return _discretization.Covariance(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the asset value after a time interval dt according to the given discretization.
        /// By default, it returns E(x_0,t_0,dt) + S(x_0,t_0,dt) * dw
        /// where E is the expectation and S the standard deviation.
        /// </summary>
        public virtual Vector Evolve(Time t0, Vector x0, Time dt, Vector dw)
        {
            return Apply(Expectation(t0, x0, dt), StdDeviation(t0, x0, dt) * dw);
        }

        /// <summary>
        /// Applies a change to the asset value. By default, it returns x + dx.
        /// </summary>
        public virtual Vector Apply(Vector x0, Vector dx)
        {
            return x0 + dx;
        }
        #endregion

        #region Utilities
        /// <summary>
        /// Returns the time value corresponding to the given date
        /// in the reference system of the stochastic process.
        /// </summary>
        public virtual Time Time(Date date)
        {
            throw new NotImplementedException("Date/time conversion not supported for this process.");
        }
        #endregion

        #region IObserver and IObservable implementation
        public virtual void Update() => NotifyObservers();
        public void RegisterWith(IObserver observer) => _observable.RegisterWith(observer);
        public void UnregisterWith(IObserver observer) => _observable.UnregisterWith(observer);
        protected void NotifyObservers() => _observable.NotifyObservers();
        #endregion
    }

    /// <summary>
    /// 1-dimensional stochastic process.
    /// </summary>
    /// <remarks>
    /// This class describes a stochastic process governed by
    /// dx_t = mu(t, x_t)dt + sigma(t, x_t)dW_t.
    /// </remarks>
    public abstract class StochasticProcess1D : StochasticProcess
    {
        /// <summary>
        /// Discretization of a 1-D stochastic process.
        /// </summary>
        public new interface IDiscretization
        {
            Real Drift(StochasticProcess1D process, Time t0, Real x0, Time dt);
            Real Diffusion(StochasticProcess1D process, Time t0, Real x0, Time dt);
            Real Variance(StochasticProcess1D process, Time t0, Real x0, Time dt);
        }

        protected new readonly IDiscretization _discretization;

        protected StochasticProcess1D() { }

        protected StochasticProcess1D(IDiscretization discretization)
        {
            _discretization = discretization;
        }

        #region 1-D stochastic process interface
        /// <summary>
        /// Returns the initial value of the state variable.
        /// </summary>
        public abstract Real X0 { get; }

        /// <summary>
        /// Returns the drift part of the equation, i.e. mu(t, x).
        /// </summary>
        public abstract Real Drift(Time t, Real x);

        /// <summary>
        /// Returns the diffusion part of the equation, i.e., sigma(t, x).
        /// </summary>
        public abstract Real Diffusion(Time t, Real x);

        /// <summary>
        /// Returns the expectation E(x_{t_0 + dt} | x_{t_0} = x_0).
        /// </summary>
        public virtual Real Expectation(Time t0, Real x0, Time dt)
        {
            return Apply(x0, _discretization.Drift(this, t0, x0, dt));
        }

        /// <summary>
        /// Returns the standard deviation S(x_{t_0 + dt} | x_{t_0} = x_0).
        /// </summary>
        public virtual Real StdDeviation(Time t0, Real x0, Time dt)
        {
            return _discretization.Diffusion(this, t0, x0, dt);
        }

        /// <summary>
        /// Returns the variance V(x_{t_0 + dt} | x_{t_0} = x_0).
        /// </summary>
        public virtual Real Variance(Time t0, Real x0, Time dt)
        {
            return _discretization.Variance(this, t0, x0, dt);
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