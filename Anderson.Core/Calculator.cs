using System;
using System.Collections.Generic;
using Anderson.Model;
using Anderson.Interface;
using Anderson.Engine;

namespace Anderson
{
    /// <summary>
    /// Main facade for option pricing calculations
    /// </summary>
    public class OptionCalculator
    {
        private readonly Dictionary<string, IOptionPricingEngine> _engines;

        public OptionCalculator()
        {
            _engines = new Dictionary<string, IOptionPricingEngine>
            {
                { "Anderson", new AndersonAmericanEngine() },
                { "BlackScholes", new BlackScholesEngine() }
            };
        }

        public PricingResult Price(OptionContract contract, MarketData marketData, string? engineName = null)
        {
            // Auto-select engine if not specified
            if (string.IsNullOrEmpty(engineName))
            {
                engineName = contract.Style == OptionStyle.American ? "Anderson" : "BlackScholes";
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
    }
}