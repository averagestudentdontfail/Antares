// C# code for BlackCalculator.cs

using System;
using Antares.Instrument;
using Antares.Math;
using Antares.Math.Distribution;
using Antares.Pattern;

namespace Antares.Engine
{
    /// <summary>
    /// Black 1976 calculator class.
    /// </summary>
    /// <remarks>
    /// When the variance is null, division by zero occur during
    /// the calculation of delta, delta forward, gamma, gamma
    /// forward, rho, dividend rho, vega, and strike sensitivity.
    /// </remarks>
    public class BlackCalculator
    {
        #region Inner Calculator Class for Visitor Pattern
        
        /// <summary>
        /// Calculator visitor for handling different payoff types.
        /// </summary>
        private class Calculator : IAcyclicVisitor,
                                  IVisitor<Payoff>,
                                  IVisitor<PlainVanillaPayoff>,
                                  IVisitor<CashOrNothingPayoff>,
                                  IVisitor<AssetOrNothingPayoff>,
                                  IVisitor<GapPayoff>
        {
            private readonly BlackCalculator _black;

            public Calculator(BlackCalculator black)
            {
                _black = black;
            }

            public void Visit(Payoff payoff)
            {
                QL.Fail($"unsupported payoff type: {payoff.Name}");
            }

            public void Visit(PlainVanillaPayoff payoff)
            {
                // No additional processing needed for plain vanilla payoffs
            }

            public void Visit(CashOrNothingPayoff payoff)
            {
                _black._alpha = _black._dalphaDd1 = 0.0;
                _black._x = payoff.CashPayoff;
                _black._dxDstrike = 0.0;

                switch (payoff.OptionType)
                {
                    case Option.Type.Call:
                        _black._beta = _black._cumD2;
                        _black._dbetaDd2 = _black._nD2;
                        break;
                    case Option.Type.Put:
                        _black._beta = 1.0 - _black._cumD2;
                        _black._dbetaDd2 = -_black._nD2;
                        break;
                    default:
                        QL.Fail("invalid option type");
                        break;
                }
            }

            public void Visit(AssetOrNothingPayoff payoff)
            {
                _black._beta = _black._dbetaDd2 = 0.0;

                switch (payoff.OptionType)
                {
                    case Option.Type.Call:
                        _black._alpha = _black._cumD1;
                        _black._dalphaDd1 = _black._nD1;
                        break;
                    case Option.Type.Put:
                        _black._alpha = 1.0 - _black._cumD1;
                        _black._dalphaDd1 = -_black._nD1;
                        break;
                    default:
                        QL.Fail("invalid option type");
                        break;
                }
            }

            public void Visit(GapPayoff payoff)
            {
                _black._x = payoff.SecondStrike;
                _black._dxDstrike = 0.0;
            }
        }

        #endregion

        #region Private Fields

        private readonly double _strike;
        private readonly double _forward;
        private readonly double _stdDev;
        private readonly double _discount;
        private readonly double _variance;
        private double _d1;
        private double _d2;
        private double _alpha;
        private double _beta;
        private double _dalphaDd1;
        private double _dbetaDd2;
        private double _nD1;
        private double _cumD1;
        private double _nD2;
        private double _cumD2;
        private double _x;
        private double _dxDs;
        private double _dxDstrike;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the BlackCalculator class.
        /// </summary>
        /// <param name="payoff">The payoff of the option.</param>
        /// <param name="forward">The forward price of the underlying.</param>
        /// <param name="stdDev">The standard deviation of the underlying.</param>
        /// <param name="discount">The discount factor.</param>
        public BlackCalculator(StrikedTypePayoff payoff, double forward, double stdDev, double discount = 1.0)
        {
            _strike = payoff.Strike;
            _forward = forward;
            _stdDev = stdDev;
            _discount = discount;
            _variance = stdDev * stdDev;

            Initialize(payoff);
        }

        /// <summary>
        /// Initializes a new instance of the BlackCalculator class.
        /// </summary>
        /// <param name="optionType">The type of the option (Call or Put).</param>
        /// <param name="strike">The strike price of the option.</param>
        /// <param name="forward">The forward price of the underlying.</param>
        /// <param name="stdDev">The standard deviation of the underlying.</param>
        /// <param name="discount">The discount factor.</param>
        public BlackCalculator(Option.Type optionType, double strike, double forward, double stdDev, double discount = 1.0)
        {
            _strike = strike;
            _forward = forward;
            _stdDev = stdDev;
            _discount = discount;
            _variance = stdDev * stdDev;

            Initialize(new PlainVanillaPayoff(optionType, strike));
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the alpha coefficient.
        /// </summary>
        public double Alpha => _alpha;

        /// <summary>
        /// Gets the beta coefficient.
        /// </summary>
        public double Beta => _beta;

        /// <summary>
        /// Probability of being in the money in the bond martingale measure, i.e. N(d2).
        /// It is a risk-neutral probability, not the real world one.
        /// </summary>
        public double ItmCashProbability => _cumD2;

        /// <summary>
        /// Probability of being in the money in the asset martingale measure, i.e. N(d1).
        /// It is a risk-neutral probability, not the real world one.
        /// </summary>
        public double ItmAssetProbability => _cumD1;

        #endregion

        #region Private Methods

        /// <summary>
        /// Initializes the Black calculator with the given payoff.
        /// </summary>
        /// <param name="payoff">The payoff to initialize with.</param>
        private void Initialize(StrikedTypePayoff payoff)
        {
            QL.Require(_strike >= 0.0, $"strike ({_strike}) must be non-negative");
            QL.Require(_forward > 0.0, $"forward ({_forward}) must be positive");
            QL.Require(_stdDev >= 0.0, $"stdDev ({_stdDev}) must be non-negative");
            QL.Require(_discount > 0.0, $"discount ({_discount}) must be positive");

            if (_stdDev >= QLDefines.EPSILON)
            {
                if (Comparison.Close(_strike, 0.0))
                {
                    _d1 = QLDefines.MAX_REAL;
                    _d2 = QLDefines.MAX_REAL;
                    _cumD1 = 1.0;
                    _cumD2 = 1.0;
                    _nD1 = 0.0;
                    _nD2 = 0.0;
                }
                else
                {
                    _d1 = Math.Log(_forward / _strike) / _stdDev + 0.5 * _stdDev;
                    _d2 = _d1 - _stdDev;
                    var f = new CumulativeNormalDistribution();
                    _cumD1 = f.Value(_d1);
                    _cumD2 = f.Value(_d2);
                    _nD1 = f.Derivative(_d1);
                    _nD2 = f.Derivative(_d2);
                }
            }
            else
            {
                if (Comparison.Close(_forward, _strike))
                {
                    _d1 = 0;
                    _d2 = 0;
                    _cumD1 = 0.5;
                    _cumD2 = 0.5;
                    _nD1 = Math.Sqrt(2.0) / Math.Sqrt(Math.PI);
                    _nD2 = Math.Sqrt(2.0) / Math.Sqrt(Math.PI);
                }
                else if (_forward > _strike)
                {
                    _d1 = QLDefines.MAX_REAL;
                    _d2 = QLDefines.MAX_REAL;
                    _cumD1 = 1.0;
                    _cumD2 = 1.0;
                    _nD1 = 0.0;
                    _nD2 = 0.0;
                }
                else
                {
                    _d1 = QLDefines.MIN_REAL;
                    _d2 = QLDefines.MIN_REAL;
                    _cumD1 = 0.0;
                    _cumD2 = 0.0;
                    _nD1 = 0.0;
                    _nD2 = 0.0;
                }
            }

            _x = _strike;
            _dxDstrike = 1.0;

            // The following one will probably disappear as soon as
            // super-share will be properly handled
            _dxDs = 0.0;

            // This part is always executed.
            // In case of plain-vanilla payoffs, it is also the only part which is executed.
            switch (payoff.OptionType)
            {
                case Option.Type.Call:
                    _alpha = _cumD1;        //  N(d1)
                    _dalphaDd1 = _nD1;      //  n(d1)
                    _beta = -_cumD2;        // -N(d2)
                    _dbetaDd2 = -_nD2;      // -n(d2)
                    break;
                case Option.Type.Put:
                    _alpha = -1.0 + _cumD1; // -N(-d1)
                    _dalphaDd1 = _nD1;      //  n( d1)
                    _beta = 1.0 - _cumD2;   //  N(-d2)
                    _dbetaDd2 = -_nD2;      // -n( d2)
                    break;
                default:
                    QL.Fail("invalid option type");
                    break;
            }

            // Now dispatch on type using visitor pattern
            var calc = new Calculator(this);
            payoff.Accept(calc);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the value of the option.
        /// </summary>
        public double Value()
        {
            return _discount * (_forward * _alpha + _x * _beta);
        }

        /// <summary>
        /// Sensitivity to change in the underlying forward price.
        /// </summary>
        public double DeltaForward()
        {
            double temp = _stdDev * _forward;
            double dalphaDforward = _dalphaDd1 / temp;
            double dbetaDforward = _dbetaDd2 / temp;
            double temp2 = dalphaDforward * _forward + _alpha + dbetaDforward * _x; // DXDforward = 0.0

            return _discount * temp2;
        }

        /// <summary>
        /// Sensitivity to change in the underlying spot price.
        /// </summary>
        public virtual double Delta(double spot)
        {
            QL.Require(spot > 0.0, $"positive spot value required: {spot} not allowed");

            double dforwardDs = _forward / spot;
            double temp = _stdDev * spot;
            double dalphaDs = _dalphaDd1 / temp;
            double dbetaDs = _dbetaDd2 / temp;
            double temp2 = dalphaDs * _forward + _alpha * dforwardDs +
                          dbetaDs * _x + _beta * _dxDs;

            return _discount * temp2;
        }

        /// <summary>
        /// Sensitivity in percent to a percent change in the underlying forward price.
        /// </summary>
        public double ElasticityForward()
        {
            double val = Value();
            double del = DeltaForward();
            if (val > QLDefines.EPSILON)
                return del / val * _forward;
            else if (Math.Abs(del) < QLDefines.EPSILON)
                return 0.0;
            else if (del > 0.0)
                return QLDefines.MAX_REAL;
            else
                return QLDefines.MIN_REAL;
        }

        /// <summary>
        /// Sensitivity in percent to a percent change in the underlying spot price.
        /// </summary>
        public virtual double Elasticity(double spot)
        {
            double val = Value();
            double del = Delta(spot);
            if (val > QLDefines.EPSILON)
                return del / val * spot;
            else if (Math.Abs(del) < QLDefines.EPSILON)
                return 0.0;
            else if (del > 0.0)
                return QLDefines.MAX_REAL;
            else
                return QLDefines.MIN_REAL;
        }

        /// <summary>
        /// Second order derivative with respect to change in the underlying forward price.
        /// </summary>
        public double GammaForward()
        {
            double temp = _stdDev * _forward;
            double dalphaDforward = _dalphaDd1 / temp;
            double dbetaDforward = _dbetaDd2 / temp;

            double d2alphaDforward2 = -dalphaDforward / _forward * (1 + _d1 / _stdDev);
            double d2betaDforward2 = -dbetaDforward / _forward * (1 + _d2 / _stdDev);

            double temp2 = d2alphaDforward2 * _forward + 2.0 * dalphaDforward +
                          d2betaDforward2 * _x; // DXDforward = 0.0

            return _discount * temp2;
        }

        /// <summary>
        /// Second order derivative with respect to change in the underlying spot price.
        /// </summary>
        public virtual double Gamma(double spot)
        {
            QL.Require(spot > 0.0, $"positive spot value required: {spot} not allowed");

            double dforwardDs = _forward / spot;
            double temp = _stdDev * spot;
            double dalphaDs = _dalphaDd1 / temp;
            double dbetaDs = _dbetaDd2 / temp;

            double d2alphaDs2 = -dalphaDs / spot * (1 + _d1 / _stdDev);
            double d2betaDs2 = -dbetaDs / spot * (1 + _d2 / _stdDev);

            double temp2 = d2alphaDs2 * _forward + 2.0 * dalphaDs * dforwardDs +
                          d2betaDs2 * _x + 2.0 * dbetaDs * _dxDs;

            return _discount * temp2;
        }

        /// <summary>
        /// Sensitivity to time to maturity.
        /// </summary>
        public virtual double Theta(double spot, double maturity)
        {
            QL.Require(maturity >= 0.0, $"maturity ({maturity}) must be non-negative");
            if (Comparison.Close(maturity, 0.0)) return 0.0;
            return -(Math.Log(_discount) * Value() +
                    Math.Log(_forward / spot) * spot * Delta(spot) +
                    0.5 * _variance * spot * spot * Gamma(spot)) / maturity;
        }

        /// <summary>
        /// Sensitivity to time to maturity per day, assuming 365 day per year.
        /// </summary>
        public virtual double ThetaPerDay(double spot, double maturity)
        {
            return Theta(spot, maturity) / 365.0;
        }

        /// <summary>
        /// Sensitivity to volatility.
        /// </summary>
        public double Vega(double maturity)
        {
            QL.Require(maturity >= 0.0, "negative maturity not allowed");

            double temp = Math.Log(_strike / _forward) / _variance;
            // actually DalphaDsigma / SQRT(T)
            double dalphaDsigma = _dalphaDd1 * (temp + 0.5);
            double dbetaDsigma = _dbetaDd2 * (temp - 0.5);

            double temp2 = dalphaDsigma * _forward + dbetaDsigma * _x;

            return _discount * Math.Sqrt(maturity) * temp2;
        }

        /// <summary>
        /// Sensitivity to discounting rate.
        /// </summary>
        public double Rho(double maturity)
        {
            QL.Require(maturity >= 0.0, "negative maturity not allowed");

            // actually DalphaDr / T
            double dalphaDr = _dalphaDd1 / _stdDev;
            double dbetaDr = _dbetaDd2 / _stdDev;
            double temp = dalphaDr * _forward + _alpha * _forward + dbetaDr * _x;

            return maturity * (_discount * temp - Value());
        }

        /// <summary>
        /// Sensitivity to dividend/growth rate.
        /// </summary>
        public double DividendRho(double maturity)
        {
            QL.Require(maturity >= 0.0, "negative maturity not allowed");

            // actually DalphaDq / T
            double dalphaDq = -_dalphaDd1 / _stdDev;
            double dbetaDq = -_dbetaDd2 / _stdDev;

            double temp = dalphaDq * _forward - _alpha * _forward + dbetaDq * _x;

            return maturity * _discount * temp;
        }

        /// <summary>
        /// Sensitivity to strike.
        /// </summary>
        public double StrikeSensitivity()
        {
            double temp = _stdDev * _strike;
            double dalphaDstrike = -_dalphaDd1 / temp;
            double dbetaDstrike = -_dbetaDd2 / temp;

            double temp2 = dalphaDstrike * _forward + dbetaDstrike * _x + _beta * _dxDstrike;

            return _discount * temp2;
        }

        /// <summary>
        /// Gamma with respect to strike.
        /// </summary>
        public double StrikeGamma()
        {
            double temp = _stdDev * _strike;
            double dalphaDstrike = -_dalphaDd1 / temp;
            double dbetaDstrike = -_dbetaDd2 / temp;

            double d2alphaD2strike = -dalphaDstrike / _strike * (1 - _d1 / _stdDev);
            double d2betaD2strike = -dbetaDstrike / _strike * (1 - _d2 / _stdDev);

            double temp2 = d2alphaD2strike * _forward + d2betaD2strike * _x +
                          2.0 * dbetaDstrike * _dxDstrike;

            return _discount * temp2;
        }

        #endregion
    }
}