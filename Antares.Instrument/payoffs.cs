// Payoffs.cs

using System;
using Antares.Pattern;
using Antares.Time;

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
        public virtual void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<Payoff> visitor) visitor.Visit(this);
            else QL.Fail("not a payoff visitor");
        }
    }

    public static class Option
    {
        public enum Type { Put = -1, Call = 1 }
    }
    #endregion

    /// <summary>
    /// Dummy payoff class.
    /// </summary>
    public sealed class NullPayoff : Payoff
    {
        public override string Name => "Null";
        public override string Description => Name;
        public override Real GetValue(Real price) => QL.Fail("dummy payoff given");

        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<NullPayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }

    /// <summary>
    /// Intermediate class for put/call payoffs.
    /// </summary>
    public abstract class TypePayoff : Payoff
    {
        protected TypePayoff(Option.Type type) { OptionType = type; }
        public Option.Type OptionType { get; }
        public override string Description => $"{Name} {OptionType}";
    }

    /// <summary>
    /// Payoff based on a floating strike.
    /// </summary>
    public class FloatingTypePayoff : TypePayoff
    {
        public FloatingTypePayoff(Option.Type type) : base(type) { }
        public override string Name => "FloatingType";
        public Real GetValue(Real price, Real strike)
        {
            return OptionType switch
            {
                Option.Type.Call => Math.Max(price - strike, 0.0),
                Option.Type.Put => Math.Max(strike - price, 0.0),
                _ => QL.Fail("unknown/illegal option type")
            };
        }
        public override Real GetValue(Real price) => QL.Fail("floating payoff not handled");
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<FloatingTypePayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }

    /// <summary>
    /// Intermediate class for payoffs based on a fixed strike.
    /// </summary>
    public abstract class StrikedTypePayoff : TypePayoff, IStrikedTypePayoff
    {
        protected StrikedTypePayoff(Option.Type type, Real strike) : base(type) { Strike = strike; }
        public Real Strike { get; }
        public override string Description => $"{base.Description}, {Strike} strike";
    }

    /// <summary>
    /// Plain-vanilla payoff.
    /// </summary>
    public sealed class PlainVanillaPayoff : StrikedTypePayoff
    {
        public PlainVanillaPayoff(Option.Type type, Real strike) : base(type, strike) { }
        public override string Name => "Vanilla";
        public override Real GetValue(Real price)
        {
            return OptionType switch
            {
                Option.Type.Call => Math.Max(price - Strike, 0.0),
                Option.Type.Put => Math.Max(Strike - price, 0.0),
                _ => QL.Fail("unknown/illegal option type")
            };
        }
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<PlainVanillaPayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }

    /// <summary>
    /// Payoff with strike expressed as percentage.
    /// </summary>
    public sealed class PercentageStrikePayoff : StrikedTypePayoff
    {
        public PercentageStrikePayoff(Option.Type type, Real moneyness) : base(type, moneyness) { }
        public override string Name => "PercentageStrike";
        public override Real GetValue(Real price)
        {
            return OptionType switch
            {
                Option.Type.Call => price * Math.Max(1.0 - Strike, 0.0),
                Option.Type.Put => price * Math.Max(Strike - 1.0, 0.0),
                _ => QL.Fail("unknown/illegal option type")
            };
        }
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<PercentageStrikePayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }

    /// <summary>
    /// Binary asset-or-nothing payoff.
    /// </summary>
    public sealed class AssetOrNothingPayoff : StrikedTypePayoff
    {
        public AssetOrNothingPayoff(Option.Type type, Real strike) : base(type, strike) { }
        public override string Name => "AssetOrNothing";
        public override Real GetValue(Real price)
        {
            return OptionType switch
            {
                Option.Type.Call => (price - Strike > 0.0 ? price : 0.0),
                Option.Type.Put => (Strike - price > 0.0 ? price : 0.0),
                _ => QL.Fail("unknown/illegal option type")
            };
        }
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<AssetOrNothingPayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }

    /// <summary>
    /// Binary cash-or-nothing payoff.
    /// </summary>
    public sealed class CashOrNothingPayoff : StrikedTypePayoff
    {
        public CashOrNothingPayoff(Option.Type type, Real strike, Real cashPayoff) : base(type, strike) { CashPayoff = cashPayoff; }
        public Real CashPayoff { get; }
        public override string Name => "CashOrNothing";
        public override string Description => $"{base.Description}, {CashPayoff} cash payoff";
        public override Real GetValue(Real price)
        {
            return OptionType switch
            {
                Option.Type.Call => (price - Strike > 0.0 ? CashPayoff : 0.0),
                Option.Type.Put => (Strike - price > 0.0 ? CashPayoff : 0.0),
                _ => QL.Fail("unknown/illegal option type")
            };
        }
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<CashOrNothingPayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }

    /// <summary>
    /// Binary gap payoff.
    /// </summary>
    public sealed class GapPayoff : StrikedTypePayoff
    {
        public GapPayoff(Option.Type type, Real strike, Real secondStrike) : base(type, strike) { SecondStrike = secondStrike; }
        public Real SecondStrike { get; }
        public override string Name => "Gap";
        public override string Description => $"{base.Description}, {SecondStrike} strike payoff";
        public override Real GetValue(Real price)
        {
            return OptionType switch
            {
                Option.Type.Call => (price - Strike >= 0.0 ? price - SecondStrike : 0.0),
                Option.Type.Put => (Strike - price >= 0.0 ? SecondStrike - price : 0.0),
                _ => QL.Fail("unknown/illegal option type")
            };
        }
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<GapPayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }

    /// <summary>
    /// Binary superfund payoff.
    /// </summary>
    public sealed class SuperFundPayoff : StrikedTypePayoff
    {
        public Real SecondStrike { get; }
        public SuperFundPayoff(Real strike, Real secondStrike) : base(Option.Type.Call, strike)
        {
            QL.Require(strike > 0.0, $"strike ({strike}) must be positive");
            QL.Require(secondStrike > strike, $"second strike ({secondStrike}) must be higher than first strike ({strike})");
            SecondStrike = secondStrike;
        }
        public override string Name => "SuperFund";
        public override Real GetValue(Real price) => (price >= Strike && price < SecondStrike) ? price / Strike : 0.0;
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<SuperFundPayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }

    /// <summary>
    /// Binary supershare payoff.
    /// </summary>
    public sealed class SuperSharePayoff : StrikedTypePayoff
    {
        public Real SecondStrike { get; }
        public Real CashPayoff { get; }
        public SuperSharePayoff(Real strike, Real secondStrike, Real cashPayoff) : base(Option.Type.Call, strike)
        {
            QL.Require(secondStrike > strike, $"second strike ({secondStrike}) must be higher than first strike ({strike})");
            SecondStrike = secondStrike;
            CashPayoff = cashPayoff;
        }
        public override string Name => "SuperShare";
        public override string Description => $"{base.Description}, {SecondStrike} second strike, {CashPayoff} amount";
        public override Real GetValue(Real price) => (price >= Strike && price < SecondStrike) ? CashPayoff : 0.0;
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<SuperSharePayoff> visitor) visitor.Visit(this);
            else base.Accept(v);
        }
    }
}