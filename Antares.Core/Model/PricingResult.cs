using System;

namespace Antares.Model
{
    /// <summary>
    /// Contains the result of option pricing calculation
    /// </summary>
    public class PricingResult
    {
        public decimal TheoreticalPrice { get; set; }
        public Greeks Greeks { get; set; }
        public decimal IntrinsicValue { get; set; }
        public decimal TimeValue { get; set; }
        public string PricingModel { get; set; } = string.Empty;
        public TimeSpan CalculationTime { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }

        public PricingResult(decimal price, Greeks greeks, string model = "Unknown")
        {
            TheoreticalPrice = price;
            Greeks = greeks;
            PricingModel = model;
            TimeValue = Math.Max(0, price - IntrinsicValue);
        }

        public PricingResult(string errorMessage)
        {
            IsSuccess = false;
            ErrorMessage = errorMessage;
            Greeks = new Greeks();
        }

        public void CalculateIntrinsicValue(OptionContract contract, decimal underlyingPrice)
        {
            IntrinsicValue = contract.Right == OptionRight.Call
                ? Math.Max(0, underlyingPrice - contract.Strike)
                : Math.Max(0, contract.Strike - underlyingPrice);
            
            TimeValue = Math.Max(0, TheoreticalPrice - IntrinsicValue);
        }

        public override string ToString()
        {
            if (!IsSuccess)
                return $"Error: {ErrorMessage}";

            return $"Price: {TheoreticalPrice:C}, Intrinsic: {IntrinsicValue:C}, " +
                   $"Time Value: {TimeValue:C}, Model: {PricingModel}";
        }
    }
}