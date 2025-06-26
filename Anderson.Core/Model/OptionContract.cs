using System;

namespace Anderson.Model
{
    /// <summary>
    /// Represents an option contract with all necessary parameters
    /// </summary>
    public class OptionContract
    {
        public string Symbol { get; set; } = string.Empty;
        public OptionRight Right { get; set; }
        public decimal Strike { get; set; }
        public DateTime Expiry { get; set; }
        public OptionStyle Style { get; set; } = OptionStyle.American;
        public Greeks Greeks { get; set; } = new Greeks();

        public double TimeToExpiry(DateTime currentTime)
        {
            var timeSpan = Expiry - currentTime;
            return Math.Max(0, timeSpan.TotalDays / 365.0);
        }

        public override string ToString()
        {
            return $"{Symbol} {Right} {Strike} {Expiry:yyyy-MM-dd} ({Style})";
        }
    }

    public enum OptionStyle
    {
        American,
        European
    }
}