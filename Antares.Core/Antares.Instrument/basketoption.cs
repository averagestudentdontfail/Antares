// BasketOption.cs

using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using Antares.Instrument;
using Antares.Pattern;

// Type aliases for clarity
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace Antares.Instrument
{
    /// <summary>
    /// Base interface for multi-asset payoffs.
    /// </summary>
    public interface IBasketPayoff
    {
        Real GetValue(Vector underlyingValues);
    }

    /// <summary>
    /// Base class for payoffs depending on multiple assets.
    /// </summary>
    public abstract class BasketPayoff : IBasketPayoff
    {
        protected readonly IPayoff _basePayoff;

        protected BasketPayoff(IPayoff p)
        {
            _basePayoff = p ?? throw new ArgumentNullException(nameof(p));
        }

        public IPayoff BasePayoff => _basePayoff;

        /// <summary>
        /// This method accumulates the values of the underlying assets.
        /// It is called by operator() after accumulation to get the value of the payoff.
        /// </summary>
        public abstract Real Accumulate(Vector a);

        /// <summary>
        /// Calculates the payoff based on the underlying asset values.
        /// </summary>
        public virtual Real GetValue(Vector underlyingValues)
        {
            Real basketValue = Accumulate(underlyingValues);
            return _basePayoff.GetValue(basketValue);
        }

        public virtual void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<BasketPayoff> visitor)
                visitor.Visit(this);
            else if (_basePayoff != null)
                _basePayoff.Accept(v);
        }
    }

    /// <summary>
    /// Payoff on the minimum of a basket of assets.
    /// </summary>
    public class MinBasketPayoff : BasketPayoff
    {
        public MinBasketPayoff(IPayoff p) : base(p) { }
        public override Real Accumulate(Vector a) => a.Minimum();
    }

    /// <summary>
    /// Payoff on the maximum of a basket of assets.
    /// </summary>
    public class MaxBasketPayoff : BasketPayoff
    {
        public MaxBasketPayoff(IPayoff p) : base(p) { }
        public override Real Accumulate(Vector a) => a.Maximum();
    }

    /// <summary>
    /// Payoff on the weighted average of a basket of assets.
    /// </summary>
    public class AverageBasketPayoff : BasketPayoff
    {
        private readonly Vector _weights;

        public AverageBasketPayoff(IPayoff p, Vector weights) : base(p)
        {
            _weights = weights;
        }

        public AverageBasketPayoff(IPayoff p, int n) : base(p)
        {
            _weights = Vector.Build.Dense(n, 1.0 / n);
        }

        public IReadOnlyList<double> Weights => _weights.AsArray();
        public override Real Accumulate(Vector a) => _weights.DotProduct(a);
    }

    /// <summary>
    /// Payoff on the spread between two assets.
    /// </summary>
    public class SpreadBasketPayoff : BasketPayoff
    {
        public SpreadBasketPayoff(IPayoff p) : base(p) { }
        public override Real Accumulate(Vector a)
        {
            QL.Require(a.Count == 2, "payoff is only defined for two underlyings");
            return a[0] - a[1];
        }
    }

    /// <summary>
    /// Basket option on a number of assets.
    /// </summary>
    public class BasketOption : MultiAssetOption
    {
        public BasketOption(IBasketPayoff payoff, IExercise exercise)
            : base(payoff, exercise) { }

        /// <summary>
        /// Generic pricing engine for basket options.
        /// </summary>
        public abstract class Engine : GenericEngine<Arguments, Results> { }
    }
}