// Null.cs

using System;

namespace Antares.Utility
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'ql/utilities/null.hpp' header.
    /// In C++, this header provides a `Null<T>()` utility to generate a "magic number"
    /// (e.g., float.MaxValue) representing an uninitialized or null state for primitive types.
    /// <para>
    /// This pattern is superseded in C# by built-in language features for nullability.
    /// This class is marked as obsolete to guide developers towards the idiomatic C# equivalents.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>C++ `Null<T>()` where T is a value type (e.g., `Real`, `Integer`)</term>
    ///     <description>
    ///       Use a nullable value type (`T?`) and the `null` keyword.
    ///       For example, `Real x = Null<Real>();` becomes `double? x = null;`.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `if (x == Null<Real>())`</term>
    ///     <description>
    ///       Use a standard null check.
    ///       For example, `if (x == null)` or `if (!x.HasValue)`.
    ///     </description>
    ///   </item>
    ///    <item>
    ///     <term>C++ `Null<T>()` where T is a class type</term>
    ///     <description>
    ///       Use the standard `null` keyword, as reference types are inherently nullable.
    ///     </description>
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