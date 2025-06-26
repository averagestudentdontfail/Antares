using System;
using Anderson.Model;
using Anderson.Interface;
using Anderson.Distribution;

namespace Anderson.Engine.Engines
{
    /// <summary>
    /// Black-Scholes-Merton European option pricing engine
    /// </summary>
    public class BlackScholesEngine : IOptionPricingEngine
    {
        public string Name => "Black-Scholes-Merton";
        public bool SupportsGreeks => true;

        public bool SupportsStyle(OptionStyle style) => style == OptionStyle.European;

        public PricingResult Price(OptionContract contract, MarketData marketData)
        {
            var startTime = DateTime.Now;
            
            try
            {
                double S = (double)marketData.UnderlyingPrice;
                double K = (double)contract.Strike;
                double T = contract.TimeToExpiry(marketData.ValuationTime);
                double r = marketData.RiskFreeRate;
                double q = marketData.DividendYield;
                double vol = marketData.Volatility;

                // Handle expiry case
                if (T <= 1e-12)
                {
                    decimal intrinsic = contract.Right == OptionRight.Call 
                        ? Math.Max(0m, marketData.UnderlyingPrice - contract.Strike)
                        : Math.Max(0m, contract.Strike - marketData.UnderlyingPrice);
                    
                    var expiredGreeks = new Greeks(
                        delta: intrinsic > 0 ? (contract.Right == OptionRight.Call ? 1m : -1m) : 0m
                    );
                    
                    var expiredResult = new PricingResult(intrinsic, expiredGreeks, Name);
                    expiredResult.CalculateIntrinsicValue(contract, marketData.UnderlyingPrice);
                    return expiredResult;
                }

                var andersonRight = contract.Right == OptionRight.Call ? Anderson.OptionRight.Call : Anderson.OptionRight.Put;
                
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
                return new PricingResult($"Black-Scholes pricing failed: {ex.Message}")
                {
                    CalculationTime = DateTime.Now - startTime
                };
            }
        }
    }
}