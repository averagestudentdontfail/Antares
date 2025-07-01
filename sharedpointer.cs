// C# code for SharedPtr.cs

using System;

namespace Antares.Ext
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'quantlib/shared_ptr.hpp' header.
    /// In C++, that header provides compatibility aliases for smart pointer utilities
    /// like `shared_ptr`, `weak_ptr`, and various pointer casts.
    /// <para>
    /// In C#, these concepts are handled directly by the .NET runtime, the Garbage Collector (GC),
    /// and built-in language features. There is no need for a compatibility layer or smart pointer classes.
    /// This class is marked as obsolete to guide developers towards the idiomatic C# equivalents.
    /// </para>
    /// <list type="bullet">
    ///   <item>
    ///     <term>C++ `shared_ptr<T>`</term>
    ///     <description>
    ///       Use a direct reference to a class instance (e.g., `MyClass obj = new MyClass();`).
    ///       The .NET Garbage Collector automatically manages object lifetime.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `weak_ptr<T>`</term>
    ///     <description>
    ///       Use <see cref="System.WeakReference{T}"/>. This allows you to hold a reference
    ///       to an object without preventing it from being garbage collected.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `make_shared<T>(...)`</term>
    ///     <description>
    ///       Use the `new` operator (e.g., `new MyClass(...)`). The .NET runtime handles
    ///       memory allocation efficiently.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `static_pointer_cast<T>(ptr)`</term>
    ///     <description>
    ///       Use a standard C# explicit cast (e.g., `(DerivedType)baseReference`). This will throw an
    ///       <see cref="InvalidCastException"/> at runtime if the cast is invalid.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <term>C++ `dynamic_pointer_cast<T>(ptr)`</term>
    ///     <description>
    ///       Use the C# `as` operator (e.g., `baseReference as DerivedType`). This will
    ///       return `null` if the cast is invalid, making it a safe cast.
    ///     </description>
    ///   </item>
    ///   <item>
    //     <term>C++ `enable_shared_from_this<T>`</term>
    ///     <description>
    ///       This pattern is not needed in C#. An object can simply pass a reference to itself
    ///       using the `this` keyword. The GC will manage the lifetime correctly.
    ///     </description>
    ///   </item>
    /// </list>
    /// </summary>
    [Obsolete("In C#, use direct object references, the 'new' keyword, and built-in casting operators. Smart pointers are not needed due to the Garbage Collector. This class is for documentation purposes only.", error: true)]
    public static class SharedPtr
    {
        // This class is intentionally empty. Its sole purpose is to document the C#
        // equivalents of C++ smart pointer features and prevent its usage.
    }
}