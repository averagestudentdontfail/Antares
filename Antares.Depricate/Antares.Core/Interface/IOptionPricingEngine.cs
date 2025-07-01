using Antares.Model;

namespace Antares.Interface
{
    /// <summary>
    /// Interface for option pricing engines
    /// </summary>
    public interface IOptionPricingEngine
    {
        string Name { get; }
        PricingResult Price(OptionContract contract, MarketData marketData);
        bool SupportsStyle(OptionStyle style);
        bool SupportsGreeks { get; }
    }
}