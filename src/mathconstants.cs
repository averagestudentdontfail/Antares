// Mathconstants.cs

using System;

namespace Antares
{
    /// <summary>
    /// Provides a set of common mathematical constants.
    /// This class is a C# equivalent of the C++ `ql/mathconstants.hpp` header.
    /// </summary>
    public static class MathConstants
    {
        /// <summary>
        /// The base of natural logarithms, e.
        /// Corresponds to M_E.
        /// </summary>
        public const double E = 2.71828182845904523536;

        /// <summary>
        /// The base-2 logarithm of e.
        /// Corresponds to M_LOG2E.
        /// </summary>
        public const double Log2E = 1.44269504088896340736;

        /// <summary>
        /// The base-10 logarithm of e.
        /// Corresponds to M_LOG10E and M_IVLN10.
        /// </summary>
        public const double Log10E = 0.434294481903251827651;

        /// <summary>
        /// The natural logarithm of 2.
        /// Corresponds to M_LN2.
        /// </summary>
        public const double Ln2 = 0.693147180559945309417;

        /// <summary>
        /// The natural logarithm of 10.
        /// Corresponds to M_LN10.
        /// </summary>
        public const double Ln10 = 2.30258509299404568402;

        /// <summary>
        /// The ratio of a circle's circumference to its diameter, pi.
        /// Corresponds to M_PI.
        /// </summary>
        public const double PI = 3.14159265358979323846;

        /// <summary>
        /// Two times pi.
        /// Corresponds to M_TWOPI.
        /// </summary>
        public const double TwoPi = 6.28318530717958647692; // PI * 2.0

        /// <summary>
        /// Pi divided by 2.
        /// Corresponds to M_PI_2.
        /// </summary>
        public const double PiOver2 = 1.57079632679489661923;

        /// <summary>
        /// Pi divided by 4.
        /// Corresponds to M_PI_4.
        /// </summary>
        public const double PiOver4 = 0.785398163397448309616;
        
        /// <summary>
        /// Three times pi divided by 4.
        /// Corresponds to M_3PI_4.
        /// </summary>
        public const double ThreePiOver4 = 2.3561944901923448370;

        /// <summary>
        /// The square root of pi.
        /// Corresponds to M_SQRTPI.
        /// </summary>
        public const double SqrtPi = 1.77245385090551602792981;

        /// <summary>
        /// One divided by pi.
        /// Corresponds to M_1_PI.
        /// </summary>
        public const double OneOverPi = 0.318309886183790671538;

        /// <summary>
        /// Two divided by pi.
        /// Corresponds to M_2_PI.
        /// </summary>
        public const double TwoOverPi = 0.636619772367581343076;

        /// <summary>
        /// One divided by the square root of pi.
        /// Corresponds to M_1_SQRTPI.
        /// </summary>
        public const double OneOverSqrtPi = 0.564189583547756286948;

        /// <summary>
        /// Two divided by the square root of pi.
        /// Corresponds to M_2_SQRTPI.
        /// </summary>
        public const double TwoOverSqrtPi = 1.12837916709551257390;

        /// <summary>
        /// The square root of 2.
        /// Corresponds to M_SQRT2.
        /// </summary>
        public const double Sqrt2 = 1.41421356237309504880;

        /// <summary>
        /// One divided by the square root of 2.
        /// Corresponds to M_SQRT1_2.
        /// </summary>
        public const double Sqrt1_2 = 0.70710678118654752440;

        /// <summary>
        /// The lower part of the natural logarithm of 2 for extended precision calculations.
        /// Corresponds to M_LN2LO.
        /// </summary>
        public const double Ln2Lo = 1.9082149292705877000E-10;
        
        /// <summary>
        /// The higher part of the natural logarithm of 2 for extended precision calculations.
        /// Corresponds to M_LN2HI.
        /// </summary>
        public const double Ln2Hi = 6.9314718036912381649E-1;
        
        /// <summary>
        /// The square root of 3.
        /// Corresponds to M_SQRT3.
        /// </summary>
        public const double Sqrt3 = 1.73205080756887719000;
        
        /// <summary>
        /// One divided by the natural logarithm of 2.
        /// Corresponds to M_INVLN2.
        /// </summary>
        public const double InvLn2 = 1.4426950408889633870;
    }
}