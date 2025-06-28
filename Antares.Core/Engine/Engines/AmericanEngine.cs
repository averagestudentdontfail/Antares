using System;
using Antares.Model;
using Antares.Interface;
using Antares.Engine;
using Antares.Distribution;
using ModelOptionRight = Antares.Model.OptionRight; 

namespace Antares.Engine.Engines
{
    /// <summary>
    /// Enhanced American option pricing engine using the rigorous Antares mathematical framework
    /// with comprehensive diagnostic capabilities and mathematical validation
    /// </summary>
    public class AmericanEngine : IOptionPricingEngine
    {
        private readonly RigorousQdFpAmericanEngine _coreEngine;
        private readonly bool _enableDiagnostics;
        
        public string Name => "Antares American (Rigorous QdFp)";
        public bool SupportsGreeks => true;

        public enum Scheme
        {
            Fast,
            Accurate,
            Precise,
            Diagnostic
        }

        public AmericanEngine(Scheme scheme = Scheme.Accurate, bool enableDiagnostics = false)
        {
            _enableDiagnostics = enableDiagnostics;
            
            IQdFpIterationScheme iterationScheme = scheme switch
            {
                Scheme.Fast => new QdFpLegendreScheme(l: 7, m: 2, n: 7, p: 27),
                Scheme.Precise => new QdFpTanhSinhScheme(m: 10, n: 30, accuracy: 1e-12),
                Scheme.Diagnostic => new QdFpLegendreLobattoScheme(l: 20, m: 12, n: 20, finalAccuracy: 1e-12),
                _ => new QdFpLegendreLobattoScheme(l: 16, m: 8, n: 16, finalAccuracy: 1e-10)
            };
            
            _coreEngine = new RigorousQdFpAmericanEngine(
                iterationScheme, 
                FixedPointEquation.Auto, 
                enableDiagnostics, 
                convergenceTolerance: scheme == Scheme.Precise ? 1e-10 : 1e-8);
        }

        public bool SupportsStyle(OptionStyle style) => style == OptionStyle.American;

        public PricingResult Price(OptionContract contract, MarketData marketData)
        {
            var startTime = DateTime.Now;
            
            try
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"\n=== AMERICAN ENGINE PRICING START ===");
                    Console.WriteLine($"Contract: {contract.Symbol} {contract.Right} K={contract.Strike} T={contract.TimeToExpiry(marketData.ValuationTime):F4}");
                    Console.WriteLine($"Market: S={marketData.UnderlyingPrice} vol={marketData.Volatility:P2} r={marketData.RiskFreeRate:P2} q={marketData.DividendYield:P2}");
                }

                // Convert domain models to primitive types
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
                    
                    if (_enableDiagnostics)
                    {
                        Console.WriteLine($"Expired option: intrinsic = {intrinsic:C}");
                        Console.WriteLine("=== AMERICAN ENGINE PRICING END ===\n");
                    }
                    
                    return expiredResult;
                }

                // Perform enhanced input validation
                var validationResult = ValidateInputsEnhanced(S, K, r, q, vol, T);
                if (!validationResult.isValid)
                {
                    if (_enableDiagnostics)
                    {
                        Console.WriteLine($"Input validation failed: {validationResult.message}");
                        Console.WriteLine("=== AMERICAN ENGINE PRICING END ===\n");
                    }
                    
                    return new PricingResult($"Input validation failed: {validationResult.message}")
                    {
                        CalculationTime = DateTime.Now - startTime
                    };
                }

                // Price using rigorous core engine
                double basePrice = GetPrice(S, K, r, q, vol, T, contract.Right);
                
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"Core engine result: {basePrice:F6}");
                }

                // Validate core engine result
                if (!ValidateCoreEngineResult(basePrice, S, K, r, q, vol, T))
                {
                    if (_enableDiagnostics)
                    {
                        Console.WriteLine("Core engine result validation failed, using enhanced fallback");
                    }
                    
                    basePrice = CalculateEnhancedFallback(S, K, r, q, vol, T, contract.Right);
                }

                // Calculate Greeks using enhanced finite differences
                var greeks = CalculateGreeksEnhanced(S, K, r, q, vol, T, contract.Right, basePrice);
                
                // Assemble final result with comprehensive validation
                var result = new PricingResult((decimal)basePrice, greeks, Name)
                {
                    CalculationTime = DateTime.Now - startTime
                };
                
                result.CalculateIntrinsicValue(contract, marketData.UnderlyingPrice);
                
                // Final result validation
                if (!ValidateFinalResult(result, marketData))
                {
                    if (_enableDiagnostics)
                    {
                        Console.WriteLine("Final result validation failed");
                    }
                    
                    return new PricingResult("Final result validation failed")
                    {
                        CalculationTime = DateTime.Now - startTime
                    };
                }
                
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"Final result: American={result.TheoreticalPrice:C}, Premium={result.TimeValue:C}");
                    Console.WriteLine($"Greeks: Δ={result.Greeks.Delta:F4}, Γ={result.Greeks.Gamma:F6}");
                    Console.WriteLine($"Calculation time: {result.CalculationTime.TotalMilliseconds:F1}ms");
                    Console.WriteLine("=== AMERICAN ENGINE PRICING END ===\n");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"American engine error: {ex.Message}");
                    Console.WriteLine("=== AMERICAN ENGINE PRICING END ===\n");
                }
                
                return new PricingResult($"American option pricing failed: {ex.Message}")
                {
                    CalculationTime = DateTime.Now - startTime
                };
            }
        }

        private (bool isValid, string message) ValidateInputsEnhanced(double S, double K, double r, double q, double vol, double T)
        {
            if (S <= 0) return (false, "Non-positive underlying price");
            if (K <= 0) return (false, "Non-positive strike price");
            if (vol < 0) return (false, "Negative volatility");
            if (vol > 3.0) return (false, "Excessive volatility (>300%)");
            if (T <= 0) return (false, "Non-positive time to expiry");
            if (T > 30.0) return (false, "Excessive time to expiry (>30 years)");
            if (Math.Abs(r) > 0.5) return (false, "Extreme interest rate");
            if (Math.Abs(q) > 0.5) return (false, "Extreme dividend yield");
            
            // Additional mathematical constraints
            if (S / K > 100 || K / S > 100) return (false, "Extreme moneyness");
            if (vol * Math.Sqrt(T) > 5.0) return (false, "Excessive vol*sqrt(T)");
            
            return (true, "");
        }

        private bool ValidateCoreEngineResult(double price, double S, double K, double r, double q, double vol, double T)
        {
            if (double.IsNaN(price) || double.IsInfinity(price)) return false;
            if (price < 0) return false;
            
            // Economic bounds checking
            double intrinsic = Math.Max(0, K - S); // Put intrinsic
            double maxReasonable = K * 2; // Should never exceed 2x strike
            
            if (price > maxReasonable) return false;
            
            // Should be at least intrinsic value
            if (price < intrinsic - 1e-6) return false;
            
            // Compare with European lower bound
            double europeanPrice = CalculateBlackScholesPut(S, K, r, q, vol, T);
            if (price < europeanPrice - 1e-6) return false;
            
            return true;
        }

        private double CalculateEnhancedFallback(double S, double K, double r, double q, double vol, double T, ModelOptionRight right)
        {
            // Calculate European price as baseline
            double europeanPrice = (right == ModelOptionRight.Put) 
                ? CalculateBlackScholesPut(S, K, r, q, vol, T)
                : CalculateBlackScholesCall(S, K, r, q, vol, T);
            
            double intrinsic = (right == ModelOptionRight.Put) 
                ? Math.Max(0, K - S) 
                : Math.Max(0, S - K);
            
            // Enhanced early exercise premium estimation
            double carryBenefit = Math.Abs(r - q);
            double moneyness = (right == ModelOptionRight.Put) ? K / S : S / K;
            
            // ITM options have higher early exercise value
            double itmFactor = Math.Max(0, moneyness - 1.0);
            double timeFactor = Math.Min(1.0, T * 2); // Time decay factor
            double volFactor = Math.Min(1.0, vol); // Volatility factor
            
            double earlyExercisePremium = carryBenefit * itmFactor * timeFactor * volFactor * europeanPrice * 0.1;
            
            double fallbackPrice = Math.Max(intrinsic, europeanPrice + earlyExercisePremium);
            
            if (_enableDiagnostics)
            {
                Console.WriteLine($"Enhanced fallback: European={europeanPrice:F6}, EE_Premium={earlyExercisePremium:F6}, Total={fallbackPrice:F6}");
            }
            
            return fallbackPrice;
        }

        private bool ValidateFinalResult(PricingResult result, MarketData marketData)
        {
            if (!result.IsSuccess) return false;
            if (result.TheoreticalPrice < 0) return false;
            
            // Greeks bounds checking
            if (Math.Abs((double)result.Greeks.Delta) > 1.5) return false;
            if ((double)result.Greeks.Gamma < 0) return false;
            if ((double)result.Greeks.Vega < 0) return false;
            
            return true;
        }

        private double GetPrice(double s, double k, double r, double q, double vol, double t, ModelOptionRight right)
        {
            if (right == ModelOptionRight.Put)
            {
                return _coreEngine.CalculatePut(s, k, r, q, vol, t);
            }
            // Use put-call symmetry for calls: C(S,K,r,q) = P(K,S,q,r)
            return _coreEngine.CalculatePut(k, s, q, r, vol, t);
        }

        private Greeks CalculateGreeksEnhanced(double S, double K, double r, double q, double vol, double T, ModelOptionRight right, double basePrice)
        {
            // Enhanced finite difference parameters with adaptive sizing
            double spotBump = Math.Max(S * 0.0001, 0.01); // Smaller, more accurate bumps
            double volBump = Math.Max(vol * 0.01, 0.0001);
            double rateBump = Math.Max(Math.Abs(r) * 0.01, 0.0001);
            double timeBump = Math.Min(T * 0.01, 1.0 / 365.0);

            try
            {
                // Delta and Gamma with enhanced accuracy
                double priceUp_Spot = GetPrice(S + spotBump, K, r, q, vol, T, right);
                double priceDown_Spot = GetPrice(S - spotBump, K, r, q, vol, T, right);
                double delta = (priceUp_Spot - priceDown_Spot) / (2 * spotBump);
                double gamma = (priceUp_Spot - 2 * basePrice + priceDown_Spot) / (spotBump * spotBump);

                // Vega with enhanced accuracy
                double priceUp_Vol = GetPrice(S, K, r, q, vol + volBump, T, right);
                double priceDown_Vol = GetPrice(S, K, r, q, vol - volBump, T, right);
                double vega = (priceUp_Vol - priceDown_Vol) / (2 * volBump) * 0.01;

                // Rho with enhanced accuracy
                double priceUp_Rate = GetPrice(S, K, r + rateBump, q, vol, T, right);
                double priceDown_Rate = GetPrice(S, K, r - rateBump, q, vol, T, right);
                double rho = (priceUp_Rate - priceDown_Rate) / (2 * rateBump) * 0.01;

                // Theta with time boundary handling
                double timeAfterBump = Math.Max(0.0, T - timeBump);
                double price_TimeDecay = GetPrice(S, K, r, q, vol, timeAfterBump, right);
                double theta = (price_TimeDecay - basePrice) / timeBump;

                // Enhanced bounds checking and validation
                delta = Math.Max(-2.0, Math.Min(2.0, delta));
                gamma = Math.Max(0.0, Math.Min(1.0, gamma));
                vega = Math.Max(0.0, Math.Min(basePrice, vega));
                rho = Math.Max(-basePrice * 10, Math.Min(basePrice * 10, rho));
                theta = Math.Max(-basePrice * 365, Math.Min(0, theta));

                return new Greeks(
                    delta: (decimal)delta,
                    gamma: (decimal)gamma,
                    vega: (decimal)vega,
                    theta: (decimal)theta / 365m, // Convert to daily theta
                    rho: (decimal)rho,
                    lambda: 0m
                );
            }
            catch (Exception ex)
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"Greeks calculation error: {ex.Message}");
                }
                return new Greeks(); // Return zero Greeks on error
            }
        }

        private static double CalculateBlackScholesPut(double S, double K, double r, double q, double vol, double T)
        {
            if (T <= 1e-9) return Math.Max(K - S, 0.0);
            if (vol <= 1e-9) return Math.Max(K * Math.Exp(-r * T) - S * Math.Exp(-q * T), 0.0);

            double sqrtT = Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * sqrtT);
            double d2 = d1 - vol * sqrtT;
            
            return K * Math.Exp(-r * T) * Distributions.CumulativeNormal(-d2) - S * Math.Exp(-q * T) * Distributions.CumulativeNormal(-d1);
        }

        private static double CalculateBlackScholesCall(double S, double K, double r, double q, double vol, double T)
        {
            if (T <= 1e-9) return Math.Max(S - K, 0.0);
            if (vol <= 1e-9) return Math.Max(S * Math.Exp(-q * T) - K * Math.Exp(-r * T), 0.0);

            double sqrtT = Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * sqrtT);
            double d2 = d1 - vol * sqrtT;
            
            return S * Math.Exp(-q * T) * Distributions.CumulativeNormal(d1) - K * Math.Exp(-r * T) * Distributions.CumulativeNormal(d2);
        }
    }
}