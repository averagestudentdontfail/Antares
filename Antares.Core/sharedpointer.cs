// C# code for SharedPtr.cs

using System;

namespace Antares.Ext
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'quantlib/shared_ptr.hpp' header.
    /// In C++, that header provides compatibility aliases for smart pointer utilities
    /// like shared_ptr, weak_ptr, and various pointer casts.
    ///
    /// In C#, these concepts are handled directly by the .NET runtime, the Garbage Collector (GC),
    /// and built-in language features. There is no need for a compatibility layer or smart pointer classes.
    /// This class is marked as obsolete to guide developers towards the idiomatic C# equivalents.
    ///
    /// <para>Common C++ smart pointer patterns and their C# equivalents:</para>
    /// - C++ shared_ptr&lt;T&gt;: Use a direct reference to a class instance (e.g., <c>MyClass obj = new MyClass();</c>). The .NET GC manages object lifetime.
    /// - C++ weak_ptr&lt;T&gt;: Use <see cref="System.WeakReference{T}"/> to hold a reference without preventing garbage collection.
    /// - C++ make_shared&lt;T&gt;(...): Use the <c>new</c> operator (e.g., <c>new MyClass(...)</c>).
    /// - C++ static_pointer_cast&lt;T&gt;(ptr): Use a C# explicit cast (e.g., <c>(DerivedType)baseReference</c>), which throws <see cref="InvalidCastException"/> if invalid.
    /// - C++ dynamic_pointer_cast&lt;T&gt;(ptr): Use the C# <c>as</c> operator (e.g., <c>baseReference as DerivedType</c>), which returns null if invalid.
    /// - C++ enable_shared_from_this&lt;T&gt;: Not needed in C#; use <c>this</c> to pass a reference to self.
    /// </summary>
    [Obsolete("In C#, use direct object references, the 'new' keyword, and built-in casting operators. Smart pointers are not needed due to the Garbage Collector. This class is for documentation purposes only.", error: true)]
    public static class SharedPtr
    {
        // This class is intentionally empty. Its sole purpose is to document the C#
        // equivalents of C++ smart pointer features and prevent its usage.
    }
}