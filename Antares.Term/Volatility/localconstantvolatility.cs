// Localconstantvolatility.cs

using System;
using Antares.Pattern;
using Antares.Time;
using Antares.Time.Day;

namespace Antares.Term.Volatility
{
    /// <summary>
    /// Local volatility curve derived from a Black curve.
    /// </summary>
    public class LocalVolCurve : LocalVolTermStructure
    {
        private readonly Handle<BlackVarianceCurve> _blackVarianceCurve;

        public LocalVolCurve(Handle<BlackVarianceCurve> curve)
            : base(curve.Value.BusinessDayConvention, curve.Value.DayCounter)
        {
            _blackVarianceCurve = curve;
            RegisterWith(_blackVarianceCurve);
        }

        #region TermStructure and VolatilityTermStructure interface overrides
        public override Date ReferenceDate => _blackVarianceCurve.Value.ReferenceDate;
        public override Calendar Calendar => _blackVarianceCurve.Value.Calendar;
        public override DayCounter DayCounter => _blackVarianceCurve.Value.DayCounter;
        public override Date MaxDate => _blackVarianceCurve.Value.MaxDate;

        // A curve is strike-independent, so it is defined for all strikes.
        public override Rate MinStrike => double.MinValue;
        public override Rate MaxStrike => double.MaxValue;
        #endregion

        #region LocalVolTermStructure implementation
        /// <summary>
        /// Calculates the local volatility based on the time derivative of the Black variance.
        /// </summary>
        /// <remarks>
        /// The relation
        /// `Integral(0..T) of sigma_L^2(t)dt = sigma_B^2 * T`
        /// holds, where `sigma_L(t)` is the local volatility at time `t` and
        /// `sigma_B(T)` is the Black volatility for maturity `T`. From the above, the formula
        /// `sigma_L(t) = sqrt(d/dt * (sigma_B^2(t) * t))`
        /// can be deduced, which is implemented here.
        /// </remarks>
        protected override Volatility LocalVolImpl(Time t, Real dummyStrike)
        {
            const Time dt = 1.0 / 365.0;
            // The dummyStrike is ignored as BlackVarianceCurve is strike-independent.
            Real var1 = _blackVarianceCurve.Value.BlackVariance(t, dummyStrike, true);
            Real var2 = _blackVarianceCurve.Value.BlackVariance(t + dt, dummyStrike, true);
            Real derivative = (var2 - var1) / dt;
            
            // The derivative can be negative in practice, due to interpolation artifacts.
            // We cap it at zero to avoid exceptions from Math.Sqrt.
            if (derivative < 0.0)
                return 0.0;
                
            return Math.Sqrt(derivative);
        }
        #endregion

        #region Visitability
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<LocalVolCurve> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                base.Accept(v); // Fall back to LocalVolTermStructure visitor
            }
        }
        #endregion
    }
}