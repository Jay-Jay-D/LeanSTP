using QuantConnect.Data.Custom;

namespace QuantConnect.Algorithm.CSharp
{
    public class QuandlUSTeasuryYield : Quandl
    {
        public QuandlUSTeasuryYield()
            : base("10 YR")
        {
        }
    }
}