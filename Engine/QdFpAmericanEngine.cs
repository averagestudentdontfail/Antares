using System;
using QuantConnect.Indicators;
using QuantConnect.Securities.Option;
using Anderson.Engine;
using Anderson.Interpolation.Interpolators;
using Anderson.Distribution;

namespace Anderson.Engine
{
    public enum FixedPointEquation { FP_A, FP_B, Auto }

    /// <summary>
    /// High-performance American option pricing engine based on fixed-point iteration for the exercise boundary.
    /// This engine provides a highly accurate and fast solution by iteratively refining an initial guess
    /// of the exercise boundary obtained from the QdPlusAmericanEngine.
    /// Reference: Andersen, Lake, and Offengenden (2015), "High Performance American Option Pricing".
    /// </summary>
    public class QdFpAmericanEngine
    {
        private readonly IQdFpIterationScheme _scheme;
        private readonly FixedPointEquation _fpEquation;

        public QdFpAmericanEngine(
            IQdFpIterationScheme scheme = null,
            FixedPointEquation fpEquation = FixedPointEquation.Auto)
        {
            // Default to a highly accurate scheme if none is provided.
            _scheme = scheme ?? new QdFpLegendreLobattoScheme(16, 8, 16, 1e-10);
            _fpEquation = fpEquation;
        }

        /// <summary>
        /// Calculates the price of an American put option.
        /// </summary>
        public double CalculatePut(double S, double K, double r, double q, double vol, double T)
        {
            // Per Andersen & Lake (2021), this case is for a double-boundary put, not handled here.
            if (r < 0.0 && q < r)
            {
                throw new ArgumentException("Double-boundary case for put options (q < r < 0) is not supported by this single-boundary engine.");
            }
            
            // European case: if early exercise is never optimal, return European price.
            double xmax = QdPlusAmericanEngine.XMax(K, r, q);
            if (xmax <= 0)
            {
                // Use a simple Black-Scholes calculation for European put
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            // --- Step 1: Get initial guess for the boundary from QdPlusAmericanEngine ---
            var qdPlusEngine = new QdPlusAmericanEngine(_scheme.GetNumberOfChebyshevInterpolationNodes());
            var boundaryInterp = qdPlusEngine.GetPutExerciseBoundary(S, K, r, q, vol, T);

            // Create a delegate for the boundary function B(tau) using the interpolation
            Func<double, double> B = tau =>
            {
                if (tau <= 1e-12) return xmax;
                double z = 2.0 * Math.Sqrt(tau / T) - 1.0;
                double h_val = boundaryInterp.Value(z);
                return xmax * Math.Exp(-Math.Sqrt(Math.Max(0, h_val)));
            };

            // --- Step 2: Choose the fixed-point equation formulation ---
            bool useFP_A = (_fpEquation == FixedPointEquation.FP_A) || (_fpEquation == FixedPointEquation.Auto && Math.Abs(r - q) < 0.001);
            DqFpEquation equation = useFP_A
                ? (DqFpEquation)new DqFpEquation_A(K, r, q, vol, B, _scheme.GetFixedPointIntegrator())
                : new DqFpEquation_B(K, r, q, vol, B, _scheme.GetFixedPointIntegrator());

            // --- Step 3: Perform iterative refinement ---
            double[] z_nodes = ((ChebyshevInterpolation)boundaryInterp).Nodes();
            double[] y_values = boundaryInterp.Values(); // h-values
            Func<double, double> h_transform = fv => Math.Pow(Math.Log(Math.Max(1e-12, fv) / xmax), 2);

            // --- Part A: Jacobi-Newton Steps ---
            for (int k = 0; k < _scheme.GetNumberOfJacobiNewtonFixedPointSteps(); k++)
            {
                for (int i = 1; i < z_nodes.Length; i++)
                {
                    double tau = 0.25 * T * Math.Pow(1 + z_nodes[i], 2);
                    double b_current = B(tau);
                    (double N, double D, double Fv) = equation.F(tau, b_current);

                    if (tau < 1e-9)
                    {
                        y_values[i] = h_transform(Fv);
                    }
                    else
                    {
                        (double Nd, double Dd) = equation.NDd(tau, b_current);
                        double alpha = K * Math.Exp(-(r - q) * tau);
                        double fd = alpha * (Nd / D - Dd * N / (D * D)); // Derivative of f(B) wrt B
                        double b_new = b_current - (Fv - b_current) / (fd - 1.0); // Jacobi-Newton update
                        y_values[i] = h_transform(b_new);
                    }
                }
                boundaryInterp.UpdateY(y_values);
            }
            
            // --- Part B: Naive Richardson Fixed-Point Steps ---
            for (int k = 0; k < _scheme.GetNumberOfNaiveFixedPointSteps(); k++)
            {
                for (int i = 1; i < z_nodes.Length; i++)
                {
                    double tau = 0.25 * T * Math.Pow(1.0 + z_nodes[i], 2);
                    (_, _, double Fv) = equation.F(tau, B(tau));
                    y_values[i] = h_transform(Fv);
                }
                boundaryInterp.UpdateY(y_values);
            }
            
            // --- Step 4: Calculate final price using the refined boundary ---
            var addOnValueFunc = new QdPlusAddOnValue(T, S, K, r, q, vol, xmax, boundaryInterp);
            double addOn = _scheme.GetExerciseBoundaryToPriceIntegrator().Integrate(addOnValueFunc.Evaluate, 0.0, Math.Sqrt(T));
            
            double europeanValue = CalculateBlackScholesPut(S, K, r, q, vol, T);

            return Math.Max(europeanValue, 0.0) + Math.Max(0.0, addOn);
        }

        /// <summary>
        /// Calculates the Black-Scholes price for a European put option
        /// </summary>
        private static double CalculateBlackScholesPut(double S, double K, double r, double q, double vol, double T)
        {
            if (T <= 0) return Math.Max(K - S, 0.0);
            if (vol <= 0) return Math.Max(K - S * Math.Exp(-q * T), 0.0) * Math.Exp(-r * T);

            double sqrtT = Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * sqrtT);
            double d2 = d1 - vol * sqrtT;

            // Use the Distributions.CumulativeNormal function from Anderson.Distribution
            double Nd1 = Distributions.CumulativeNormal(d1);
            double Nd2 = Distributions.CumulativeNormal(d2);

            return K * Math.Exp(-r * T) * (1.0 - Nd2) - S * Math.Exp(-q * T) * (1.0 - Nd1);
        }
    }
}