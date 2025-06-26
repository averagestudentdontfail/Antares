using System;
using Anderson;
using Anderson.Model;

class Program
{
    static void Main()
    {
        Console.WriteLine("Anderson Derivative Pricing Engine - Test");
        Console.WriteLine("=========================================");
        Console.WriteLine();

        // Create calculator
        var calculator = new Calculator();

        // Create test contract
        var contract = new OptionContract
        {
            Symbol = "AAPL",
            Right = OptionRight.Put,
            Strike = 150m,
            Expiry = DateTime.Now.AddDays(30),
            Style = OptionStyle.American
        };

        // Create market data
        var marketData = new MarketData(
            underlyingPrice: 145m,
            volatility: 0.25,
            riskFreeRate: 0.05,
            dividendYield: 0.02
        );

        Console.WriteLine($"Contract: {contract}");
        Console.WriteLine($"Market Data: {marketData}");
        Console.WriteLine($"Time to Expiry: {contract.TimeToExpiry(marketData.ValuationTime):F4} years");
        Console.WriteLine();

        // Test American engine
        Console.WriteLine("American Engine Test:");
        Console.WriteLine("--------------------");
        var americanResult = calculator.Price(contract, marketData, "American");
        PrintResult(americanResult);

        // Test European engine for comparison
        contract.Style = OptionStyle.European;
        Console.WriteLine("European Engine Test:");
        Console.WriteLine("--------------------");
        var europeanResult = calculator.Price(contract, marketData, "European");
        PrintResult(europeanResult);

        // Show early exercise premium
        if (americanResult.IsSuccess && europeanResult.IsSuccess)
        {
            Console.WriteLine("Early Exercise Analysis:");
            Console.WriteLine("------------------------");
            Console.WriteLine($"American Price:  {americanResult.TheoreticalPrice:C}");
            Console.WriteLine($"European Price:  {europeanResult.TheoreticalPrice:C}");
            Console.WriteLine($"Early Exercise Premium: {americanResult.TheoreticalPrice - europeanResult.TheoreticalPrice:C}");
        }

        Console.WriteLine();
        Console.WriteLine("Test completed. Press any key to exit...");
        Console.ReadKey();
    }

    static void PrintResult(PricingResult result)
    {
        if (result.IsSuccess)
        {
            Console.WriteLine($"  Price: {result.TheoreticalPrice:C}");
            Console.WriteLine($"  Intrinsic: {result.IntrinsicValue:C}");
            Console.WriteLine($"  Time Value: {result.TimeValue:C}");
            Console.WriteLine($"  Model: {result.PricingModel}");
            Console.WriteLine($"  Time: {result.CalculationTime.TotalMilliseconds:F1}ms");
            Console.WriteLine($"  Greeks - Delta: {result.Greeks.Delta:F4}, Gamma: {result.Greeks.Gamma:F6}, Vega: {result.Greeks.Vega:F4}");
        }
        else
        {
            Console.WriteLine($"  ERROR: {result.ErrorMessage}");
        }
        Console.WriteLine();
    }
}