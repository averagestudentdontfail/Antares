// Dividend.cs

using System;
using System.Collections.Generic;
using System.Linq;
using Antares.Pattern;
using Antares.Time;

namespace Antares.Cashflow
{
    /// <summary>
    /// Base interface for a stock dividend.
    /// </summary>
    public interface IDividend : ICashFlow
    {
        /// <summary>
        /// Returns the dividend amount for a given underlying value.
        /// </summary>
        Real Amount(Real underlying);
    }

    /// <summary>
    /// Abstract base class for a stock dividend.
    /// </summary>
    public abstract class Dividend : CashFlow, IDividend
    {
        private readonly Date _date;

        protected Dividend(Date date)
        {
            _date = date;
        }

        #region Event interface
        public override Date Date => _date;
        #endregion

        #region CashFlow interface
        public abstract override Real Amount { get; }
        #endregion

        #region Dividend interface
        public abstract Real Amount(Real underlying);
        #endregion

        #region Visitability
        public override void Accept(IAcyclicVisitor v)
        {
            if (v is IVisitor<IDividend> visitor)
            {
                visitor.Visit(this);
            }
            else
            {
                base.Accept(v); // Fall back to CashFlow visitor
            }
        }
        #endregion
    }

    /// <summary>
    /// Predetermined cash flow paying a fixed amount at a given date.
    /// </summary>
    public class FixedDividend : Dividend
    {
        private readonly Real _amount;

        public FixedDividend(Real amount, Date date) : base(date)
        {
            _amount = amount;
        }

        #region Dividend interface
        public override Real Amount => _amount;
        public override Real Amount(Real underlying) => _amount;
        #endregion
    }

    /// <summary>
    /// Predetermined cash flow paying a fractional amount of an underlying at a given date.
    /// </summary>
    public class FractionalDividend : Dividend
    {
        private readonly Real _rate;
        private readonly Real? _nominal;

        public FractionalDividend(Real rate, Date date) : base(date)
        {
            _rate = rate;
            _nominal = null;
        }

        public FractionalDividend(Real rate, Real nominal, Date date) : base(date)
        {
            _rate = rate;
            _nominal = nominal;
        }

        #region Dividend interface
        public override Real Amount
        {
            get
            {
                QL.Require(_nominal.HasValue, "no nominal given");
                return _rate * _nominal.Value;
            }
        }

        public override Real Amount(Real underlying) => _rate * underlying;
        #endregion

        #region Inspectors
        public Real Rate => _rate;
        public Real? Nominal => _nominal;
        #endregion
    }

    public static class DividendFactory
    {
        /// <summary>
        /// Helper function building a sequence of fixed dividends.
        /// </summary>
        public static List<IDividend> DividendVector(IReadOnlyList<Date> dividendDates, IReadOnlyList<Real> dividends)
        {
            QL.Require(dividendDates.Count == dividends.Count, "size mismatch between dividend dates and amounts");

            var items = new List<IDividend>(dividendDates.Count);
            for (int i = 0; i < dividendDates.Count; i++)
            {
                items.Add(new FixedDividend(dividends[i], dividendDates[i]));
            }
            return items;
        }
    }
}