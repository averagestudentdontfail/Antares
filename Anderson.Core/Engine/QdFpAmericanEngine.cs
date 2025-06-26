using System;
using Anderson.Distribution;
using Anderson.Interpolation.Interpolators;

namespace Anderson.Engine
{
    public enum FixedPointEquation { FP_A, FP_B, Auto }

    public class QdFpAmericanEngine
    {
        private readonly IQdFpIterationScheme _scheme;
        private readonly FixedPointEquation _fpEquation;

        public QdFpAmericanEngine(
            IQdFpIterationScheme? scheme = null,
            FixedPointEquation fpEquation = FixedPointEquation.Auto)
        {
            _scheme = scheme ?? new QdFpLegendreScheme(16, 8, 16, 32);
            _fpEquation = fpEquation;
        }

        public double CalculatePut(double S, double K, double r, double q, double vol, double T)
        {
            double xmax = QdPlusAmericanEngine.XMax(K, r, q);

            if (xmax <= 0)
            {
                throw new ArgumentException("This case (likely q < r < 0) results in a double exercise boundary and is not supported by this single-boundary engine.");
            }

            if (double.IsInfinity(xmax))
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            // Step 1: Get initial guess for the boundary from the QD+ Engine
            var qdPlusEngine = new QdPlusAmericanEngine(_scheme.GetNumberOfChebyshevInterpolationNodes());
            var boundaryInterp = qdPlusEngine.GetPutExerciseBoundary(K, r, q, vol, T);
            
            // Step 2: Define boundary function B(τ) with the CORRECT coordinate transformation
            double sqrtT = Math.Sqrt(T);
            Func<double, double> B_func = tau => 
            {
                if (tau <= 1e-12) return xmax;
                double z_node = (2.0 * Math.Sqrt(tau) / sqrtT) - 1.0;
                double h_val = boundaryInterp.Value(z_node);
                return xmax * Math.Exp(-Math.Sqrt(Math.Max(0, h_val)));
            };

            // Step 3: Choose the fixed-point equation formulation
            bool useFP_A = (_fpEquation == FixedPointEquation.FP_A) || 
                           (_fpEquation == FixedPointEquation.Auto && Math.Abs(r - q) < 0.01 && vol > 0.05);

            DqFpEquation equation = useFP_A
                ? new DqFpEquation_A(K, r, q, vol, B_func, _scheme.GetFixedPointIntegrator())
                : new DqFpEquation_B(K, r, q, vol, B_func, _scheme.GetFixedPointIntegrator());

            // Step 4: Perform iterative refinement of the boundary
            double[] z_nodes = boundaryInterp.Nodes();
            double[] h_values = boundaryInterp.Values();
            Func<double, double> h_transform = fv => Math.Pow(Math.Log(Math.Max(1e-12, fv) / xmax), 2);

            // Part A: Jacobi-Newton Steps
            for (int k = 0; k < _scheme.GetNumberOfJacobiNewtonFixedPointSteps(); k++)
            {
                for (int i = 1; i < z_nodes.Length; i++)
                {
                    double tau = 0.25 * T * Math.Pow(1 + z_nodes[i], 2);
                    double b_current = B_func(tau);
                    (double N, double D, double Fv) = equation.F(tau, b_current);

                    if (tau < 1e-9 || Math.Abs(D) < 1e-9)
                    {
                        h_values[i] = h_transform(Fv);
                    }
                    else
                    {
                        (double Nd, double Dd) = equation.NDd(tau, b_current);
                        double alpha = K * Math.Exp(-(r - q) * tau);
                        double fd = alpha * (Nd * D - Dd * N) / (D * D);
                        double b_new = b_current - (Fv - b_current) / (fd - 1.0);
                        h_values[i] = h_transform(b_new);
                    }
                }
                boundaryInterp.UpdateY(h_values);
            }
            
            // Part B: Naive Richardson Fixed-Point Steps
            for (int k = 0; k < _scheme.GetNumberOfNaiveFixedPointSteps(); k++)
            {
                for (int i = 1; i < z_nodes.Length; i++)
                {
                    double tau = 0.25 * T * Math.Pow(1.0 + z_nodes[i], 2);
                    (_, _, double Fv) = equation.F(tau, B_func(tau));
                    h_values[i] = h_transform(Fv);
                }
                boundaryInterp.UpdateY(h_values);
            }
            
            // Step 5: Calculate final price using the refined boundary
            // CORRECTED: Pass the concrete ChebyshevInterpolation type.
            var addOnValueFunc = new QdPlusAddOnValue(T, S, K, r, q, vol, xmax, boundaryInterp);
            double addOn = _scheme.GetExerciseBoundaryToPriceIntegrator().Integrate(addOnValueFunc.Evaluate, 0.0, sqrtT);
            
            double europeanValue = CalculateBlackScholesPut(S, K, r, q, vol, T);

            // Final price is the greater of the European price and intrinsic value, plus the early exercise premium.
            return Math.Max(europeanValue, K - S) + Math.Max(0.0, addOn);
        }

        private static double CalculateBlackScholesPut(double S, double K, double r, double q, double vol, double T)
        {
            if (T <= 1e-9) return Math.Max(K - S, 0.0);
            if (vol <= 1e-9) return Math.Max(K * Math.Exp(-r * T) - S * Math.Exp(-q * T), 0.0);

            double sqrtT = Math.Sqrt(T);
            double d1 = (Math.Log(S / K) + (r - q + 0.5 * vol * vol) * T) / (vol * sqrtT);
            double d2 = d1 - vol * sqrtT;
            
            return K * Math.Exp(-r * T) * Distributions.CumulativeNormal(-d2) - S * Math.Exp(-q * T) * Distributions.CumulativeNormal(-d1);
        }
    }
}