// Optional.cs

using System;

namespace Antares.Ext
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'quantlib/optional.hpp' header.
    /// In C++, that header provides a compatibility alias for an optional/nullable type (either std::optional or boost::optional).
    /// <para>
    /// In C#, this concept is a fundamental language feature and does not require a special class. This class is marked as obsolete to guide developers towards the idiomatic C# equivalents.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>C++ optional&lt;T&gt; where T is a value type (e.g., double, int)</term>
    ///     <description>
    ///       Use a nullable value type, System.Nullable&lt;T&gt;, with the C# shorthand T?. For example, optional&lt;double&gt; becomes double?.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ optional&lt;T&gt; where T is a reference type (e.g., MyClass*)</term>
    ///     <description>
    ///       Use a standard reference type, which is inherently nullable in C#. For example, optional&lt;MyClass&gt; becomes a reference of type MyClass which can be assigned null. With C# 8+ and nullable reference types enabled, this would be MyClass?.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ std::nullopt or boost::none</term>
    ///     <description>
    ///       Use the null keyword. For example: double? myOptionalDouble = null;.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    [Obsolete("In C#, use nullable types (e.g., 'double?') for value types and standard nullable references for classes. The 'null' keyword replaces 'nullopt'. Smart-wrapper types are not needed.", error: true)]
    public static class Optional
    {
        // This class is intentionally empty. Its sole purpose is to document the C# equivalents of C++ optional types and prevent its usage.
    }
}