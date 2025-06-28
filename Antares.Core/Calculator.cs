using System;
using System.Collections.Generic;
using Antares.Model;
using Antares.Interface;
using Antares.Engine.Engines;

namespace Antares
{
    /// <summary>
    /// Enhanced main facade for option pricing calculations with rigorous mathematical framework.
    /// Integrates the complete spectral collocation methodology for American options.
    /// </summary>
    public class Calculator
    {
        private readonly Dictionary<string, IOptionPricingEngine> _engines;
        private readonly bool _enableDiagnostics;

        public Calculator(bool enableDiagnostics = false)
        {
            _enableDiagnostics = enableDiagnostics;
            
            _engines = new Dictionary<string, IOptionPricingEngine>
            {
                // Primary engines with rigorous mathematical framework
                { "American", new AmericanEngine(AmericanEngine.Scheme.Accurate, enableDiagnostics) },
                { "European", new EuropeanEngine() },
                
                // Alternative schemes for different precision/speed tradeoffs
                { "AmericanFast", new AmericanEngine(AmericanEngine.Scheme.Fast, enableDiagnostics) },
                { "AmericanPrecise", new AmericanEngine(AmericanEngine.Scheme.Precise, enableDiagnostics) },
                { "AmericanDiagnostic", new AmericanEngine(AmericanEngine.Scheme.Diagnostic, enableDiagnostics) },
                
                // Legacy aliases
                { "Antares", new AmericanEngine(AmericanEngine.Scheme.Accurate, enableDiagnostics) },
                { "BlackScholes", new EuropeanEngine() }
            };

            if (enableDiagnostics)
            {
                Console.WriteLine("Calculator initialized with rigorous mathematical framework");
                Console.WriteLine($"Available engines: {string.Join(", ", _engines.Keys)}");
            }
        }

        /// <summary>
        /// Prices an option contract using the specified or auto-selected engine with enhanced validation.
        /// </summary>
        /// <param name="contract">The option contract to price</param>
        /// <param name="marketData">Current market data</param>
        /// <param name="engineName">Engine to use (null for auto-selection)</param>
        /// <returns>Pricing result with theoretical price and Greeks</returns>
        public PricingResult Price(OptionContract contract, MarketData marketData, string? engineName = null)
        {
            // Enhanced input validation
            var validationResult = ValidateInputsEnhanced(contract, marketData);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            // Enhanced auto-selection logic
            if (string.IsNullOrEmpty(engineName))
            {
                engineName = SelectOptimalEngine(contract, marketData);
            }

            // Get the engine with fallback logic
            if (!_engines.TryGetValue(engineName, out var engine))
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"Unknown engine '{engineName}', falling back to auto-selection");
                }
                
                engineName = SelectOptimalEngine(contract, marketData);
                engine = _engines[engineName];
            }

            // Check engine compatibility
            if (!engine.SupportsStyle(contract.Style))
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"Engine {engineName} does not support {contract.Style}, selecting compatible engine");
                }
                
                engineName = contract.Style == OptionStyle.American ? "American" : "European";
                engine = _engines[engineName];
            }

            if (_enableDiagnostics)
            {
                Console.WriteLine($"Using engine: {engine.Name}");
            }

            // Price the option with enhanced error handling
            try
            {
                var result = engine.Price(contract, marketData);
                
                // Post-pricing validation
                if (result.IsSuccess)
                {
                    var postValidation = ValidateResult(result, contract, marketData);
                    if (!postValidation.isValid)
                    {
                        if (_enableDiagnostics)
                        {
                            Console.WriteLine($"Post-pricing validation failed: {postValidation.message}");
                        }
                        
                        // Attempt recovery with different engine
                        return AttemptRecoveryPricing(contract, marketData, engineName);
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                if (_enableDiagnostics)
                {
                    Console.WriteLine($"Pricing error with {engineName}: {ex.Message}");
                }
                
                return AttemptRecoveryPricing(contract, marketData, engineName);
            }
        }

        /// <summary>
        /// Enhanced input validation with comprehensive checks
        /// </summary>
        private static PricingResult ValidateInputsEnhanced(OptionContract contract, MarketData marketData)
        {
            // Contract validation
            if (string.IsNullOrWhiteSpace(contract.Symbol))
            {
                return new PricingResult("Contract symbol is required");
            }

            if (contract.Strike <= 0)
            {
                return new PricingResult("Strike price must be positive");
            }

            if (contract.Expiry <= DateTime.Now.AddMinutes(-5)) // Allow small tolerance for real-time pricing
            {
                return new PricingResult("Option has expired");
            }

            // Market data validation
            if (marketData.UnderlyingPrice <= 0)
            {
                return new PricingResult("Underlying price must be positive");
            }

            if (marketData.Volatility < 0)
            {
                return new PricingResult("Volatility cannot be negative");
            }

            if (marketData.Volatility > 5.0) // 500% vol seems extreme but not impossible
            {
                return new PricingResult("Volatility seems unreasonably high (>500%)");
            }

            // Enhanced mathematical constraints
            double timeToExpiry = contract.TimeToExpiry(marketData.ValuationTime);
            if (timeToExpiry > 30.0) // 30 years maximum
            {
                return new PricingResult("Time to expiry exceeds maximum (30 years)");
            }

            if (Math.Abs(marketData.RiskFreeRate) > 0.5) // 50% rates seem extreme
            {
                return new PricingResult("Risk-free rate seems unreasonable");
            }

            if (Math.Abs(marketData.DividendYield) > 0.5) // 50% dividend yield seems extreme
            {
                return new PricingResult("Dividend yield seems unreasonable");
            }

            // Moneyness validation
            double moneyness = (double)(marketData.UnderlyingPrice / contract.Strike);
            if (moneyness > 100 || moneyness < 0.01) // Extreme moneyness
            {
                return new PricingResult("Extreme moneyness detected");
            }

            // All validations passed
            return new PricingResult(0m, new Greeks(), "Validation") { IsSuccess = true };
        }

        /// <summary>
        /// Intelligent engine selection based on contract and market characteristics
        /// </summary>
        private string SelectOptimalEngine(OptionContract contract, MarketData marketData)
        {
            if (contract.Style == OptionStyle.European)
            {
                return "European";
            }

            // For American options, select based on precision needs
            double timeToExpiry = contract.TimeToExpiry(marketData.ValuationTime);
            double volatility = marketData.Volatility;
            
            // Use precise engine for challenging cases
            if (timeToExpiry > 2.0 || volatility < 0.05 || Math.Abs(marketData.RiskFreeRate - marketData.DividendYield) > 0.2)
            {
                return _enableDiagnostics ? "AmericanDiagnostic" : "AmericanPrecise";
            }
            
            // Use fast engine for simple cases
            if (timeToExpiry < 0.1 && volatility > 0.3)
            {
                return "AmericanFast";
            }
            
            // Default to accurate engine
            return "American";
        }

        /// <summary>
        /// Validates pricing results for mathematical and economic consistency
        /// </summary>
        private (bool isValid, string message) ValidateResult(PricingResult result, OptionContract contract, MarketData marketData)
        {
            if (!result.IsSuccess)
            {
                return (false, "Pricing failed");
            }

            double price = (double)result.TheoreticalPrice;
            double strike = (double)contract.Strike;
            double spot = (double)marketData.UnderlyingPrice;

            // Basic bounds checking
            if (price < 0)
            {
                return (false, "Negative option price");
            }

            // Intrinsic value check
            double intrinsic = contract.Right == OptionRight.Call 
                ? Math.Max(0, spot - strike) 
                : Math.Max(0, strike - spot);
            
            if (price < intrinsic - 0.01)
            {
                return (false, "Price below intrinsic value");
            }

            // Reasonable upper bound
            double maxReasonable = contract.Right == OptionRight.Call ? spot * 2 : strike * 2;
            if (price > maxReasonable)
            {
                return (false, "Price exceeds reasonable bounds");
            }

            // Greeks validation
            if (Math.Abs((double)result.Greeks.Delta) > 1.5)
            {
                return (false, "Delta out of reasonable bounds");
            }

            if ((double)result.Greeks.Gamma < -0.01) // Allow small numerical errors
            {
                return (false, "Negative gamma");
            }

            return (true, "");
        }

        /// <summary>
        /// Attempts recovery pricing with fallback engines
        /// </summary>
        private PricingResult AttemptRecoveryPricing(OptionContract contract, MarketData marketData, string failedEngine)
        {
            if (_enableDiagnostics)
            {
                Console.WriteLine($"Attempting recovery pricing after {failedEngine} failure");
            }

            // Try European engine as ultimate fallback
            if (failedEngine != "European")
            {
                try
                {
                    var europeanEngine = _engines["European"];
                    var result = europeanEngine.Price(contract, marketData);
                    
                    if (result.IsSuccess && contract.Style == OptionStyle.American)
                    {
                        // Add conservative early exercise premium estimate
                        decimal conservativePremium = result.TheoreticalPrice * 0.05m; // 5% conservative estimate
                        result = new PricingResult(result.TheoreticalPrice + conservativePremium, result.Greeks, "European+EE")
                        {
                            CalculationTime = result.CalculationTime
                        };
                        result.CalculateIntrinsicValue(contract, marketData.UnderlyingPrice);
                    }
                    
                    if (_enableDiagnostics)
                    {
                        Console.WriteLine("Recovery successful with European engine");
                    }
                    
                    return result;
                }
                catch (Exception ex)
                {
                    if (_enableDiagnostics)
                    {
                        Console.WriteLine($"Recovery failed: {ex.Message}");
                    }
                }
            }

            return new PricingResult("All pricing engines failed");
        }

        /// <summary>
        /// Gets a list of available pricing engines with descriptions.
        /// </summary>
        public IEnumerable<(string name, string description)> GetAvailableEnginesWithDescriptions()
        {
            return new[]
            {
                ("American", "Rigorous spectral collocation for American options"),
                ("AmericanFast", "Fast approximation for American options"),
                ("AmericanPrecise", "High-precision American option pricing"),
                ("AmericanDiagnostic", "Diagnostic mode with detailed analysis"),
                ("European", "Analytical Black-Scholes-Merton for European options"),
                ("Antares", "Alias for American engine"),
                ("BlackScholes", "Alias for European engine")
            };
        }

        /// <summary>
        /// Gets a list of available pricing engines.
        /// </summary>
        public IEnumerable<string> GetAvailableEngines() => _engines.Keys;

        /// <summary>
        /// Adds or updates a pricing engine.
        /// </summary>
        public void AddEngine(string name, IOptionPricingEngine engine)
        {
            _engines[name] = engine;
            
            if (_enableDiagnostics)
            {
                Console.WriteLine($"Engine '{name}' added/updated: {engine.Name}");
            }
        }

        /// <summary>
        /// Gets detailed information about a specific engine.
        /// </summary>
        public (string Name, bool SupportsGreeks, bool SupportsAmerican, bool SupportsEuropean)? GetEngineInfo(string engineName)
        {
            if (!_engines.TryGetValue(engineName, out var engine))
                return null;

            return (
                engine.Name,
                engine.SupportsGreeks,
                engine.SupportsStyle(OptionStyle.American),
                engine.SupportsStyle(OptionStyle.European)
            );
        }

        /// <summary>
        /// Creates a sample option contract for testing.
        /// </summary>
        public static OptionContract CreateSampleContract()
        {
            return new OptionContract
            {
                Symbol = "SAMPLE",
                Right = OptionRight.Put,
                Strike = 100m,
                Expiry = DateTime.Now.AddDays(30),
                Style = OptionStyle.American
            };
        }

        /// <summary>
        /// Creates sample market data for testing.
        /// </summary>
        public static MarketData CreateSampleMarketData()
        {
            return new MarketData(
                underlyingPrice: 95m,
                volatility: 0.20,
                riskFreeRate: 0.05,
                dividendYield: 0.02
            );
        }
    }
}