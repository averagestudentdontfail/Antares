using System;

namespace Antares.Model
{
    /// <summary>
    /// Represents the option Greeks (risk sensitivities)
    /// </summary>
    public class Greeks
    {
        public decimal Delta { get; set; }      // Price sensitivity to underlying price
        public decimal Gamma { get; set; }      // Delta sensitivity to underlying price  
        public decimal Vega { get; set; }       // Price sensitivity to volatility
        public decimal Theta { get; set; }      // Price sensitivity to time decay
        public decimal Rho { get; set; }        // Price sensitivity to interest rate
        public decimal Lambda { get; set; }     // Leverage (elasticity)

        public Greeks(decimal delta = 0m, decimal gamma = 0m, decimal vega = 0m, 
                     decimal theta = 0m, decimal rho = 0m, decimal lambda = 0m)
        {
            Delta = delta;
            Gamma = gamma;
            Vega = vega;
            Theta = theta;
            Rho = rho;
            Lambda = lambda;
        }

        public override string ToString()
        {
            return $"Delta: {Delta:F4}, Gamma: {Gamma:F4}, Vega: {Vega:F4}, " +
                   $"Theta: {Theta:F4}, Rho: {Rho:F4}, Lambda: {Lambda:F4}";
        }
    }
}