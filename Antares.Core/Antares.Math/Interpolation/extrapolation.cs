// C# code for Extrapolation.cs

namespace Antares.Math.Interpolation
{
    /// <summary>
    /// Base contract for classes that can perform extrapolation.
    /// </summary>
    /// <remarks>
    /// In the original C++ library, this was a base class. In C#, it has been
    /// translated to an interface to allow for more flexible designs, as C#
    /// does not support multiple class inheritance. Classes that can extrapolate,
    /// such as interpolations or term structures, should implement this interface.
    /// </remarks>
    public interface IExtrapolator
    {
        /// <summary>
        /// Gets a value indicating whether extrapolation is enabled.
        /// Corresponds to the C++ `allowsExtrapolation()` method.
        /// </summary>
        bool AllowsExtrapolation { get; }

        /// <summary>
        /// Enables or disables extrapolation for subsequent calculations.
        /// </summary>
        /// <remarks>
        /// This method replaces the C++ `enableExtrapolation(bool)` and
        /// `disableExtrapolation(bool)` methods.
        /// <list type="bullet">
        ///   <item><c>EnableExtrapolation(true)</c> is equivalent to C++ <c>enableExtrapolation(true)</c>.</item>
        ///   <item><c>EnableExtrapolation(false)</c> is equivalent to C++ <c>enableExtrapolation(false)</c> or <c>disableExtrapolation(true)</c>.</item>
        /// </list>
        /// </remarks>
        /// <param name="value">True to enable extrapolation; false to disable it.</param>
        void EnableExtrapolation(bool value);
    }
}