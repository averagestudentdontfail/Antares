// C# code for Concentrating1dMesher.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Math;
using Antares.Math.Interpolation;
using Antares.Math.Order;
using Antares.Math.Solver;

namespace Antares.Method.Mesh
{
    /// <summary>
    /// One-dimensional grid mesher concentrating around critical points.
    /// </summary>
    public class Concentrating1dMesher : Fdm1dMesher
    {
        /// <summary>
        /// Constructor for a grid concentrating around a single point.
        /// </summary>
        /// <param name="start">The start of the domain.</param>
        /// <param name="end">The end of the domain.</param>
        /// <param name="size">The number of points in the grid.</param>
        /// <param name="cPointInfo">A tuple containing the concentration point and its density. Both can be null.</param>
        /// <param name="requireCPoint">If true, the concentration point is required to be on the grid.</param>
        public Concentrating1dMesher(double start, double end, int size,
            (double? point, double? density) cPointInfo,
            bool requireCPoint = false) : base(size)
        {
            QL.Require(end > start, "end must be larger than start");

            double? cPoint = cPointInfo.point;
            double? density = cPointInfo.density.HasValue ? cPointInfo.density * (end - start) : null;

            QL.Require(!cPoint.HasValue || (cPoint.Value >= start && cPoint.Value <= end),
                "cPoint must be between start and end");
            QL.Require(!density.HasValue || density.Value > 0.0, "density > 0 required");
            QL.Require(!cPoint.HasValue || density.HasValue, "density must be given if cPoint is given");
            QL.Require(!requireCPoint || cPoint.HasValue, "cPoint is required in grid but not given");

            double dx = 1.0 / (size - 1);

            if (cPoint.HasValue)
            {
                var u = new List<double>();
                var z = new List<double>();
                LinearInterpolation transform = null;
                double c1 = System.Math.Asinh((start - cPoint.Value) / density.Value);
                double c2 = System.Math.Asinh((end - cPoint.Value) / density.Value);
                if (requireCPoint)
                {
                    u.Add(0.0);
                    z.Add(0.0);
                    if (!Comparison.Close(cPoint.Value, start) && !Comparison.Close(cPoint.Value, end))
                    {
                        double z0 = -c1 / (c2 - c1);
                        double u0 = System.Math.Max(
                                        System.Math.Min((long)System.Math.Round(z0 * (size - 1), MidpointRounding.AwayFromZero), (long)size - 2), 1L)
                                    / (double)(size - 1);
                        u.Add(u0);
                        z.Add(z0);
                    }
                    u.Add(1.0);
                    z.Add(1.0);
                    transform = new LinearInterpolation(u.ToArray(), z.ToArray());
                }

                for (int i = 1; i < size - 1; ++i)
                {
                    double li = requireCPoint ? transform.Value(i * dx) : i * dx;
                    _locations[i] = cPoint.Value + density.Value * System.Math.Sinh(c1 * (1.0 - li) + c2 * li);
                }
            }
            else
            {
                for (int i = 1; i < size - 1; ++i)
                {
                    _locations[i] = start + i * dx * (end - start);
                }
            }

            _locations[0] = start;
            _locations[size - 1] = end;

            for (int i = 0; i < size - 1; ++i)
            {
                _dplus[i] = _dminus[i + 1] = _locations[i + 1] - _locations[i];
            }
            _dplus[size - 1] = _dminus[0] = double.NaN;
        }

        /// <summary>
        /// Constructor for a grid concentrating around multiple points.
        /// </summary>
        /// <param name="start">The start of the domain.</param>
        /// <param name="end">The end of the domain.</param>
        /// <param name="size">The number of points in the grid.</param>
        /// <param name="cPoints">A list of tuples, each containing a concentration point, its density, and a flag indicating if it's a required point.</param>
        /// <param name="tol">Tolerance for the ODE solver and root finders.</param>
        public Concentrating1dMesher(double start, double end, int size,
            IReadOnlyList<Tuple<double, double, bool>> cPoints,
            double tol = 1e-8) : base(size)
        {
            QL.Require(end > start, "end must be larger than start");

            var points = cPoints.Select(cp => cp.Item1).ToList();
            var betas = cPoints.Select(cp => Functional.Squared(cp.Item2 * (end - start))).ToList();

            // Get scaling factor 'a' so that y(1) = end
            double aInit = 0.0;
            for (int i = 0; i < points.Count; ++i)
            {
                double c1 = System.Math.Asinh((start - points[i]) / betas[i]);
                double c2 = System.Math.Asinh((end - points[i]) / betas[i]);
                aInit += (c2 - c1) / points.Count;
            }

            var fct = new OdeIntegrationFct(points, betas, tol);
            Func<double, double> funcToSolve = x => fct.Solve(x, start, 0.0, 1.0) - end;
            double a = new Brent().Solve(funcToSolve, tol, aInit, 0.1 * aInit);

            // Solve ODE for all grid points
            var x = new Array(size);
            var y = new Array(size);
            x[0] = 0.0;
            y[0] = start;
            double dx = 1.0 / (size - 1);
            for (int i = 1; i < size; ++i)
            {
                x[i] = i * dx;
                y[i] = fct.Solve(a, y[i - 1], x[i - 1], x[i]);
            }

            // Eliminate numerical noise and ensure y(1) = end
            double dy = y[size - 1] - end;
            for (int i = 1; i < size; ++i)
            {
                y[i] -= i * dx * dy;
            }

            var odeSolution = new LinearInterpolation(x._storage.ToArray(), y._storage.ToArray());

            // Ensure required points are part of the grid
            var w = new List<Tuple<double, double>> { Tuple.Create(0.0, 0.0) };
            for (int i = 0; i < points.Count; ++i)
            {
                if (cPoints[i].Item3 && points[i] > start && points[i] < end)
                {
                    int j = Array.BinarySearch(y._storage.ToArray(), points[i]);
                    if (j < 0) j = ~j;
                    j = System.Math.Min(j, size - 1);

                    Func<double, double> rootFunc = val => odeSolution.Value(val, true) - points[i];
                    double e = new Brent().Solve(rootFunc, QLDefines.EPSILON, x[j], 0.5 / size);

                    w.Add(Tuple.Create(System.Math.Min(x[size - 2], x[j]), e));
                }
            }
            w.Add(Tuple.Create(1.0, 1.0));
            
            // Sort and remove duplicates based on the first item being "close enough"
            w.Sort((p1, p2) => p1.Item1.CompareTo(p2.Item1));
            var distinctW = new List<Tuple<double, double>>();
            if (w.Any())
            {
                distinctW.Add(w[0]);
                for (int i = 1; i < w.Count; i++)
                {
                    if (!Comparison.CloseEnough(w[i].Item1, distinctW.Last().Item1, 1000))
                    {
                        distinctW.Add(w[i]);
                    }
                }
            }

            var u = distinctW.Select(item => item.Item1).ToArray();
            var z = distinctW.Select(item => item.Item2).ToArray();
            var transform = new LinearInterpolation(u, z);

            for (int i = 0; i < size; ++i)
            {
                _locations[i] = odeSolution.Value(transform.Value(i * dx));
            }

            for (int i = 0; i < size - 1; ++i)
            {
                _dplus[i] = _dminus[i + 1] = _locations[i + 1] - _locations[i];
            }
            _dplus[size - 1] = _dminus[0] = double.NaN;
        }


        private class OdeIntegrationFct
        {
            private readonly AdaptiveRungeKutta<double> _rk;
            private readonly IReadOnlyList<double> _points;
            private readonly IReadOnlyList<double> _betas;

            public OdeIntegrationFct(IReadOnlyList<double> points, IReadOnlyList<double> betas, double tol)
            {
                _rk = new AdaptiveRungeKutta<double>(tol);
                _points = points;
                _betas = betas;
            }

            public double Solve(double a, double y0, double x0, double x1)
            {
                AdaptiveRungeKutta<double>.OdeFunction1d odeFct = (x, y) => Jac(a, x, y);
                return _rk.Integrate(odeFct, y0, x0, x1);
            }

            private double Jac(double a, double x, double y)
            {
                double s = 0.0;
                for (int i = 0; i < _points.Count; ++i)
                {
                    s += 1.0 / (_betas[i] + Functional.Squared(y - _points[i]));
                }
                return a / System.Math.Sqrt(s);
            }
        }
    }
}