// C# code for Functional.cs

using System;
using System.Linq.Expressions;

namespace Antares.Ext
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'ql/functional.hpp' header.
    /// In C++, that header provides compatibility aliases for functional programming utilities
    /// like <c>std::function</c>, <c>std::bind</c>, and <c>std::placeholders</c>, all marked as deprecated.
    /// <para>
    /// In C#, these features are fundamental language and Base Class Library (BCL) constructs.
    /// There is no need for a compatibility layer. This class is marked as obsolete to guide
    /// developers towards the idiomatic C# equivalents.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>C++ <c>std::function</c></term>
    ///     <description>
    ///       Use the built-in delegate types <see cref="System.Func{TResult}"/> or <see cref="System.Action"/>.
    ///       For example, <c>std::function&lt;double(double, int)&gt;</c> becomes <c>Func&lt;double, int, double&gt;</c>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ <c>std::bind</c> and <c>std::placeholders</c></term>
    ///     <description>
    ///       Use C# lambda expressions to create closures. This is the modern, type-safe, and more readable
    ///       equivalent of binding arguments. For example:
    ///       <para>
    ///       <c>// C++: auto bound_func = std::bind(my_func, 10, std::placeholders::_1);</c>
    ///       <c>// C#: Func&lt;ArgType, ReturnType&gt; boundFunc = (arg) =&gt; MyFunc(10, arg);</c>
    ///       </para>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ <c>std::ref</c> and <c>std::cref</c></term>
    ///     <description>
    ///       These are used in C++ to pass arguments by reference to <c>std::bind</c>. In C#, this is handled
    ///       natively by the language. Methods can accept parameters by reference using the <c>ref</c> or <c>in</c> keywords.
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