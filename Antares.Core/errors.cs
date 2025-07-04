// Errors.cs

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace Antares
{
    /// <summary>
    /// Base exception class for the Antares library, corresponding to QuantLib::Error.
    /// </summary>
    /// <remarks>
    /// This exception is intended to be thrown by the helper methods in the static QL class,
    /// which automatically capture file, line, and function information.
    /// </remarks>
    public class AntaresException : Exception
    {
        /// <summary>
        /// Creates a new AntaresException with a formatted message including file, line, and function details.
        /// This constructor is primarily for use by the QL helper class.
        /// </summary>
        public AntaresException(string file, int line, string function, string message)
            : base(Format(file, line, function, message))
        {
        }

        // Standard constructors for general use
        public AntaresException() : base() { }
        public AntaresException(string message) : base(message) { }
        public AntaresException(string message, Exception innerException) : base(message, innerException) { }

        private static string Format(string file, int line, string function, string message)
        {
            var sb = new StringBuilder();
            sb.Append(message);

            if (!string.IsNullOrEmpty(function) && function != "(unknown)")
                sb.Append($"\n   in function {function}");

            if (!string.IsNullOrEmpty(file))
                sb.Append($"\n   at {Path.GetFileName(file)}:{line}");

            return sb.ToString();
        }
    }

    /// <summary>
    /// Provides static helper methods for assertion and error handling,
    /// replacing the C++ QL_FAIL, QL_REQUIRE, QL_ASSERT, and QL_ENSURE macros.
    /// </summary>
    public static class QL
    {
        /// <summary>
        /// Throws an AntaresException unconditionally. Replaces the QL_FAIL macro.
        /// </summary>
        /// <param name="message">The error message. Use C# interpolated strings for formatting.</param>
        /// <param name="file">The source file path (automatically captured).</param>
        /// <param name="line">The source line number (automatically captured).</param>
        /// <param name="function">The calling member name (automatically captured).</param>
        [System.Diagnostics.DebuggerStepThrough]
        public static void Fail(string message,
                                [CallerFilePath] string file = "",
                                [CallerLineNumber] int line = 0,
                                [CallerMemberName] string function = "")
        {
            throw new AntaresException(file, line, function, message);
        }

        /// <summary>
        /// Throws an AntaresException if the given condition is not met.
        /// This is a general-purpose assertion. Replaces the QL_ASSERT macro.
        /// </summary>
        /// <param name="condition">The condition to check. An exception is thrown if it is false.</param>
        /// <param name="message">The error message. Use C# interpolated strings for formatting.</param>
        /// <param name="file">The source file path (automatically captured).</param>
        /// <param name="line">The source line number (automatically captured).</param>
        /// <param name="function">The calling member name (automatically captured).</param>
        [System.Diagnostics.DebuggerStepThrough]
        public static void Assert(bool condition, string message,
                                  [CallerFilePath] string file = "",
                                  [CallerLineNumber] int line = 0,
                                  [CallerMemberName] string function = "")
        {
            if (!condition)
                throw new AntaresException(file, line, function, message);
        }

        /// <summary>
        /// Throws an AntaresException if the given pre-condition is not met.
        /// Replaces the QL_REQUIRE macro.
        /// </summary>
        /// <param name="condition">The pre-condition to check. An exception is thrown if it is false.</param>
        /// <param name="message">The error message. Use C# interpolated strings for formatting.</param>
        /// <param name="file">The source file path (automatically captured).</param>
        /// <param name="line">The source line number (automatically captured).</param>
        /// <param name="function">The calling member name (automatically captured).</param>
        [System.Diagnostics.DebuggerStepThrough]
        public static void Require(bool condition, string message,
                                   [CallerFilePath] string file = "",
                                   [CallerLineNumber] int line = 0,
                                   [CallerMemberName] string function = "")
        {
            if (!condition)
                throw new AntaresException(file, line, function, message);
        }

        /// <summary>
        /// Throws an AntaresException if the given post-condition is not met.
        /// Replaces the QL_ENSURE macro.
        /// </summary>
        /// <param name="condition">The post-condition to check. An exception is thrown if it is false.</param>
        /// <param name="message">The error message. Use C# interpolated strings for formatting.</param>
        /// <param name="file">The source file path (automatically captured).</param>
        /// <param name="line">The source line number (automatically captured).</param>
        /// <param name="function">The calling member name (automatically captured).</param>
        [System.Diagnostics.DebuggerStepThrough]
        public static void Ensure(bool condition, string message,
                                  [CallerFilePath] string file = "",
                                  [CallerLineNumber] int line = 0,
                                  [CallerMemberName] string function = "")
        {
            if (!condition)
                throw new AntaresException(file, line, function, message);
        }
    }
}