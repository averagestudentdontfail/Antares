// Tuple.cs

using System;

namespace Antares.Ext
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'quantlib/tuple.hpp' header.
    /// In C++, that header provides compatibility aliases for std::tuple and its helpers
    /// (make_tuple, get, tie), all of which are marked as deprecated.
    /// <para>
    /// In C#, these features are built directly into the language and the Base Class Library (BCL).
    /// There is no need for a compatibility layer. This class is marked as obsolete to guide
    /// developers towards the idiomatic C# equivalents.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>C++ `std::tuple` or `QuantLib::ext::tuple`</term>
    ///     <description>
    ///       Use <see cref="System.ValueTuple"/> or C# tuple literals, e.g.,
    ///       <code>var t = (1, "text");</code>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `std::make_tuple`</term>
    ///     <description>
    ///       Use tuple literals directly, e.g.,
    ///       <code>var t = (1, "text");</code> is the equivalent of <code>auto t = std::make_tuple(1, "text");</code>.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `std::get`</term>
    ///     <description>
    ///       Access tuple elements by their property names, e.g.,
    ///       <code>t.Item1</code>, <code>t.Item2</code>, etc.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `std::tie` or structured bindings</term>
    ///     <description>
    ///       Use C# deconstruction, e.g.,
    ///       <code>(int myInt, string myString) = t;</code>.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    [Obsolete("In C#, use System.ValueTuple and its language features (literals, deconstruction) directly. This class is for documentation purposes only and provides no functionality.", error: true)]
    public static class Tuple
    {
        // This class is intentionally empty. Its sole purpose is to document the C#
        // equivalent of the deprecated C++ tuple helpers and prevent its usage.
    }
}