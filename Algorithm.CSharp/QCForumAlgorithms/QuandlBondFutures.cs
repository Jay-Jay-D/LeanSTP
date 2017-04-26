using QuantConnect.Data.Custom;

namespace QuantConnect.Algorithm.CSharp
{
    public class QuandlBondFutures : Quandl
    {
        public QuandlBondFutures()
            : base("Settle")
        {
        }
    }
}