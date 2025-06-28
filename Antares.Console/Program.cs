using System;
using Antares;
using Antares.Model;

class Program
{
    static void Main()
    {
        Console.WriteLine("Antares Rigorous Derivative Pricing Engine Test");
        Console.WriteLine("==========================================================");
        Console.WriteLine("Mathematical Framework: Spectral Collocation with Enhanced Validation");
        Console.WriteLine();

        var calculator = new Calculator();

        // Enable diagnostic mode for detailed analysis
        bool enableDiagnostics = true;
        
        Console.WriteLine("Configuration: Rigorous mode with full mathematical validation");
        Console.WriteLine($"Diagnostics: {(enableDiagnostics ? "ENABLED" : "DISABLED")}");
        Console.WriteLine();

        // Test 1: Original test case
        Console.WriteLine("Test 1: Original Parameters (Expected: Moderate Early Exercise)");
        Console.WriteLine("--------------------------------------------------------------");
        TestContractRigorous(calculator, 
            strike: 150m, spot: 145m, vol: 0.25, r: 0.05, q: 0.02, days: 30, enableDiagnostics);
        
        WaitForUser();
        
        // Test 2: Deep ITM put with early exercise value
        Console.WriteLine("Test 2: Deep ITM Put (Expected: Significant Early Exercise Premium)");
        Console.WriteLine("-------------------------------------------------------------------");
        TestContractRigorous(calculator, 
            strike: 100m, spot: 70m, vol: 0.20, r: 0.05, q: 0.15, days: 90, enableDiagnostics);
        
        WaitForUser();
        
        // Test 3: Very deep ITM, low vol (maximum early exercise)
        Console.WriteLine("Test 3: Very Deep ITM, Low Vol (Expected: Maximum Early Exercise)");
        Console.WriteLine("-----------------------------------------------------------------");
        TestContractRigorous(calculator, 
            strike: 100m, spot: 60m, vol: 0.10, r: 0.06, q: 0.20, days: 180, enableDiagnostics);
        
        WaitForUser();
        
        // Test 4: Deep ITM Put with r > q (Expected Early Exercise Premium)
        Console.WriteLine("Test 4: Deep ITM Put with r > q (Expected: Strong Early Exercise)");
        Console.WriteLine("-----------------------------------------------------------------");
        TestContractRigorous(calculator, 
            strike: 100m, spot: 70m, vol: 0.20, r: 0.15, q: 0.05, days: 90, enableDiagnostics);

        Console.WriteLine();
        Console.WriteLine("==========================================================");
        Console.WriteLine("Rigorous Analysis Complete!");
        Console.WriteLine("Expected vs Actual Results Comparison:");
        Console.WriteLine("  Test 1: Should show ~$0.70+ early exercise premium");
        Console.WriteLine("  Test 2: Should show ~$0.50+ early exercise premium"); 
        Console.WriteLine("  Test 3: Should show ~$1.00+ early exercise premium");
        Console.WriteLine("  Test 4: Should show ~$2.75+ early exercise premium");
        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
    
    static void TestContractRigorous(Calculator calc, decimal strike, decimal spot,
                                   double vol, double r, double q, int days, bool enableDiagnostics = true)
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
        Console.WriteLine($"Moneyness: {(spot / strike - 1):P1} ({(spot < strike ? "ITM" : "OTM")})");
        
        // Calculate theoretical expectations
        double xmax = CalculateExpectedXMax(strike, r, q);
        bool shouldHaveEarlyExercise = ShouldHaveEarlyExercise(spot, strike, r, q, vol, days);
        
        Console.WriteLine($"Theoretical XMax: {xmax:F2}");
        Console.WriteLine($"Expected early exercise: {(shouldHaveEarlyExercise ? "YES" : "NO")}");
        Console.WriteLine();

        if (enableDiagnostics)
        {
            Console.WriteLine(">>> DIAGNOSTIC MODE: Detailed mathematical analysis follows <<<");
        }

        // Price American option with rigorous engine
        var americanResult = calc.Price(contract, marketData, "American");

        // Price European for comparison
        contract.Style = OptionStyle.European;
        var europeanResult = calc.Price(contract, marketData, "European");

        if (americanResult.IsSuccess && europeanResult.IsSuccess)
        {
            decimal intrinsic = Math.Max(0, strike - spot);
            decimal earlyExercisePremium = americanResult.TheoreticalPrice - europeanResult.TheoreticalPrice;
            decimal premiumPercent = europeanResult.TheoreticalPrice > 0 ? 
                (earlyExercisePremium / europeanResult.TheoreticalPrice) * 100 : 0;

            Console.WriteLine("RESULTS:");
            Console.WriteLine($"  Intrinsic Value:        {intrinsic:C}");
            Console.WriteLine($"  European Price:         {europeanResult.TheoreticalPrice:C}");
            Console.WriteLine($"  American Price:         {americanResult.TheoreticalPrice:C}");
            Console.WriteLine($"  Early Exercise Premium: {earlyExercisePremium:C} ({premiumPercent:F2}%)");
            Console.WriteLine($"  Calculation Time:       {americanResult.CalculationTime.TotalMilliseconds:F1}ms");
            Console.WriteLine($"  Greeks: Δ={americanResult.Greeks.Delta:F3}, Γ={americanResult.Greeks.Gamma:F5}, ν={americanResult.Greeks.Vega:F4}");
            
            // Enhanced result validation
            Console.WriteLine();
            Console.WriteLine("VALIDATION:");
            var validation = ValidateResult(americanResult, europeanResult, intrinsic, shouldHaveEarlyExercise);
            Console.WriteLine($"  Mathematical Consistency: {(validation.isConsistent ? "PASS" : "FAIL")}");
            Console.WriteLine($"  Economic Reasonableness:  {(validation.isReasonable ? "PASS" : "FAIL")}");
            Console.WriteLine($"  Early Exercise Logic:     {(validation.earlyExerciseCorrect ? "PASS" : "FAIL")}");
            
            if (!validation.isConsistent || !validation.isReasonable || !validation.earlyExerciseCorrect)
            {
                Console.WriteLine($"  Issues: {validation.issues}");
            }
        }
        else
        {
            Console.WriteLine($"  ERROR: {americanResult.ErrorMessage ?? europeanResult.ErrorMessage}");
        }
        
        Console.WriteLine();
    }

    static double CalculateExpectedXMax(decimal K, double r, double q)
    {
        double strike = (double)K;
        if (Math.Abs(q) < 1e-12)
        {
            return r > 0.0 ? 0.0 : strike;
        }
        
        if (q > 0.0)
        {
            double ratio = r / q;
            return strike * Math.Min(1.0, Math.Max(0.05, ratio)); // With stability adjustment
        }
        
        return strike;
    }

    static bool ShouldHaveEarlyExercise(decimal S, decimal K, double r, double q, double vol, int days)
    {
        // Basic early exercise conditions for American puts
        bool isITM = S < K;
        bool hasCarryBenefit = r > q; // Interest earned on strike > dividends lost
        bool hasReasonableTime = days > 5; // Not too close to expiry
        bool reasonableVol = vol > 0.05; // Some volatility for time value
        
        // For ITM puts with positive carry benefit, early exercise should occur
        if (isITM && hasCarryBenefit && hasReasonableTime)
            return true;
            
        // For very deep ITM puts, early exercise likely regardless of carry
        if (S / K < 0.8 && hasReasonableTime)
            return true;
            
        return false;
    }

    static (bool isConsistent, bool isReasonable, bool earlyExerciseCorrect, string issues) 
        ValidateResult(PricingResult american, PricingResult european, decimal intrinsic, bool shouldHaveEarlyExercise)
    {
        var issues = new System.Collections.Generic.List<string>();
        
        // Mathematical consistency checks
        bool isConsistent = true;
        if (american.TheoreticalPrice < european.TheoreticalPrice - 0.01m)
        {
            isConsistent = false;
            issues.Add("American < European");
        }
        
        if (american.TheoreticalPrice < intrinsic - 0.01m)
        {
            isConsistent = false;
            issues.Add("American < Intrinsic");
        }
        
        // Economic reasonableness
        bool isReasonable = true;
        if (american.TheoreticalPrice > european.TheoreticalPrice * 1.5m)
        {
            isReasonable = false;
            issues.Add("Excessive premium");
        }
        
        if (Math.Abs(american.Greeks.Delta) > 1.2m)
        {
            isReasonable = false;
            issues.Add("Delta out of bounds");
        }
        
        // Early exercise logic
        bool earlyExerciseCorrect = true;
        decimal premium = american.TheoreticalPrice - european.TheoreticalPrice;
        
        if (shouldHaveEarlyExercise && premium < 0.01m)
        {
            earlyExerciseCorrect = false;
            issues.Add("Expected early exercise premium missing");
        }
        
        return (isConsistent, isReasonable, earlyExerciseCorrect, string.Join("; ", issues));
    }

    static void WaitForUser()
    {
        Console.WriteLine("Press any key to continue to next test...");
        Console.ReadKey();
        Console.WriteLine();
    }
}