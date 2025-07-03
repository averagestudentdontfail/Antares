// Variants.cs

using System;

namespace Antares.Utility
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'ql/utilities/variants.hpp' header.
    /// In C++, this header provides a variant_visitor helper struct to simplify using std::visit with std::variant.
    /// <para>
    /// This entire pattern is a C++-specific language feature for handling discriminated unions.
    /// In C#, this problem is solved differently and more idiomatically using built-in language features, making a direct port of variant_visitor unnecessary.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>C++ std::variant</term>
    ///     <description>
    ///       The C# equivalent of a variable that can hold one of several types is the universal base class <see cref="System.Object"/>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ std::visit with variant_visitor</term>
    ///     <description>
    ///       The C# equivalent is a switch statement with type-pattern matching.
    ///       This allows for clean, type-safe dispatch based on the runtime type of an object.
    ///       <example>
    ///       <code>
    ///       // C++ code:
    ///       // std::variant<int, string> v = "hello";
    ///       // std::visit(variant_visitor{
    ///       //     [](int i) { /* handle int */ },
    ///       //     [](const string& s) { /* handle string */ }
    ///       // }, v);
    ///
    ///       // C# equivalent:
    ///       object v = "hello";
    ///       switch (v)
    ///       {
    ///           case int i:
    ///               // handle int
    ///               break;
    ///           case string s:
    ///               // handle string
    ///               break;
    ///       }
    ///       </code>
    ///       </example>
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    [Obsolete("The C++ variant/visitor pattern is replaced by using System.Object and type-pattern matching (e.g., 'switch (obj) { case int i: ... }') in C#. This class is for documentation only.", error: true)]
    public static class Variants
    {
        // This class is intentionally empty.
    }
}