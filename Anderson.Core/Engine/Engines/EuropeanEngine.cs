// Anderson.Core/Engine/Engines/EuropeanEngine.cs
using System;
using Anderson.Model;
using Anderson.Interface;
using Anderson.Distribution;
using ModelOptionRight = Anderson.Model.ModelOptionRight;

namespace Anderson.Engine.Engines
{
    /// <summary>
    /// Black-Scholes-Merton European option pricing engine with analytical Greeks.
    /// </summary>
    public class EuropeanEngine : IOptionPricingEngine
    {
        public string Name => "Black-Scholes-Merton";
        public bool SupportsGreeks => true;

        public bool SupportsStyle(OptionStyle style) => style == OptionStyle.European;

        public PricingResult Price(OptionContract contract, MarketData marketData)
        {
            var startTime = DateTime.Now;
            
            try
            {
                // Convert domain models to primitive types
                double S = (double)marketData.UnderlyingPrice;
                double K = (double)contract.Strike;
                double T = contract.TimeToExpiry(marketData.ValuationTime);
                double r = marketData.RiskFreeRate;
                double q = marketData.DividendYield;
                double vol = marketData.Volatility;

                // Handle expiry case
                if (T <= 1e-12)
                {
                    decimal intrinsic = contract.Right == ModelOptionRight.Call 
                        ? Math.Max(0m, marketData.UnderlyingPrice - contract.Strike)
                        : Math.Max(0m, contract.Strike - marketData.UnderlyingPrice);
                    
                    var expiredGreeks = new Greeks(
                        delta: intrinsic > 0 ? (contract.Right == ModelOptionRight.Call ? 1m : -1m) : 0m
                    );
                    
                    var expiredResult = new PricingResult(intrinsic, expiredGreeks, Name);
                    expiredResult.CalculateIntrinsicValue(contract, marketData.UnderlyingPrice);
                    return expiredResult;
                }

                // Convert to internal option right enum for BlackScholes calculation
                var andersonRight = contract.Right == ModelOptionRight.Call ? Anderson.ModelOptionRight.Call : Anderson.ModelOptionRight.Put;
                
                // Calculate price and Greeks using analytical Black-Scholes formulas
                double price = BlackScholes.Price(andersonRight, S, K, T, r, q, vol);
                var greeks = BlackScholes.Greeks(andersonRight, S, K, T, r, q, vol);

                var result = new PricingResult((decimal)price, greeks, Name)
                {
                    CalculationTime = DateTime.Now - startTime
                };
                
                result.CalculateIntrinsicValue(contract, marketData.UnderlyingPrice);
                return result;
            }
            catch (Exception ex)
            {
                return new PricingResult($"European option pricing failed: {ex.Message}")
                {
                    CalculationTime = DateTime.Now - startTime
                };
            }
        }
    }
}