// C# code for DividendSchedule.cs

using Antares.Cashflow;
using System.Collections.Generic;

// The C++ header defines `DividendSchedule` as a `std::vector` of shared pointers to `Dividend`.
// In C#, the idiomatic equivalent is a list or read-only list of interfaces.
// A global `using` alias provides a convenient way to refer to this type,
// mirroring the C++ `typedef`.
global using DividendSchedule = System.Collections.Generic.IReadOnlyList<Antares.Cashflow.IDividend>;

namespace Antares.Instrument
{
    /// <summary>
    /// This class is a placeholder corresponding to the C++ 'ql/instruments/dividendschedule.hpp' header.
    /// <para>
    /// The original header defines <c>DividendSchedule</c> as a type alias (typedef) for a
    /// <c>std::vector</c> of <c>Dividend</c> smart pointers. In C#, this concept is best represented
    /// by a collection interface, and we use a <c>global using</c> directive to create the alias.
    /// </para>
    /// <para>
    /// See the <c>global using</c> directive at the top of this file.
    /// </para>
    /// </summary>
    public static class DividendScheduleType
    {
        // This class is intentionally empty. Its purpose is to provide a home for the documentation
        // of the file-level global using statement.
    }
}