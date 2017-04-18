using System.Collections.Generic;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class TimeSeriesMomentumEffect : QCAlgorithm
    {
        // The Generally the 10 year maturity is used as a proxy of the risk free rate.
        private readonly string riskFreeReturnQuandlCode = "USTREASURY/YIELD";


        private List<Symbol> symbols = new List<Symbol>();
        // The list of Commodities Futures can be found in Table 1
        // at http://pages.stern.nyu.edu/~lpederse/papers/TimeSeriesMomentum.pdf

        private string[] universeTickers =
        {
            // Commodities available data.
            Futures.Metals.Gold,
            Futures.Metals.Platinum,
            Futures.Metals.Silver,
            Futures.Energies.CrudeOilWTI,
            Futures.Energies.HeatingOil,
            Futures.Energies.Gasoline,
            Futures.Energies.NaturalGas,
            Futures.Grains.Corn,
            Futures.Grains.Soybeans,
            Futures.Grains.SoybeanMeal,
            Futures.Grains.SoybeanOil,
            Futures.Grains.Wheat,
            Futures.Indices.SP500EMini,
            Futures.Meats.LeanHogs,
            Futures.Meats.FeederCattle,
            Futures.Softs.Cocoa,
            Futures.Softs.Coffee,
            Futures.Softs.Cotton2,
            Futures.Softs.Sugar11
        };

        public override void Initialize()
        {
            // Set the basic algorithm parameters.
            SetStartDate(2000, 01, 01);
            SetEndDate(2017, 03, 31);
            SetCash(100000);

            AddData<QuandlUSTeasuryYield>(riskFreeReturnQuandlCode);
        }

        public override void OnData(Slice slice)
        {
            base.OnData(slice);
        }

        public void OnData(Quandl data)
        {
        }
    }

    public class QuandlUSTeasuryYield : Quandl
    {
        public QuandlUSTeasuryYield()
            : base("10 YR")
        {
        }
    }
}