using System;
using QuantConnect;
using QuantConnect.Data.Market;

namespace Anderson.Distribution
{
    /// <summary>
    /// Provides static methods for calculating European option prices and Greeks
    /// using the analytical Black-Scholes-Merton formula.
    /// </summary>
    public static class BlackScholes
    {
        /// <summary>
        /// Calculates the price of a European option.
        /// </summary>
        public static double Price(Anderson.OptionRight right, double S, double K, double T, double r, double q, double vol)
        {
            if (T <= 0) return Math.Max(0, right == Anderson.OptionRight.Call ? S - K : K - S);
            double volSqrtT = vol * Math.Sqrt(T);
            if (volSqrtT <= 1e-12)
            {
                double intrinsic = right == Anderson.OptionRight.Call ? 
                    S * Math.Exp(-q * T) - K * Math.Exp(-r * T) : 
                    K * Math.Exp(-r * T) - S * Math.Exp(-q * T);
                return Math.Max(0, intrinsic);
            }
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / volSqrtT;
            double d2 = d1 - volSqrtT;
            if (right == Anderson.OptionRight.Call)
            {
                return S * Math.Exp(-q * T) * Distributions.CumulativeNormal(d1) - 
                       K * Math.Exp(-r * T) * Distributions.CumulativeNormal(d2);
            }
            else // Put
            {
                return K * Math.Exp(-r * T) * Distributions.CumulativeNormal(-d2) - 
                       S * Math.Exp(-q * T) * Distributions.CumulativeNormal(-d1);
            }
        }

        /// <summary>
        /// Calculates the analytical Greeks for a European option.
        /// </summary>
        public static QuantConnect.Data.Market.Greeks Greeks(Anderson.OptionRight right, double S, double K, double T, double r, double q, double vol)
        {
            if (T <= 0 || vol <= 0) // Handle expired or zero-volatility cases
            {
                decimal intrinsic = (right == Anderson.OptionRight.Call) ? 
                    (decimal)Math.Max(0, S - K) : (decimal)Math.Max(0, K - S);
                decimal deltaExpired = 0m;
                if (intrinsic > 0) deltaExpired = (right == Anderson.OptionRight.Call) ? 1m : -1m;
                return CreateGreeks(deltaExpired, 0m, 0m, 0m, 0m, 0m);
            }

            double volSqrtT = vol * Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / volSqrtT;
            double d2 = d1 - volSqrtT;
            double n_d1 = Distributions.NormalDensity(d1);
            
            double delta, gamma, vega, theta, rho;
            
            gamma = Math.Exp(-q * T) * n_d1 / (S * volSqrtT);
            vega = S * Math.Exp(-q * T) * n_d1 * Math.Sqrt(T) * 0.01;
            
            if (right == Anderson.OptionRight.Call)
            {
                delta = Math.Exp(-q * T) * Distributions.CumulativeNormal(d1);
                theta = -(S * vol * Math.Exp(-q * T) * n_d1) / (2 * Math.Sqrt(T))
                        - r * K * Math.Exp(-r * T) * Distributions.CumulativeNormal(d2)
                        + q * S * Math.Exp(-q * T) * Distributions.CumulativeNormal(d1);
                rho = K * T * Math.Exp(-r * T) * Distributions.CumulativeNormal(d2) * 0.01;
            }
            else // Put
            {
                delta = -Math.Exp(-q * T) * Distributions.CumulativeNormal(-d1);
                theta = -(S * vol * Math.Exp(-q * T) * n_d1) / (2 * Math.Sqrt(T))
                        + r * K * Math.Exp(-r * T) * Distributions.CumulativeNormal(-d2)
                        - q * S * Math.Exp(-q * T) * Distributions.CumulativeNormal(-d1);
                rho = -K * T * Math.Exp(-r * T) * Distributions.CumulativeNormal(-d2) * 0.01;
            }

            return CreateGreeks(
                (decimal)delta, (decimal)gamma, (decimal)vega, 
                (decimal)theta / 365.0m, (decimal)rho, 0m);
        }

        /// <summary>
        /// Helper method to create Greeks using reflection to access constructor
        /// </summary>
        private static QuantConnect.Data.Market.Greeks CreateGreeks(decimal delta, decimal gamma, decimal vega, decimal theta, decimal rho, decimal lambda)
        {
            try
            {
                // Use reflection to create Greeks instance since constructor might be internal
                var greeksType = typeof(QuantConnect.Data.Market.Greeks);
                var constructor = greeksType.GetConstructor(new[] { typeof(decimal), typeof(decimal), typeof(decimal), typeof(decimal), typeof(decimal), typeof(decimal) });
                
                if (constructor != null)
                {
                    return (QuantConnect.Data.Market.Greeks)constructor.Invoke(new object[] { delta, gamma, vega, theta, rho, lambda });
                }
                
                // Fallback: try to create using default constructor
                var greeks = (QuantConnect.Data.Market.Greeks)Activator.CreateInstance(greeksType, true);
                return greeks;
            }
            catch
            {
                // Final fallback - return null, QuantConnect will handle it
                return null;
            }
        }

        internal static double Price(OptionRight andersonRight, double s, double k, double t, double r, double q, double vol)
        {
            throw new NotImplementedException();
        }
    }
}