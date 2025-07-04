// Types.cs

global using Integer = System.Int32;
global using BigInteger = System.Int64;
global using Natural = System.UInt32;
global using BigNatural = System.UInt64;
global using Real = System.Double;
global using Decimal = System.Double; // Note: This is an alias for Real, not C#'s System.Decimal.
global using Size = System.Int32;
global using Time = System.Double;
global using DiscountFactor = System.Double;
global using Rate = System.Double;
global using Spread = System.Double;
global using Volatility = System.Double;
global using Probability = System.Double;

namespace Antares
{
    /// <summary>
    /// This class serves as a documentation placeholder for the global type aliases
    /// defined at the top of this file. These aliases are used throughout the
    /// Antares library to maintain consistency with the original QuantLib C++ codebase.
    /// </summary>
    /// <remarks>
    /// The actual type aliases are defined using C# 10's `global using` feature.
    /// This makes the aliases available project-wide, providing semantic meaning
    /// to primitive types (e.g., using `Rate` instead of `double` for an interest rate).
    /// <br/><br/>
    /// <b>Type Mappings:</b>
    /// <list type="table">
    ///   <listheader>
    ///     <term>C++ Type</term>
    ///     <description>.NET Type</description>
    ///   </listheader>
    ///   <item><term>Real, Decimal, Time, DiscountFactor, Rate, Spread, Volatility, Probability</term><description>System.Double (aliased as Real, etc.)</description></item>
    ///   <item><term>Integer</term><description>System.Int32 (aliased as Integer)</description></item>
    ///   <item><term>BigInteger</term><description>System.Int64 (aliased as BigInteger)</description></item>
    ///   <item><term>Natural</term><description>System.UInt32 (aliased as Natural)</description></item>
    ///   <item><term>BigNatural</term><description>System.UInt64 (aliased as BigNatural)</description></item>
    ///   <item><term>Size (from std::size_t)</term><description>System.Int32 (aliased as Size)</description></item>
    /// </list>
    /// </remarks>
    public static class TypeAliases
    {
        // This class is intentionally empty. Its purpose is to provide a home for the documentation
        // of the file-level global using statements.
    }
}