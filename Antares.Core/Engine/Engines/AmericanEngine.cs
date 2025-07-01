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
                Scheme.Fast => new QdFpLegendreScheme(l: 7, m: 3, n: 8, p: 16),
                Scheme.Precise => new QdFpTanhSinhScheme(m: 12, n: 24, accuracy: 1e-12),
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

                // Handle expired options
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

                // Calculate American option price
                double basePrice = GetAmericanPrice(S, K, r, q, vol, T, contract.Right);
                
                // Calculate Greeks using finite differences with improved accuracy
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

        private double GetAmericanPrice(double s, double k, double r, double q, double vol, double t, ModelOptionRight right)
        {
            if (right == ModelOptionRight.Put)
            {
                return _coreEngine.CalculatePut(s, k, r, q, vol, t);
            }
            else
            {
                // For American calls, use put-call transformation
                // American Call(S,K,r,q,T) = Put(K,S,q,r,T) + S*exp(-q*T) - K*exp(-r*T)
                double putValue = _coreEngine.CalculatePut(k, s, q, r, vol, t);
                double forwardDifference = s * Math.Exp(-q * t) - k * Math.Exp(-r * t);
                double callValue = putValue + forwardDifference;
                
                // Ensure call value is at least intrinsic value
                double intrinsic = Math.Max(0.0, s - k);
                return Math.Max(callValue, intrinsic);
            }
        }

        private Greeks CalculateGreeksByFiniteDifference(double S, double K, double r, double q, double vol, double T, ModelOptionRight right, double basePrice)
        {
            // Use relative bumps for better numerical accuracy
            double spotBump = Math.Max(S * 0.0001, 0.01);
            double volBump = Math.Max(vol * 0.01, 0.0001);
            double rateBump = Math.Max(Math.Abs(r) * 0.01, 0.0001);
            double timeBump = Math.Min(T * 0.01, 1.0 / 365.0);

            try
            {
                // Delta and Gamma using central differences
                double priceUp_Spot = GetAmericanPrice(S + spotBump, K, r, q, vol, T, right);
                double priceDown_Spot = GetAmericanPrice(S - spotBump, K, r, q, vol, T, right);
                double delta = (priceUp_Spot - priceDown_Spot) / (2 * spotBump);
                double gamma = (priceUp_Spot - 2 * basePrice + priceDown_Spot) / (spotBump * spotBump);

                // Vega using central differences
                double priceUp_Vol = GetAmericanPrice(S, K, r, q, vol + volBump, T, right);
                double priceDown_Vol = GetAmericanPrice(S, K, r, q, vol - volBump, T, right);
                double vega = (priceUp_Vol - priceDown_Vol) / (2 * volBump) * 0.01;

                // Rho using central differences
                double priceUp_Rate = GetAmericanPrice(S, K, r + rateBump, q, vol, T, right);
                double priceDown_Rate = GetAmericanPrice(S, K, r - rateBump, q, vol, T, right);
                double rho = (priceUp_Rate - priceDown_Rate) / (2 * rateBump) * 0.01;

                // Theta using forward difference (time decay)
                double theta;
                if (T > timeBump)
                {
                    double price_TimeDecay = GetAmericanPrice(S, K, r, q, vol, T - timeBump, right);
                    theta = price_TimeDecay - basePrice;
                }
                else
                {
                    // For very short times, estimate theta from Black-Scholes
                    double bsTheta = CalculateBlackScholesTheta(S, K, r, q, vol, T, right);
                    theta = bsTheta * timeBump;
                }

                // Apply bounds checking to Greeks
                delta = Math.Max(-1.1, Math.Min(1.1, delta));
                gamma = Math.Max(0.0, Math.Min(100.0, gamma));
                vega = Math.Max(0.0, vega);
                
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
                // Fallback to Black-Scholes Greeks if finite differences fail
                return CalculateBlackScholesGreeks(S, K, r, q, vol, T, right);
            }
        }

        private double CalculateBlackScholesTheta(double S, double K, double r, double q, double vol, double T, ModelOptionRight right)
        {
            if (T <= 1e-12 || vol <= 1e-12) return 0.0;

            double sqrtT = Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * sqrtT);
            double d2 = d1 - vol * sqrtT;
            
            var norm = new StandardNormalDistribution();
            double nd1 = norm.Density(d1);
            
            if (right == ModelOptionRight.Call)
            {
                double theta = -S * Math.Exp(-q * T) * nd1 * (vol / (2 * sqrtT))
                             - r * K * Math.Exp(-r * T) * norm.CumulativeDistribution(d2)
                             + q * S * Math.Exp(-q * T) * norm.CumulativeDistribution(d1);
                return theta;
            }
            else
            {
                double theta = -S * Math.Exp(-q * T) * nd1 * (vol / (2 * sqrtT))
                             + r * K * Math.Exp(-r * T) * norm.CumulativeDistribution(-d2)
                             - q * S * Math.Exp(-q * T) * norm.CumulativeDistribution(-d1);
                return theta;
            }
        }

        private Greeks CalculateBlackScholesGreeks(double S, double K, double r, double q, double vol, double T, ModelOptionRight right)
        {
            if (T <= 1e-12 || vol <= 1e-12)
            {
                double intrinsic = right == ModelOptionRight.Call ? Math.Max(0, S - K) : Math.Max(0, K - S);
                decimal deltaExpired = intrinsic > 0 ? (right == ModelOptionRight.Call ? 1m : -1m) : 0m;
                return new Greeks(deltaExpired, 0m, 0m, 0m, 0m, 0m);
            }

            double sqrtT = Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * sqrtT);
            double d2 = d1 - vol * sqrtT;
            
            var norm = new StandardNormalDistribution();
            double nd1 = norm.Density(d1);
            
            double delta, gamma, vega, theta, rho;
            
            gamma = Math.Exp(-q * T) * nd1 / (S * vol * sqrtT);
            vega = S * Math.Exp(-q * T) * nd1 * sqrtT * 0.01;
            
            if (right == ModelOptionRight.Call)
            {
                delta = Math.Exp(-q * T) * norm.CumulativeDistribution(d1);
                theta = -(S * vol * Math.Exp(-q * T) * nd1) / (2 * sqrtT)
                        - r * K * Math.Exp(-r * T) * norm.CumulativeDistribution(d2)
                        + q * S * Math.Exp(-q * T) * norm.CumulativeDistribution(d1);
                rho = K * T * Math.Exp(-r * T) * norm.CumulativeDistribution(d2) * 0.01;
            }
            else
            {
                delta = -Math.Exp(-q * T) * norm.CumulativeDistribution(-d1);
                theta = -(S * vol * Math.Exp(-q * T) * nd1) / (2 * sqrtT)
                        + r * K * Math.Exp(-r * T) * norm.CumulativeDistribution(-d2)
                        - q * S * Math.Exp(-q * T) * norm.CumulativeDistribution(-d1);
                rho = -K * T * Math.Exp(-r * T) * norm.CumulativeDistribution(-d2) * 0.01;
            }

            return new Greeks(
                delta: (decimal)delta,
                gamma: (decimal)gamma,
                vega: (decimal)vega,
                theta: (decimal)theta / 365.0m,
                rho: (decimal)rho,
                lambda: 0m
            );
        }
    }
}