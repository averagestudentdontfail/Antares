using System;
using Antares.Model;
using Antares.Interface;
using Antares.Engine;
using Antares.Distribution;
using ModelOptionRight = Antares.Model.OptionRight; 

namespace Antares.Engine.Engines
{
    public class AmericanEngine : IOptionPricingEngine
    {
        private readonly QdFpAmericanEngine _coreEngine;
        
        public string Name => "Antares American (QdFp)";
        public bool SupportsGreeks => true;

        public enum Scheme
        {
            Fast,
            Accurate,
            Precise
        }

        public AmericanEngine(Scheme scheme = Scheme.Accurate)
        {
            IQdFpIterationScheme iterationScheme = scheme switch
            {
                Scheme.Fast => new QdFpLegendreScheme(l: 7, m: 2, n: 7, p: 27),
                Scheme.Precise => new QdFpTanhSinhScheme(m: 10, n: 30, accuracy: 1e-12),
                _ => new QdFpLegendreLobattoScheme(l: 16, m: 8, n: 16, finalAccuracy: 1e-10)
            };
            
            _coreEngine = new QdFpAmericanEngine(iterationScheme);
        }

        public bool SupportsStyle(OptionStyle style) => style == OptionStyle.American;

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

                double basePrice = GetPrice(S, K, r, q, vol, T, contract.Right);
                var greeks = CalculateGreeksByFiniteDifference(S, K, r, q, vol, T, contract.Right, basePrice);
                
                var result = new PricingResult((decimal)basePrice, greeks, Name)
                {
                    CalculationTime = DateTime.Now - startTime
                };
                
                result.CalculateIntrinsicValue(contract, marketData.UnderlyingPrice);
                return result;
            }
            catch (Exception ex)
            {
                return new PricingResult($"American option pricing failed: {ex.Message}")
                {
                    CalculationTime = DateTime.Now - startTime
                };
            }
        }

        private double GetPrice(double s, double k, double r, double q, double vol, double t, ModelOptionRight right)
        {
            if (right == ModelOptionRight.Put)
            {
                return _coreEngine.CalculatePut(s, k, r, q, vol, t);
            }
            return _coreEngine.CalculatePut(k, s, q, r, vol, t);
        }

        private Greeks CalculateGreeksByFiniteDifference(double S, double K, double r, double q, double vol, double T, ModelOptionRight right, double basePrice)
        {
            double spotBump = S * 0.001;
            if (Math.Abs(spotBump) < 1e-9) spotBump = 1e-4;
            
            double volBump = 0.001;
            double rateBump = 0.0001;
            double timeBump = 1.0 / 365.0;

            try
            {
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

                double timeAfterBump = T > timeBump ? T - timeBump : 0.0;
                double price_TimeDecay = GetPrice(S, K, r, q, vol, timeAfterBump, right);
                double theta = (price_TimeDecay - basePrice);

                return new Greeks(
                    delta: (decimal)delta,
                    gamma: (decimal)gamma,
                    vega: (decimal)vega,
                    theta: (decimal)theta / 365m,
                    rho: (decimal)rho,
                    lambda: 0m
                );
            }
            catch (Exception)
            {
                return new Greeks();
            }
        }
    }
}