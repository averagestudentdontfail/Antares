// C# code for Config.cs

using System;

namespace Antares.Ext
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ configuration files
    /// (config.hpp, config.msvc.hpp, config.ansi.hpp, etc.).
    /// <para>
    /// In C++, these headers are used for platform- and compiler-specific settings,
    /// such as disabling certain warnings, defining platform-specific macros, or
    /// handling compiler bugs.
    /// </para>
    /// <para>
    /// This entire mechanism is unnecessary in C#. The .NET runtime abstracts away
    /// platform differences, and compiler settings are managed through project files
    /// and C# language features (like `#pragma warning`). There is no need for a
    /// direct translation of these files.
    /// </para>
    /// <para>
    /// The primary functional purpose of these files was to include `userconfig.hpp`,
    /// which has already been ported to `Antares.Settings` in `userconfig.cs`.
    /// </para>
    /// <para>
    /// This class is marked as obsolete to prevent any use and to document that
    /// platform-specific configuration is handled differently in the .NET ecosystem.
    /// </para>
    /// </summary>
    [Obsolete("Platform-specific configuration is not needed in C#. This class is a placeholder for documentation purposes only.", error: true)]
    public static class Config
    {
        // This class is intentionally empty.
    }
}