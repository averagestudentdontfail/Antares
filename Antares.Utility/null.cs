// Null.cs

using System;

namespace Antares.Utility
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'ql/utilities/null.hpp' header.
    /// In C++, this header provides a <c>Null&lt;T&gt;()</c> utility to generate a "magic number"
    /// (e.g., float.MaxValue) representing an uninitialized or null state for primitive types.
    /// <para>
    /// This pattern is superseded in C# by built-in language features for nullability.
    /// This class is marked as obsolete to guide developers towards the idiomatic C# equivalents.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <description>C++ <c>Null&lt;T&gt;()</c> where T is a value type (e.g., <c>Real</c>, <c>Integer</c>): Use a nullable value type (<c>T?</c>) and the <c>null</c> keyword. For example, <c>Real x = Null&lt;Real&gt;();</c> becomes <c>double? x = null;</c>.</description>
    ///   </item>
    ///   <item>
    ///     <description>C++ <c>if (x == Null&lt;Real&gt;())</c>: Use a standard null check. For example, <c>if (x == null)</c> or <c>if (!x.HasValue)</c>.</description>
    ///   </item>
    ///   <item>
    ///     <description>C++ <c>Null&lt;T&gt;()</c> where T is a class type: Use the standard <c>null</c> keyword, as reference types are inherently nullable.</description>
    ///   </item>
    /// </list>
    /// </summary>
    [Obsolete("In C#, use nullable types (e.g., 'double?') and the 'null' keyword instead of the Null<T> pattern. This class is for documentation purposes only.", error: true)]
    public static class Null
    {
        // This class is intentionally empty. Its sole purpose is to document the C#
        // equivalents of the C++ Null<T> pattern and prevent its usage.
    }
}