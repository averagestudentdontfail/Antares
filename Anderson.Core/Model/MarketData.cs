using System;

namespace Anderson.Model
{
    /// <summary>
    /// Contains market data needed for option pricing
    /// </summary>
    public class MarketData
    {
        public decimal UnderlyingPrice { get; set; }
        public double RiskFreeRate { get; set; }
        public double DividendYield { get; set; }
        public double Volatility { get; set; }
        public DateTime ValuationTime { get; set; } = DateTime.Now;

        public MarketData(decimal underlyingPrice, double volatility, 
                         double riskFreeRate = 0.02, double dividendYield = 0.0)
        {
            UnderlyingPrice = underlyingPrice;
            Volatility = volatility;
            RiskFreeRate = riskFreeRate;
            DividendYield = dividendYield;
        }

        public override string ToString()
        {
            return $"S: {UnderlyingPrice}, Vol: {Volatility:P2}, r: {RiskFreeRate:P2}, q: {DividendYield:P2}";
        }
    }
}