// C# code for Functional.cs

using System;
using System.Linq.Expressions;

namespace Antares.Ext
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'ql/functional.hpp' header.
    /// In C++, that header provides compatibility aliases for functional programming utilities
    /// like `std::function`, `std::bind`, and `std::placeholders`, all marked as deprecated.
    /// <para>
    /// In C#, these features are fundamental language and Base Class Library (BCL) constructs.
    /// There is no need for a compatibility layer. This class is marked as obsolete to guide
    /// developers towards the idiomatic C# equivalents.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>C++ `std::function`</term>
    ///     <description>
    ///       Use the built-in delegate types <see cref="System.Func{TResult}"/> or <see cref="System.Action"/>.
    ///       For example, `std::function<double(double, int)>` becomes `Func<double, int, double>`.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `std::bind` and `std::placeholders`</term>
    ///     <description>
    ///       Use C# lambda expressions to create closures. This is the modern, type-safe, and more readable
    ///       equivalent of binding arguments. For example:
    ///       <code>
    ///       // C++: auto bound_func = std::bind(my_func, 10, std::placeholders::_1);
    ///       // C#: Func<ArgType, ReturnType> boundFunc = (arg) => MyFunc(10, arg);
    ///       </code>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `std::ref` and `std::cref`</term>
    ///     <description>
    ///       These are used in C++ to pass arguments by reference to `std::bind`. In C#, this is handled
    ///       natively by the language. Methods can accept parameters by reference using the `ref` or `in` keywords.
    ///       Lambda expressions also capture the variable's reference, not its value, achieving a similar effect.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    [Obsolete("In C#, use built-in delegates (Func<>, Action<>), lambda expressions, and 'ref' parameters directly. This class is for documentation purposes only.", error: true)]
    public static class Functional
    {
        // This class is intentionally empty. Its sole purpose is to document the C#
        // equivalents of C++ functional features and prevent its usage.
    }
}