using System;
using System.Collections.Generic;
using Anderson.Model;
using Anderson.Interface;
using Anderson.Engine.Engines;

namespace Anderson
{
    /// <summary>
    /// Main facade for option pricing calculations.
    /// Provides a simple interface to price options using different engines.
    /// </summary>
    public class Calculator
    {
        private readonly Dictionary<string, IOptionPricingEngine> _engines;

        public Calculator()
        {
            _engines = new Dictionary<string, IOptionPricingEngine>
            {
                { "American", new AmericanEngine() },
                { "European", new EuropeanEngine() },
                { "Anderson", new AmericanEngine() }, // Alias for American
                { "BlackScholes", new EuropeanEngine() } // Alias for European
            };
        }

        /// <summary>
        /// Prices an option contract using the specified or auto-selected engine.
        /// </summary>
        /// <param name="contract">The option contract to price</param>
        /// <param name="marketData">Current market data</param>
        /// <param name="engineName">Engine to use (null for auto-selection)</param>
        /// <returns>Pricing result with theoretical price and Greeks</returns>
        public PricingResult Price(OptionContract contract, MarketData marketData, string? engineName = null)
        {
            // Validate inputs
            var validationResult = ValidateInputs(contract, marketData);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            // Auto-select engine if not specified
            if (string.IsNullOrEmpty(engineName))
            {
                engineName = contract.Style == OptionStyle.American ? "American" : "European";
            }

            // Get the engine
            if (!_engines.TryGetValue(engineName, out var engine))
            {
                return new PricingResult($"Unknown pricing engine: {engineName}");
            }

            // Check if engine supports the option style
            if (!engine.SupportsStyle(contract.Style))
            {
                return new PricingResult($"Engine {engineName} does not support {contract.Style} options");
            }

            // Price the option
            return engine.Price(contract, marketData);
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
        }

        /// <summary>
        /// Gets information about a specific engine.
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
        /// Validates option contract and market data inputs.
        /// </summary>
        private static PricingResult ValidateInputs(OptionContract contract, MarketData marketData)
        {
            // Validate contract
            if (string.IsNullOrWhiteSpace(contract.Symbol))
            {
                return new PricingResult("Contract symbol is required");
            }

            if (contract.Strike <= 0)
            {
                return new PricingResult("Strike price must be positive");
            }

            if (contract.Expiry <= DateTime.Now)
            {
                return new PricingResult("Option has already expired");
            }

            // Validate market data
            if (marketData.UnderlyingPrice <= 0)
            {
                return new PricingResult("Underlying price must be positive");
            }

            if (marketData.Volatility < 0)
            {
                return new PricingResult("Volatility cannot be negative");
            }

            if (marketData.Volatility > 10) // 1000% vol seems unreasonable
            {
                return new PricingResult("Volatility seems unreasonably high (>1000%)");
            }

            // All validations passed
            return new PricingResult(0m, new Greeks(), "Validation") { IsSuccess = true };
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