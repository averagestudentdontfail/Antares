using System;
using Antares.Distribution;
using Antares.Interpolation.Interpolators;

namespace Antares.Engine
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
            if (T < 1e-9) return Math.Max(0.0, K - S);

            double xmax = QdPlusAmericanEngine.XMax(K, r, q);
            
            if (xmax <= K * 1e-6)
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            var qdPlusEngine = new QdPlusAmericanEngine(_scheme.GetNumberOfChebyshevInterpolationNodes());
            ChebyshevInterpolation boundaryInterp;
            
            try
            {
                boundaryInterp = qdPlusEngine.GetPutExerciseBoundary(K, r, q, vol, T);
            }
            catch (Exception)
            {
                return CalculateBlackScholesPut(S, K, r, q, vol, T);
            }

            double sqrtT = Math.Sqrt(T);
            Func<double, double> B_func = tau =>
            {
                if (tau <= 1e-12) return xmax;
                double z_node = (2.0 * Math.Sqrt(tau) / sqrtT) - 1.0;
                z_node = Math.Max(-1.0, Math.Min(1.0, z_node));
                double h_val = boundaryInterp.Value(z_node);
                h_val = Math.Max(0.0, Math.Min(h_val, 50.0));
                return Math.Max(xmax * Math.Exp(-h_val * h_val), S * 1e-6);
            };
            
            bool useFP_A = DetermineOptimalEquation(r, q, vol);

            DqFpEquation equation = useFP_A
                ? new DqFpEquation_A(K, r, q, vol, B_func, _scheme.GetFixedPointIntegrator())
                : new DqFpEquation_B(K, r, q, vol, B_func, _scheme.GetFixedPointIntegrator());

            SolveFixedPoint(equation, boundaryInterp, sqrtT, T, xmax, K, r, q);
            
            var addOnValueFunc = new QdPlusAddOnValue(T, S, K, r, q, vol, xmax, boundaryInterp);
            double addOn = _scheme.GetExerciseBoundaryToPriceIntegrator().Integrate(addOnValueFunc.Evaluate, 0.0, sqrtT);
            
            double europeanValue = CalculateBlackScholesPut(S, K, r, q, vol, T);

            return Math.Max(K - S, europeanValue + Math.Max(0.0, addOn));
        }

        private bool DetermineOptimalEquation(double r, double q, double vol)
        {
            if (_fpEquation == FixedPointEquation.FP_A) return true;
            if (_fpEquation == FixedPointEquation.FP_B) return false;
            
            double rqDiff = Math.Abs(r - q);
            double volSq = vol * vol;
            
            bool useFP_A = rqDiff < 0.5 * volSq;
            
            if (q > r && (q - r) > 0.1)
            {
                useFP_A = false;
            }
            
            return useFP_A;
        }

        private void SolveFixedPoint(DqFpEquation equation, ChebyshevInterpolation boundaryInterp, 
            double sqrtT, double T, double xmax, double K, double r, double q)
        {
            double[] z_nodes = boundaryInterp.Nodes();
            double[] h_values = boundaryInterp.Values();
            
            Func<double, double> h_transform = fv => 
            {
                double ratio = Math.Max(1e-12, fv) / xmax;
                ratio = Math.Min(ratio, 1.0 - 1e-12);
                return Math.Sqrt(Math.Max(0.0, -Math.Log(ratio)));
            };

            for (int k = 0; k < _scheme.GetNumberOfJacobiNewtonFixedPointSteps(); k++)
            {
                for (int i = 1; i < z_nodes.Length; i++)
                {
                    double tau = 0.25 * T * Math.Pow(1 + z_nodes[i], 2);
                    
                    double h_current = Math.Max(0.0, h_values[i]);
                    double b_current = xmax * Math.Exp(-h_current * h_current);
                    
                    var (N, D, Fv) = equation.F(tau, b_current);

                    if (tau < 1e-9 || Math.Abs(D) < 1e-12)
                    {
                        h_values[i] = h_transform(Fv);
                    }
                    else
                    {
                        var (Nd, Dd) = equation.NDd(tau, b_current);
                        double alpha = K * Math.Exp(-(r - q) * tau);
                        
                        double denominator = D * D;
                        if (Math.Abs(denominator) > 1e-15)
                        {
                            double fd = alpha * (Nd * D - Dd * N) / denominator;
                            double adjustment = (Fv - b_current) / (fd - 1.0);
                            
                            double dampingFactor = 0.8;
                            double b_new = b_current - dampingFactor * adjustment;
                            
                            b_new = Math.Max(b_new, xmax * 0.01);
                            b_new = Math.Min(b_new, K * 0.99);
                            
                            h_values[i] = h_transform(b_new);
                        }
                        else
                        {
                            h_values[i] = h_transform(Fv);
                        }
                    }
                }
                boundaryInterp.UpdateY(h_values);
            }
            
            for (int k = 0; k < _scheme.GetNumberOfNaiveFixedPointSteps(); k++)
            {
                for (int i = 1; i < z_nodes.Length; i++)
                {
                    double tau = 0.25 * T * Math.Pow(1.0 + z_nodes[i], 2);
                    
                    double h_current = Math.Max(0.0, h_values[i]);
                    double b_current = xmax * Math.Exp(-h_current * h_current);
                    
                    var (_, _, Fv) = equation.F(tau, b_current);
                    h_values[i] = h_transform(Fv);
                }
                boundaryInterp.UpdateY(h_values);
            }
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