// C# code for FdBlackScholesVanillaEngine.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Cashflow;
using Antares.Engine;
using Antares.Instrument;
using Antares.Method.Mesh;
using Antares.Method.Solver;
using Antares.Method.Step;
using Antares.Method.Utility;
using Antares.Pattern;
using Antares.Process;
using Antares.Time;
using Antares.Term.Yield;

namespace Antares.Engine
{
    /// <summary>
    /// Finite-differences Black Scholes vanilla option engine.
    /// </summary>
    /// <remarks>
    /// This engine uses finite difference methods to price vanilla options
    /// under the Black-Scholes framework. It supports various features including
    /// local volatility models, dividend handling, and quanto options.
    /// </remarks>
    public class FdBlackScholesVanillaEngine : GenericEngine<VanillaOption.Arguments, VanillaOption.Results>
    {
        #region Enums
        
        /// <summary>
        /// Cash dividend model enumeration.
        /// </summary>
        public enum CashDividendModel
        {
            /// <summary>
            /// Spot dividend model - dividends are subtracted from spot price.
            /// </summary>
            Spot,
            
            /// <summary>
            /// Escrowed dividend model - dividends are held in escrow and accrue interest.
            /// </summary>
            Escrowed
        }
        
        #endregion

        #region Private Fields
        
        private readonly GeneralizedBlackScholesProcess _process;
        private readonly IReadOnlyList<IDividend> _dividends;
        private readonly int _tGrid;
        private readonly int _xGrid;
        private readonly int _dampingSteps;
        private readonly FdmSchemeDesc _schemeDesc;
        private readonly bool _localVol;
        private readonly double _illegalLocalVolOverwrite;
        private readonly FdmQuantoHelper _quantoHelper;
        private readonly CashDividendModel _cashDividendModel;
        
        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the FdBlackScholesVanillaEngine class.
        /// </summary>
        /// <param name="process">The generalized Black-Scholes process.</param>
        /// <param name="tGrid">Number of time grid points (default 100).</param>
        /// <param name="xGrid">Number of space grid points (default 100).</param>
        /// <param name="dampingSteps">Number of damping steps (default 0).</param>
        /// <param name="schemeDesc">The finite difference scheme description (default Douglas).</param>
        /// <param name="localVol">Whether to use local volatility (default false).</param>
        /// <param name="illegalLocalVolOverwrite">Fallback volatility for numerical issues (default NaN).</param>
        /// <param name="cashDividendModel">The cash dividend model (default Spot).</param>
        public FdBlackScholesVanillaEngine(
            GeneralizedBlackScholesProcess process,
            int tGrid = 100,
            int xGrid = 100,
            int dampingSteps = 0,
            FdmSchemeDesc schemeDesc = null,
            bool localVol = false,
            double illegalLocalVolOverwrite = double.NaN,
            CashDividendModel cashDividendModel = CashDividendModel.Spot)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _dividends = new List<IDividend>();
            _tGrid = tGrid;
            _xGrid = xGrid;
            _dampingSteps = dampingSteps;
            _schemeDesc = schemeDesc ?? FdmSchemeDesc.Douglas();
            _localVol = localVol;
            _illegalLocalVolOverwrite = double.IsNaN(illegalLocalVolOverwrite) ? -double.MaxValue : illegalLocalVolOverwrite;
            _quantoHelper = null;
            _cashDividendModel = cashDividendModel;
            
            RegisterWith(_process);
        }

        /// <summary>
        /// Initializes a new instance of the FdBlackScholesVanillaEngine class with dividends.
        /// </summary>
        /// <param name="process">The generalized Black-Scholes process.</param>
        /// <param name="dividends">The dividend schedule.</param>
        /// <param name="tGrid">Number of time grid points (default 100).</param>
        /// <param name="xGrid">Number of space grid points (default 100).</param>
        /// <param name="dampingSteps">Number of damping steps (default 0).</param>
        /// <param name="schemeDesc">The finite difference scheme description (default Douglas).</param>
        /// <param name="localVol">Whether to use local volatility (default false).</param>
        /// <param name="illegalLocalVolOverwrite">Fallback volatility for numerical issues (default NaN).</param>
        /// <param name="cashDividendModel">The cash dividend model (default Spot).</param>
        public FdBlackScholesVanillaEngine(
            GeneralizedBlackScholesProcess process,
            IReadOnlyList<IDividend> dividends,
            int tGrid = 100,
            int xGrid = 100,
            int dampingSteps = 0,
            FdmSchemeDesc schemeDesc = null,
            bool localVol = false,
            double illegalLocalVolOverwrite = double.NaN,
            CashDividendModel cashDividendModel = CashDividendModel.Spot)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _dividends = dividends ?? new List<IDividend>();
            _tGrid = tGrid;
            _xGrid = xGrid;
            _dampingSteps = dampingSteps;
            _schemeDesc = schemeDesc ?? FdmSchemeDesc.Douglas();
            _localVol = localVol;
            _illegalLocalVolOverwrite = double.IsNaN(illegalLocalVolOverwrite) ? -double.MaxValue : illegalLocalVolOverwrite;
            _quantoHelper = null;
            _cashDividendModel = cashDividendModel;
            
            RegisterWith(_process);
        }

        /// <summary>
        /// Initializes a new instance of the FdBlackScholesVanillaEngine class with quanto helper.
        /// </summary>
        /// <param name="process">The generalized Black-Scholes process.</param>
        /// <param name="quantoHelper">The quanto helper for multi-currency adjustments.</param>
        /// <param name="tGrid">Number of time grid points (default 100).</param>
        /// <param name="xGrid">Number of space grid points (default 100).</param>
        /// <param name="dampingSteps">Number of damping steps (default 0).</param>
        /// <param name="schemeDesc">The finite difference scheme description (default Douglas).</param>
        /// <param name="localVol">Whether to use local volatility (default false).</param>
        /// <param name="illegalLocalVolOverwrite">Fallback volatility for numerical issues (default NaN).</param>
        /// <param name="cashDividendModel">The cash dividend model (default Spot).</param>
        public FdBlackScholesVanillaEngine(
            GeneralizedBlackScholesProcess process,
            FdmQuantoHelper quantoHelper,
            int tGrid = 100,
            int xGrid = 100,
            int dampingSteps = 0,
            FdmSchemeDesc schemeDesc = null,
            bool localVol = false,
            double illegalLocalVolOverwrite = double.NaN,
            CashDividendModel cashDividendModel = CashDividendModel.Spot)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _dividends = new List<IDividend>();
            _tGrid = tGrid;
            _xGrid = xGrid;
            _dampingSteps = dampingSteps;
            _schemeDesc = schemeDesc ?? FdmSchemeDesc.Douglas();
            _localVol = localVol;
            _illegalLocalVolOverwrite = double.IsNaN(illegalLocalVolOverwrite) ? -double.MaxValue : illegalLocalVolOverwrite;
            _quantoHelper = quantoHelper;
            _cashDividendModel = cashDividendModel;
            
            RegisterWith(_process);
            if (_quantoHelper != null)
                RegisterWith(_quantoHelper);
        }

        /// <summary>
        /// Initializes a new instance of the FdBlackScholesVanillaEngine class with dividends and quanto helper.
        /// </summary>
        /// <param name="process">The generalized Black-Scholes process.</param>
        /// <param name="dividends">The dividend schedule.</param>
        /// <param name="quantoHelper">The quanto helper for multi-currency adjustments.</param>
        /// <param name="tGrid">Number of time grid points (default 100).</param>
        /// <param name="xGrid">Number of space grid points (default 100).</param>
        /// <param name="dampingSteps">Number of damping steps (default 0).</param>
        /// <param name="schemeDesc">The finite difference scheme description (default Douglas).</param>
        /// <param name="localVol">Whether to use local volatility (default false).</param>
        /// <param name="illegalLocalVolOverwrite">Fallback volatility for numerical issues (default NaN).</param>
        /// <param name="cashDividendModel">The cash dividend model (default Spot).</param>
        public FdBlackScholesVanillaEngine(
            GeneralizedBlackScholesProcess process,
            IReadOnlyList<IDividend> dividends,
            FdmQuantoHelper quantoHelper,
            int tGrid = 100,
            int xGrid = 100,
            int dampingSteps = 0,
            FdmSchemeDesc schemeDesc = null,
            bool localVol = false,
            double illegalLocalVolOverwrite = double.NaN,
            CashDividendModel cashDividendModel = CashDividendModel.Spot)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _dividends = dividends ?? new List<IDividend>();
            _tGrid = tGrid;
            _xGrid = xGrid;
            _dampingSteps = dampingSteps;
            _schemeDesc = schemeDesc ?? FdmSchemeDesc.Douglas();
            _localVol = localVol;
            _illegalLocalVolOverwrite = double.IsNaN(illegalLocalVolOverwrite) ? -double.MaxValue : illegalLocalVolOverwrite;
            _quantoHelper = quantoHelper;
            _cashDividendModel = cashDividendModel;
            
            RegisterWith(_process);
            if (_quantoHelper != null)
                RegisterWith(_quantoHelper);
        }

        #endregion

        #region Calculation

        /// <summary>
        /// Performs the finite difference calculation for the vanilla option.
        /// </summary>
        public override void Calculate()
        {
            // 0. Cash dividend model
            var exerciseDate = _arguments.Exercise.LastDate;
            var maturity = _process.Time(exerciseDate);
            var settlementDate = _process.RiskFreeRate.ReferenceDate;

            double spotAdjustment = 0.0;
            var dividendSchedule = new List<IDividend>();

            EscrowedDividendAdjustment escrowedDivAdj = null;

            switch (_cashDividendModel)
            {
                case CashDividendModel.Spot:
                    dividendSchedule = _dividends.ToList();
                    break;

                case CashDividendModel.Escrowed:
                    if (_arguments.Exercise.Type != Exercise.ExerciseType.European)
                    {
                        // Add dividend dates as stopping times for American/Bermudan options
                        foreach (var div in _dividends)
                        {
                            dividendSchedule.Add(new FixedDividend(0.0, div.Date));
                        }
                    }

                    QL.Require(_quantoHelper == null,
                        "Escrowed dividend model is not supported for Quanto-Options");

                    escrowedDivAdj = new EscrowedDividendAdjustment(
                        new DividendSchedule(_dividends),
                        new Handle<IYieldTermStructure>(_process.RiskFreeRate),
                        new Handle<IYieldTermStructure>(_process.DividendYield),
                        date => _process.Time(date),
                        maturity);

                    spotAdjustment = escrowedDivAdj.DividendAdjustment(_process.Time(settlementDate));

                    QL.Require(_process.X0.Value + spotAdjustment > 0.0,
                        "spot minus dividends becomes negative");

                    break;

                default:
                    QL.Fail("unknown cash dividend model");
                    break;
            }

            // 1. Mesher
            var payoff = _arguments.Payoff as IStrikedTypePayoff;
            QL.Require(payoff != null, "non-striked payoff given");

            var equityMesher = new FdmBlackScholesMesher(
                _xGrid, 
                _process, 
                maturity, 
                payoff.Strike,
                xMinConstraint: null,
                xMaxConstraint: null,
                eps: 0.0001,
                scaleFactor: 1.5,
                cPointInfo: (payoff.Strike, 0.1),
                dividendSchedule: dividendSchedule,
                fdmQuantoHelper: _quantoHelper,
                spotAdjustment: spotAdjustment);

            var mesher = new FdmMesherComposite(equityMesher);

            // 2. Calculator
            FdmInnerValueCalculator calculator;
            switch (_cashDividendModel)
            {
                case CashDividendModel.Spot:
                    calculator = new FdmLogInnerValue(payoff, mesher, 0);
                    break;

                case CashDividendModel.Escrowed:
                    calculator = new FdmEscrowedLogInnerValueCalculator(
                        escrowedDivAdj, payoff, mesher, 0);
                    break;

                default:
                    QL.Fail("unknown cash dividend model");
                    calculator = null; // Never reached
                    break;
            }

            // 3. Step conditions
            var conditions = FdmStepConditionComposite.VanillaComposite(
                dividendSchedule, 
                _arguments.Exercise, 
                mesher, 
                calculator,
                _process.RiskFreeRate.ReferenceDate,
                _process.RiskFreeRate.DayCounter);

            // 4. Boundary conditions
            var boundaries = new FdmBoundaryConditionSet();

            // 5. Solver
            var solverDesc = new FdmSolverDesc(
                mesher, 
                boundaries, 
                conditions, 
                calculator,
                maturity, 
                _tGrid, 
                _dampingSteps);

            var solver = new FdmBlackScholesSolver(
                new Handle<GeneralizedBlackScholesProcess>(_process),
                payoff.Strike, 
                solverDesc, 
                _schemeDesc,
                _localVol, 
                _illegalLocalVolOverwrite < 0 ? (double?)null : _illegalLocalVolOverwrite,
                _quantoHelper != null ? new Handle<FdmQuantoHelper>(_quantoHelper) : new Handle<FdmQuantoHelper>());

            var spot = _process.X0.Value + spotAdjustment;

            _results.Value = solver.ValueAt(spot);
            _results.Delta = solver.DeltaAt(spot);
            _results.Gamma = solver.GammaAt(spot);
            _results.Theta = solver.ThetaAt(spot);
        }

        #endregion
    }

    /// <summary>
    /// Builder pattern class for constructing FdBlackScholesVanillaEngine instances.
    /// </summary>
    public class MakeFdBlackScholesVanillaEngine
    {
        #region Private Fields
        
        private readonly GeneralizedBlackScholesProcess _process;
        private IReadOnlyList<IDividend> _dividends = new List<IDividend>();
        private int _tGrid = 100;
        private int _xGrid = 100;
        private int _dampingSteps = 0;
        private FdmSchemeDesc _schemeDesc;
        private bool _localVol = false;
        private double _illegalLocalVolOverwrite;
        private FdmQuantoHelper _quantoHelper;
        private FdBlackScholesVanillaEngine.CashDividendModel _cashDividendModel = FdBlackScholesVanillaEngine.CashDividendModel.Spot;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MakeFdBlackScholesVanillaEngine class.
        /// </summary>
        /// <param name="process">The generalized Black-Scholes process.</param>
        public MakeFdBlackScholesVanillaEngine(GeneralizedBlackScholesProcess process)
        {
            _process = process ?? throw new ArgumentNullException(nameof(process));
            _schemeDesc = FdmSchemeDesc.Douglas();
            _illegalLocalVolOverwrite = -double.MaxValue;
        }

        #endregion

        #region Fluent Interface Methods

        /// <summary>
        /// Sets the quanto helper for multi-currency options.
        /// </summary>
        /// <param name="quantoHelper">The quanto helper.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithQuantoHelper(FdmQuantoHelper quantoHelper)
        {
            _quantoHelper = quantoHelper;
            return this;
        }

        /// <summary>
        /// Sets the number of time grid points.
        /// </summary>
        /// <param name="tGrid">The number of time grid points.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithTGrid(int tGrid)
        {
            _tGrid = tGrid;
            return this;
        }

        /// <summary>
        /// Sets the number of space grid points.
        /// </summary>
        /// <param name="xGrid">The number of space grid points.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithXGrid(int xGrid)
        {
            _xGrid = xGrid;
            return this;
        }

        /// <summary>
        /// Sets the number of damping steps.
        /// </summary>
        /// <param name="dampingSteps">The number of damping steps.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithDampingSteps(int dampingSteps)
        {
            _dampingSteps = dampingSteps;
            return this;
        }

        /// <summary>
        /// Sets the finite difference scheme description.
        /// </summary>
        /// <param name="schemeDesc">The scheme description.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithFdmSchemeDesc(FdmSchemeDesc schemeDesc)
        {
            _schemeDesc = schemeDesc ?? throw new ArgumentNullException(nameof(schemeDesc));
            return this;
        }

        /// <summary>
        /// Sets whether to use local volatility.
        /// </summary>
        /// <param name="localVol">True to use local volatility, false otherwise.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithLocalVol(bool localVol)
        {
            _localVol = localVol;
            return this;
        }

        /// <summary>
        /// Sets the fallback volatility for numerical issues.
        /// </summary>
        /// <param name="illegalLocalVolOverwrite">The fallback volatility value.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithIllegalLocalVolOverwrite(double illegalLocalVolOverwrite)
        {
            _illegalLocalVolOverwrite = illegalLocalVolOverwrite;
            return this;
        }

        /// <summary>
        /// Sets the cash dividends using date and amount vectors.
        /// </summary>
        /// <param name="dividendDates">The dividend dates.</param>
        /// <param name="dividendAmounts">The dividend amounts.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithCashDividends(
            IReadOnlyList<Date> dividendDates,
            IReadOnlyList<Real> dividendAmounts)
        {
            _dividends = DividendFactory.DividendVector(dividendDates, dividendAmounts);
            return this;
        }

        /// <summary>
        /// Sets the cash dividend model.
        /// </summary>
        /// <param name="cashDividendModel">The cash dividend model.</param>
        /// <returns>This builder instance for method chaining.</returns>
        public MakeFdBlackScholesVanillaEngine WithCashDividendModel(
            FdBlackScholesVanillaEngine.CashDividendModel cashDividendModel)
        {
            _cashDividendModel = cashDividendModel;
            return this;
        }

        #endregion

        #region Conversion Operator

        /// <summary>
        /// Implicitly converts this builder to a pricing engine.
        /// </summary>
        /// <param name="builder">The builder instance.</param>
        /// <returns>A configured FdBlackScholesVanillaEngine instance.</returns>
        public static implicit operator IPricingEngine(MakeFdBlackScholesVanillaEngine builder)
        {
            if (builder._quantoHelper != null)
            {
                return new FdBlackScholesVanillaEngine(
                    builder._process,
                    builder._dividends,
                    builder._quantoHelper,
                    builder._tGrid,
                    builder._xGrid,
                    builder._dampingSteps,
                    builder._schemeDesc,
                    builder._localVol,
                    builder._illegalLocalVolOverwrite,
                    builder._cashDividendModel);
            }
            else
            {
                return new FdBlackScholesVanillaEngine(
                    builder._process,
                    builder._dividends,
                    builder._tGrid,
                    builder._xGrid,
                    builder._dampingSteps,
                    builder._schemeDesc,
                    builder._localVol,
                    builder._illegalLocalVolOverwrite,
                    builder._cashDividendModel);
            }
        }

        #endregion
    }
}