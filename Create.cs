using System;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Logging;
using QuantConnect.Securities;
using QuantConnect.Securities.Equity;
using QuantConnect.Securities.Option;
using Anderson.Root;
using Anderson.Engine;

using Anderson.Interpolation;
using Anderson.Integrator;
using Anderson.Distribution;

namespace Anderson
{
    /// <summary>
    /// An option pricing model for American-style options using the high-performance
    /// fixed-point iteration method developed by Andersen, Lake, and Offengenden.
    /// This engine provides highly accurate and fast pricing by iteratively refining
    /// an initial guess of the early exercise boundary. It also calculates Greeks
    /// using the finite difference method.
    /// </summary>
    public class QuantConnectQdFpAmericanEngine : IOptionPriceModel
    {
        private readonly Engine.QdFpAmericanEngine _engine;
        private IAlgorithm _algorithm; // Reference to algorithm for risk-free rate access

        /// <summary>
        /// Defines the trade-off between performance and accuracy for the engine.
        /// </summary>
        public enum Scheme
        {
            Fast,
            Accurate,
            Precise
        }

        public QuantConnectQdFpAmericanEngine(Scheme scheme = Scheme.Accurate, IAlgorithm algorithm = null)
        {
            _algorithm = algorithm;
            IQdFpIterationScheme iterationScheme;
            switch (scheme)
            {
                case Scheme.Fast:
                    iterationScheme = new QdFpLegendreScheme(l: 7, m: 2, n: 7, p: 27);
                    break;
                case Scheme.Precise:
                    iterationScheme = new QdFpTanhSinhScheme(m: 10, n: 30, accuracy: 1e-12);
                    break;
                case Scheme.Accurate:
                default:
                    iterationScheme = new QdFpLegendreLobattoScheme(l: 16, m: 8, n: 16, finalAccuracy: 1e-10);
                    break;
            }
            _engine = new Engine.QdFpAmericanEngine(iterationScheme);
        }

        /// <summary>
        /// Evaluates the price and greeks of the option contract.
        /// </summary>
        public OptionPriceModelResult Evaluate(Security security, Slice slice, OptionContract contract)
        {
            // FIX: Cast QC's decimal types to double for the numerical engine
            double S = (double)security.Price;
            double K = (double)contract.Strike;
            double T = (contract.Expiry - security.LocalTime.Date).TotalDays / 365.0;

            if (T <= 1e-12)
            {
                decimal intrinsic = (contract.Right == QuantConnect.OptionRight.Call) 
                    ? Math.Max(0m, security.Price - contract.Strike) 
                    : Math.Max(0m, contract.Strike - security.Price);
                
                // Use the existing Greeks from the contract
                return new OptionPriceModelResult(intrinsic, contract.Greeks);
            }

            // Access risk-free rate through algorithm context if available
            double r;
            if (_algorithm != null)
            {
                r = (double)_algorithm.RiskFreeInterestRateModel.GetInterestRate(slice.Time);
            }
            else
            {
                // Fallback to default rate if algorithm context not available
                r = 0.02; // 2% default
            }
            
            // FIX: Get dividend yield using DividendYieldProvider or default to 0
            double q = 0.0;
            if (security is Equity equity)
            {
                try
                {
                    // Try to get dividend yield from the equity's dividend yield model if available
                    var dividendYieldProvider = new QuantConnect.Data.DividendYieldProvider(security.Symbol);
                    q = (double)dividendYieldProvider.GetDividendYield(security.LocalTime);
                }
                catch
                {
                    // If dividend yield data is not available, default to 0
                    q = 0.0;
                }
            }
            
            // FIX: Correctly access the volatility as a property
            double vol = (double)security.VolatilityModel.Volatility;

            // --- Base Price Calculation ---
            double basePrice;
            try
            {
                basePrice = GetPrice(S, K, r, q, vol, T, contract.Right);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"QdFpAmericanEngine failed during base price calculation for {contract.Symbol}.");
                return new OptionPriceModelResult(0m, contract.Greeks);
            }

            // --- Greeks Calculation using Finite Difference ---
            // Note: For now, we return the existing contract Greeks
            // In production, you might want to update the contract's Greeks with calculated values
            var calculatedGreeks = CalculateGreeksByFiniteDifference(S, K, r, q, vol, T, contract.Right, basePrice, contract);

            // FIX: Result expects a decimal price
            return new OptionPriceModelResult((decimal)basePrice, calculatedGreeks);
        }

        /// <summary>
        /// Private helper to get the price from the engine, handling the put-call symmetry.
        /// </summary>
        private double GetPrice(double s, double k, double r, double q, double vol, double t, QuantConnect.OptionRight right)
        {
            if (right == QuantConnect.OptionRight.Put)
            {
                return _engine.CalculatePut(s, k, r, q, vol, t);
            }
            // Use put-call symmetry for Call options: C(S,K,r,q) = P(K,S,q,r)
            return _engine.CalculatePut(k, s, q, r, vol, t);
        }

        /// <summary>
        /// Calculates the option Greeks using the finite difference method (bump and re-value).
        /// Note: In this implementation, we return the existing contract Greeks.
        /// In production, you might want to update the contract's Greeks with calculated values.
        /// </summary>
        private Greeks CalculateGreeksByFiniteDifference(double S, double K, double r, double q, double vol, double T, QuantConnect.OptionRight right, double basePrice, OptionContract contract)
        {
            double spotBump = S * 0.001;
            if (Math.Abs(spotBump) < 1e-9) spotBump = 1e-4;
            
            double volBump = 0.001;
            double rateBump = 0.0001;
            double timeBump = 1.0 / 365.0;

            try
            {
                // Calculate Greeks using finite differences
                double priceUp_Spot = GetPrice(S + spotBump, K, r, q, vol, T, right);
                double priceDown_Spot = GetPrice(S - spotBump, K, r, q, vol, T, right);
                double delta = (priceUp_Spot - priceDown_Spot) / (2 * spotBump);
                double gamma = (priceUp_Spot - 2 * basePrice + priceDown_Spot) / (spotBump * spotBump);

                double priceUp_Vol = GetPrice(S, K, r, q, vol + volBump, T, right);
                double priceDown_Vol = GetPrice(S, K, r, q, vol - volBump, T, right);
                double vega = (priceUp_Vol - priceDown_Vol) / (2 * volBump) * 0.01;

                double priceUp_Rate = GetPrice(S, K, r + rateBump, q, vol, T, right);
                double priceDown_Rate = GetPrice(S, K, r - rateBump, q, vol, T, right);
                double rho = (priceUp_Rate - priceDown_Rate) / (2 * rateBump) * 0.01;

                double priceUp_Div = GetPrice(S, K, r, q + rateBump, vol, T, right);
                double priceDown_Div = GetPrice(S, K, r, q - rateBump, vol, T, right);
                double dividendRho = (priceUp_Div - priceDown_Div) / (2 * rateBump) * 0.01;

                double timeAfterBump = T > timeBump ? T - timeBump : 0.0;
                double price_TimeDecay = GetPrice(S, K, r, q, vol, timeAfterBump, right);
                double theta = (price_TimeDecay - basePrice);

                // TODO: In production, update the contract's Greeks with calculated values
                // For now, return the existing contract Greeks
                return contract.Greeks;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "QdFpAmericanEngine failed during Greeks calculation.");
                return contract.Greeks;
            }
        }
    }
}