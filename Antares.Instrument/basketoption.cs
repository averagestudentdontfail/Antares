// C# code for BasketOption.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Pattern;
using Antares.Time;
using MathNet.Numerics.LinearAlgebra;

// Type alias for clarity.
using Vector = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace Antares.Instrument
{
    #region Supporting Infrastructure (Placeholders)
    // This infrastructure is included to make the file self-contained and compilable.
    // In a real project, these would be referenced via `using` statements.

    public abstract class Payoff
    {
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract Real GetValue(Real price);
        public virtual void Accept(IAcyclicVisitor v) { }
    }

    public abstract class MultiAssetOption : Option
    {
        protected MultiAssetOption(IPayoff payoff, IExercise exercise) : base(payoff, exercise) { }
    }
    #endregion

    /// <summary>
    /// Base interface for basket payoffs.
    /// </summary>
    public interface IBasketPayoff : IPayoff
    {
        IPayoff BasePayoff { get; }
        Real GetValue(Vector a);
        Real Accumulate(Vector a);
    }

    /// <summary>
    /// Abstract base class for basket payoffs.
    /// </summary>
    public abstract class BasketPayoff : Payoff, IBasketPayoff
    {
        protected BasketPayoff(IPayoff p) { BasePayoff = p; }

        public IPayoff BasePayoff { get; }

        public override string Name => BasePayoff.Name;
        public override string Description => BasePayoff.Description;
        public override Real GetValue(Real price) => BasePayoff.GetValue(price);

        public Real GetValue(Vector a) => BasePayoff.GetValue(Accumulate(a));
        public abstract Real Accumulate(Vector a);
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