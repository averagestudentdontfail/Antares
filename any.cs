// Any.cs

using System;

namespace Antares.Ext
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'ql/any.hpp' header.
    /// In C++, that header provides a compatibility alias for a type-safe container for single values of any type (either std::any or boost::any).
    /// <para>
    /// In C#, the universal base class <see cref="System.Object"/> serves this purpose.
    /// There is no need for a special wrapper class. This class is marked as obsolete to guide developers towards the idiomatic C# equivalents.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>C++ std::any or boost::any</term>
    ///     <description>
    ///       Use the built-in <see cref="System.Object"/> type. A variable of type object can hold a value of any type. Value types will be boxed.
    ///       <code>
    ///       // C++: std::any myAny = 10.0;
    ///       // C#: object myAny = 10.0;
    ///       </code>
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ any_cast<T></term>
    ///     <description>
    ///       Use a standard C# cast (T). This will throw an <see cref="InvalidCastException"/> if the underlying type is not convertible to T.
    ///       <code>
    ///       // C++: double value = std::any_cast<double>(myAny);
    ///       // C#: double value = (double)myAny;
    ///       </code>
    ///       For a non-throwing cast with reference types, use the as operator, which returns null on failure.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    [Obsolete("In C#, use System.Object and standard casting operators '(T)' or 'as'. A special 'any' type is not needed.", error: true)]
    public static class Any
    {
        // This class is intentionally empty. Its sole purpose is to document the C# equivalents of C++'s 'any' type and prevent its usage.
    }
}