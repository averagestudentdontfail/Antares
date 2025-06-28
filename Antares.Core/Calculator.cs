using System;
using System.Collections.Generic;
using Antares.Model;
using Antares.Interface;
using Antares.Engine.Engines;

namespace Antares
{
    public class Calculator
    {
        private readonly Dictionary<string, IOptionPricingEngine> _engines;

        public Calculator()
        {
            _engines = new Dictionary<string, IOptionPricingEngine>
            {
                { "American", new AmericanEngine() },
                { "European", new EuropeanEngine() },
                { "Antares", new AmericanEngine() },
                { "BlackScholes", new EuropeanEngine() }
            };
        }

        public PricingResult Price(OptionContract contract, MarketData marketData, string? engineName = null)
        {
            var validationResult = ValidateInputs(contract, marketData);
            if (!validationResult.IsSuccess)
            {
                return validationResult;
            }

            if (string.IsNullOrEmpty(engineName))
            {
                engineName = contract.Style == OptionStyle.American ? "American" : "European";
            }

            if (!_engines.TryGetValue(engineName, out var engine))
            {
                return new PricingResult($"Unknown pricing engine: {engineName}");
            }

            if (!engine.SupportsStyle(contract.Style))
            {
                return new PricingResult($"Engine {engineName} does not support {contract.Style} options");
            }

            return engine.Price(contract, marketData);
        }

        public IEnumerable<string> GetAvailableEngines() => _engines.Keys;

        public void AddEngine(string name, IOptionPricingEngine engine)
        {
            _engines[name] = engine;
        }

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

        private static PricingResult ValidateInputs(OptionContract contract, MarketData marketData)
        {
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

            if (marketData.UnderlyingPrice <= 0)
            {
                return new PricingResult("Underlying price must be positive");
            }

            if (marketData.Volatility < 0)
            {
                return new PricingResult("Volatility cannot be negative");
            }

            if (marketData.Volatility > 10)
            {
                return new PricingResult("Volatility seems unreasonably high (>1000%)");
            }

            return new PricingResult(0m, new Greeks(), "Validation") { IsSuccess = true };
        }

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