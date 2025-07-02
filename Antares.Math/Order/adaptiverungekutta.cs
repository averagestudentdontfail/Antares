// C# code for AdaptiveRungeKutta.cs

using System;
using System.Collections.Generic;
using System.Numerics;

namespace Antares.Math.Order
{
    /// <summary>
    /// Runge Kutta method with adaptive stepsize.
    /// </summary>
    /// <remarks>
    /// This is a C# port of the adaptive Runge-Kutta method (Cash-Karp)
    /// as described in "Numerical Recipes in C", Chapter 16.2.
    /// The class is generic and supports `double` and `System.Numerics.Complex` types.
    /// </remarks>
    /// <typeparam name="T">The type of the dependent variable (e.g., double or Complex).</typeparam>
    public class AdaptiveRungeKutta<T>
    {
        /// <summary>
        /// Delegate for a system of ordinary differential equations.
        /// </summary>
        /// <param name="t">The independent variable (e.g., time).</param>
        /// <param name="y">The current values of the dependent variables.</param>
        /// <returns>The derivatives dy/dt at point (t, y).</returns>
        public delegate T[] OdeFunction(double t, T[] y);

        /// <summary>
        /// Delegate for a single 1-D ordinary differential equation.
        /// </summary>
        /// <param name="t">The independent variable (e.g., time).</param>
        /// <param name="y">The current value of the dependent variable.</param>
        /// <returns>The derivative dy/dt at point (t, y).</returns>
        public delegate T OdeFunction1d(double t, T y);

        // Cash-Karp parameters for embedded Runge-Kutta
        private readonly double _eps, _h1, _hmin;
        private readonly double a2 = 0.2, a3 = 0.3, a4 = 0.6, a5 = 1.0, a6 = 0.875;
        private readonly double b21 = 0.2;
        private readonly double b31 = 3.0 / 40.0, b32 = 9.0 / 40.0;
        private readonly double b41 = 0.3, b42 = -0.9, b43 = 1.2;
        private readonly double b51 = -11.0 / 54.0, b52 = 2.5, b53 = -70.0 / 27.0, b54 = 35.0 / 27.0;
        private readonly double b61 = 1631.0 / 55296.0, b62 = 175.0 / 512.0, b63 = 575.0 / 13824.0, b64 = 44275.0 / 110592.0, b65 = 253.0 / 4096.0;
        private readonly double c1 = 37.0 / 378.0, c3 = 250.0 / 621.0, c4 = 125.0 / 594.0, c6 = 512.0 / 1771.0;
        private readonly double dc1, dc3, dc4, dc5, dc6;

        // Control parameters from Numerical Recipes
        private const int ADAPTIVERK_MAXSTP = 10000;
        private const double ADAPTIVERK_TINY = 1.0e-30;
        private const double ADAPTIVERK_SAFETY = 0.9;
        private const double ADAPTIVERK_PGROW = -0.2;
        private const double ADAPTIVERK_PSHRINK = -0.25;
        private const double ADAPTIVERK_ERRCON = 1.89e-4;

        /// <summary>
        /// Initializes a new instance of the AdaptiveRungeKutta class.
        /// </summary>
        /// <param name="eps">Prescribed error for the solution.</param>
        /// <param name="h1">Initial step size.</param>
        /// <param name="hmin">Smallest step size allowed.</param>
        public AdaptiveRungeKutta(double eps = 1.0e-6, double h1 = 1.0e-4, double hmin = 0.0)
        {
            _eps = eps;
            _h1 = h1;
            _hmin = hmin;

            // Pre-calculate differences for error estimation
            dc1 = c1 - 2825.0 / 27648.0;
            dc3 = c3 - 18575.0 / 48384.0;
            dc4 = c4 - 13525.0 / 55296.0;
            dc5 = -277.0 / 14336.0;
            dc6 = c6 - 0.25;
        }

        /// <summary>
        /// Integrates a system of ODEs from x1 to x2.
        /// </summary>
        public T[] Integrate(OdeFunction ode, T[] y1, double x1, double x2)
        {
            int n = y1.Length;
            T[] y = (T[])y1.Clone();
            double[] yScale = new double[n];
            double x = x1;
            double h = _h1 * (x1 <= x2 ? 1.0 : -1.0);

            for (int nstp = 1; nstp <= ADAPTIVERK_MAXSTP; nstp++)
            {
                T[] dydx = ode(x, y);
                for (int i = 0; i < n; i++)
                    yScale[i] = Abs(y[i]) + Abs((dynamic)dydx[i] * h) + ADAPTIVERK_TINY;

                if ((x + h - x2) * (x + h - x1) > 0.0)
                    h = x2 - x;

                Rkqs(y, dydx, ref x, h, _eps, yScale, out double hdid, out double hnext, ode);

                if ((x - x2) * (x2 - x1) >= 0.0)
                    return y;

                if (System.Math.Abs(hnext) <= _hmin)
                    throw new InvalidOperationException($"Step size ({hnext}) too small ({_hmin} min) in AdaptiveRungeKutta");
                
                h = hnext;
            }
            throw new InvalidOperationException($"Too many steps ({ADAPTIVERK_MAXSTP}) in AdaptiveRungeKutta");
        }
        
        /// <summary>
        /// Integrates a single 1-D ODE from x1 to x2.
        /// </summary>
        public T Integrate(OdeFunction1d ode, T y1, double x1, double x2)
        {
            OdeFunction wrapper = (t, y) => new[] { ode(t, y[0]) };
            T[] result = Integrate(wrapper, new[] { y1 }, x1, x2);
            return result[0];
        }

        private void Rkqs(T[] y, T[] dydx, ref double x, double htry, double eps,
                          double[] yScale, out double hdid, out double hnext, OdeFunction derivs)
        {
            int n = y.Length;
            T[] yerr = new T[n];
            T[] ytemp = new T[n];
            double h = htry;

            while (true)
            {
                Rkck(y, dydx, x, h, ytemp, yerr, derivs);
                double errmax = 0.0;
                for (int i = 0; i < n; i++)
                    errmax = System.Math.Max(errmax, Abs((dynamic)yerr[i] / yScale[i]));
                
                errmax /= eps;

                if (errmax > 1.0)
                {
                    double htemp1 = ADAPTIVERK_SAFETY * h * System.Math.Pow(errmax, ADAPTIVERK_PSHRINK);
                    double htemp2 = h / 10.0;

                    // This logic matches the original C++ implementation, which uses max for positive h
                    // and min for negative h to ensure the step magnitude is reduced.
                    if (h >= 0.0)
                        h = System.Math.Max(htemp1, htemp2);
                    else
                        h = System.Math.Min(htemp1, htemp2);
                    
                    double xnew = x + h;
                    if (xnew == x)
                        throw new InvalidOperationException($"Stepsize underflow ({h}) at x = {x} in AdaptiveRungeKutta.Rkqs");
                    
                    continue;
                }
                else
                {
                    if (errmax > ADAPTIVERK_ERRCON)
                        hnext = ADAPTIVERK_SAFETY * h * System.Math.Pow(errmax, ADAPTIVERK_PGROW);
                    else
                        hnext = 5.0 * h;

                    hdid = h;
                    x += hdid;
                    for (int i = 0; i < n; i++)
                        y[i] = ytemp[i];
                    
                    break;
                }
            }
        }

        private void Rkck(T[] y, T[] dydx, double x, double h, T[] yout, T[] yerr, OdeFunction derivs)
        {
            int n = y.Length;
            T[] ak2 = new T[n], ak3 = new T[n], ak4 = new T[n], ak5 = new T[n], ak6 = new T[n];
            T[] ytemp = new T[n];

            // first step
            for (int i = 0; i < n; i++)
                ytemp[i] = (dynamic)y[i] + b21 * h * (dynamic)dydx[i];

            // second step
            ak2 = derivs(x + a2 * h, ytemp);
            for (int i = 0; i < n; i++)
                ytemp[i] = (dynamic)y[i] + h * (b31 * (dynamic)dydx[i] + b32 * (dynamic)ak2[i]);

            // third step
            ak3 = derivs(x + a3 * h, ytemp);
            for (int i = 0; i < n; i++)
                ytemp[i] = (dynamic)y[i] + h * (b41 * (dynamic)dydx[i] + b42 * (dynamic)ak2[i] + b43 * (dynamic)ak3[i]);

            // fourth step
            ak4 = derivs(x + a4 * h, ytemp);
            for (int i = 0; i < n; i++)
                ytemp[i] = (dynamic)y[i] + h * (b51 * (dynamic)dydx[i] + b52 * (dynamic)ak2[i] + b53 * (dynamic)ak3[i] + b54 * (dynamic)ak4[i]);

            // fifth step
            ak5 = derivs(x + a5 * h, ytemp);
            for (int i = 0; i < n; i++)
                ytemp[i] = (dynamic)y[i] + h * (b61 * (dynamic)dydx[i] + b62 * (dynamic)ak2[i] + b63 * (dynamic)ak3[i] + b64 * (dynamic)ak4[i] + b65 * (dynamic)ak5[i]);

            // sixth step
            ak6 = derivs(x + a6 * h, ytemp);
            for (int i = 0; i < n; i++)
            {
                yout[i] = (dynamic)y[i] + h * (c1 * (dynamic)dydx[i] + c3 * (dynamic)ak3[i] + c4 * (dynamic)ak4[i] + c6 * (dynamic)ak6[i]);
                yerr[i] = h * (dc1 * (dynamic)dydx[i] + dc3 * (dynamic)ak3[i] + dc4 * (dynamic)ak4[i] + dc5 * (dynamic)ak5[i] + dc6 * (dynamic)ak6[i]);
            }
        }

        private double Abs(T val)
        {
            if (val is Complex c) return c.Magnitude;
            if (val is double d) return System.Math.Abs(d);
            return System.Math.Abs(Convert.ToDouble(val));
        }
    }
}