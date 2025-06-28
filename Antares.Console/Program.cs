using System;
using Antares;
using Antares.Model;

class Program
{
    static void Main()
    {
        Console.WriteLine("Antares Derivative Pricing Engine Test");
        Console.WriteLine("=================================================");
        Console.WriteLine();

        var calculator = new Calculator();

        Console.WriteLine("Test 1: Original Parameters");
        Console.WriteLine("---------------------------");
        TestContract(calculator, 
            strike: 150m, spot: 145m, vol: 0.25, r: 0.05, q: 0.02, days: 30);
        
        Console.WriteLine();
        
        Console.WriteLine("Test 2: Deep ITM Put");
        Console.WriteLine("--------------------");
        TestContract(calculator, 
            strike: 100m, spot: 70m, vol: 0.20, r: 0.05, q: 0.15, days: 90);
        
        Console.WriteLine();
        
        Console.WriteLine("Test 3: Very Deep ITM, Low Vol");
        Console.WriteLine("-------------------------------");
        TestContract(calculator, 
            strike: 100m, spot: 60m, vol: 0.10, r: 0.06, q: 0.20, days: 180);
        
        Console.WriteLine();
        
        Console.WriteLine("Test 4: Deep ITM Put with r > q");
        Console.WriteLine("--------------------------------");
        TestContract(calculator, 
            strike: 100m, spot: 70m, vol: 0.20, r: 0.15, q: 0.05, days: 90);

        Console.WriteLine();
        Console.WriteLine("Analysis Complete! Press any key to exit...");
        Console.ReadKey();
    }
    
    static void TestContract(Calculator calc, decimal strike, decimal spot,
                           double vol, double r, double q, int days)
    {
        var contract = new OptionContract
        {
            Symbol = "TEST",
            Right = OptionRight.Put,
            Strike = strike,
            Expiry = DateTime.Now.AddDays(days),
            Style = OptionStyle.American
        };

        var marketData = new MarketData(spot, vol, r, q);

        Console.WriteLine($"Parameters: S=${spot}, K=${strike}, T={days}d, vol={vol:P0}, r={r:P0}, q={q:P0}");
        Console.WriteLine($"Moneyness: {(spot / strike - 1m):P1} ({(spot < strike ? "ITM" : "OTM")})");

        var americanResult = calc.Price(contract, marketData, "American");

        contract.Style = OptionStyle.European;
        var europeanResult = calc.Price(contract, marketData, "European");

        if (americanResult.IsSuccess && europeanResult.IsSuccess)
        {
            decimal intrinsic = Math.Max(0, strike - spot);
            decimal earlyExercisePremium = americanResult.TheoreticalPrice - europeanResult.TheoreticalPrice;

            Console.WriteLine($"  Intrinsic Value:        {intrinsic:C}");
            Console.WriteLine($"  European Price:         {europeanResult.TheoreticalPrice:C}");
            Console.WriteLine($"  American Price:         {americanResult.TheoreticalPrice:C}");
            Console.WriteLine($"  Early Exercise Premium: {earlyExercisePremium:C} ({earlyExercisePremium / europeanResult.TheoreticalPrice:P1})");
            Console.WriteLine($"  Calculation Time:       {americanResult.CalculationTime.TotalMilliseconds:F1}ms");
            Console.WriteLine($"  Delta: {americanResult.Greeks.Delta:F3}, Gamma: {americanResult.Greeks.Gamma:F5}");
        }
        else
        {
            Console.WriteLine($"  ERROR: {americanResult.ErrorMessage ?? europeanResult.ErrorMessage}");
        }
    }
}