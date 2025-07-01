using System;
using Antares.Distribution;
using Antares.Integrator.Integrators;

namespace Antares.Engine
{
    public class QdPlusAddOnValue
    {
        private readonly double _T;
        private readonly double _S;
        private readonly double _K;
        private readonly double _r;
        private readonly double _q;
        private readonly double _vol;
        private readonly double _xmax;
        private readonly Func<double, double> _boundaryFunction;
        private readonly StandardNormalDistribution _phi;

        public QdPlusAddOnValue(double T, double S, double K, double r, double q, double vol, double xmax, Func<double, double> boundaryFunction)
        {
            _T = T;
            _S = S;
            _K = K;
            _r = r;
            _q = q;
            _vol = vol;
            _xmax = xmax;
            _boundaryFunction = boundaryFunction;
            _phi = new StandardNormalDistribution();
        }

        public double Evaluate(double z)
        {
            try
            {
                double xi = 0.5 * (1.0 + z);
                double time_remaining = xi * xi * _T;
                
                if (time_remaining < 1e-12)
                {
                    return 0.0;
                }
                
                double boundary_value = GetBoundaryValue(time_remaining);
                
                if (boundary_value <= _K * 1e-6 || boundary_value >= _K * 0.999)
                {
                    return 0.0;
                }
                
                double sqrtT = Math.Sqrt(time_remaining);
                double d_plus = (Math.Log(_S / boundary_value) + (_r - _q + 0.5 * _vol * _vol) * time_remaining) / (_vol * sqrtT);
                double d_minus = d_plus - _vol * sqrtT;
                
                double term1 = _r * _K * Math.Exp(-_r * time_remaining) * _phi.CumulativeDistribution(-d_minus);
                double term2 = _q * _S * Math.Exp(-_q * time_remaining) * _phi.CumulativeDistribution(-d_plus);
                
                double jacobian = xi * _T;
                
                return (term1 - term2) * jacobian;
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        public double CalculateTotal()
        {
            try
            {
                var integrator = new GaussLegendreIntegrator(32);
                return integrator.Integrate(Evaluate, -1.0, 1.0);
            }
            catch (Exception)
            {
                return 0.0;
            }
        }

        private double GetBoundaryValue(double time_remaining)
        {
            try
            {
                if (_boundaryFunction != null)
                {
                    return _boundaryFunction(time_remaining);
                }
                
                if (time_remaining < 1e-12)
                {
                    return _r >= _q ? _K : _K * Math.Max(0.1, Math.Min(0.9, Math.Abs(_r/_q)));
                }
                else if (time_remaining > 10.0)
                {
                    return CalculateAsymptoticBoundary();
                }
                else
                {
                    double nearExpiry = _r >= _q ? _K : _K * Math.Max(0.1, Math.Min(0.9, Math.Abs(_r/_q)));
                    double asymptotic = CalculateAsymptoticBoundary();
                    double weight = Math.Min(1.0, time_remaining / Math.Max(_T, 1e-6));
                    return nearExpiry * (1.0 - weight) + asymptotic * weight;
                }
            }
            catch (Exception)
            {
                double nearExpiry = _r >= _q ? _K : _K * Math.Max(0.1, Math.Min(0.9, Math.Abs(_r/_q)));
                double asymptotic = CalculateAsymptoticBoundary();
                double weight = Math.Min(1.0, time_remaining / Math.Max(_T, 1e-6));
                return nearExpiry * (1.0 - weight) + asymptotic * weight;
            }
        }
        
        private double CalculateAsymptoticBoundary()
        {
            if (Math.Abs(_q) < 1e-12)
            {
                return _r <= 0 ? _K * 0.9 : _K * (_r > 0 ? _r/(_r + 0.5*_vol*_vol) : 0.1);
            }

            if (_r <= _q)
            {
                double mu = _r - _q - 0.5 * _vol * _vol;
                double discriminant = mu * mu + 2.0 * _r * _vol * _vol;
                
                if (discriminant > 0 && _r > 0)
                {
                    double lambda_minus = (mu - Math.Sqrt(discriminant)) / (_vol * _vol);
                    if (lambda_minus < -1e-6)
                    {
                        double boundary = _K * lambda_minus / (lambda_minus - 1.0);
                        return Math.Max(_K * 0.1, Math.Min(_K * 0.9, boundary));
                    }
                }
                
                double ratio = Math.Max(0.1, Math.Min(0.8, Math.Pow(Math.Abs(_r/_q), 0.5)));
                return _K * ratio;
            }

            double mu_std = _r - _q - 0.5 * _vol * _vol;
            double discriminant_std = mu_std * mu_std + 2.0 * _r * _vol * _vol;
            
            if (discriminant_std <= 0)
            {
                return _K * 0.7;
            }
            
            double lambda_minus_std = (mu_std - Math.Sqrt(discriminant_std)) / (_vol * _vol);
            
            if (lambda_minus_std >= -1e-6)
            {
                return _K * 0.7;
            }
            
            double boundary_std = _K * lambda_minus_std / (lambda_minus_std - 1.0);
            return Math.Max(_K * 0.1, Math.Min(_K * 0.9, boundary_std));
        }
    }
}