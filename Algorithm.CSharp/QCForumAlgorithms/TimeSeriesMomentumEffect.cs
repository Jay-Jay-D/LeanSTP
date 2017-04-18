using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     Every month, the investor considers whether the excess return of each asset over the past 12
    ///     months is positive or negative and goes long on the contract if it is positive and short if
    ///     negative. The position size is set to be inversely proportional to the instrument’s volatility.
    /// </summary>
    /// <seealso cref="QuantConnect.Algorithm.QCAlgorithm" />
    public class TimeSeriesMomentumEffect : QCAlgorithm
    {
        private readonly Dictionary<Symbol, decimal> excessReturns = new Dictionary<Symbol, decimal>();

        private readonly string[] forexTickers =
        {
            "AUDUSD",
            "EURUSD",
            "GBPUSD",
            "USDJPY"
        };

        // The full list of Futures can be found in Table 1
        // at http://pages.stern.nyu.edu/~lpederse/papers/TimeSeriesMomentum.pdf
        // Here I'll use only commodities already listed in the Futures Class.

        private readonly string[] futuresTickers =
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

        // The Generally the 10 year maturity is used as a proxy of the risk free rate.
        private readonly string riskFreeReturnQuandlCode = "USTREASURY/YIELD";

        // 
        private readonly List<Symbol> symbols = new List<Symbol>();

        private bool monthlyRebalance;

        private decimal riskFreeRetun;

        public override void Initialize()
        {
            // Set the basic algorithm parameters.
            SetStartDate(2016, 06, 01);
            SetEndDate(2016, 08, 30);
            SetCash(100000);

            AddData<QuandlUSTeasuryYield>(riskFreeReturnQuandlCode, Resolution.Daily);

            //foreach (var ticker in futuresTickers)
            //    symbols.Add(AddFuture(ticker, Resolution.Daily).Symbol);

            foreach (var ticker in forexTickers)
            {
                var forex = AddForex(ticker, Resolution.Daily, Market.Oanda);
                symbols.Add(forex.Symbol);
                // https://en.wikipedia.org/wiki/Coefficient_of_variation
                forex.VolatilityModel =
                    new IndicatorVolatilityModel<IndicatorDataPoint>(STD(forex.Symbol, 20, Resolution.Daily)
                        .Over(SMA(forex.Symbol, 20, Resolution.Daily)));
            }


            Schedule.On(DateRules.MonthStart(), TimeRules.At(0, 0), () =>
            {
                UpdateAssetsReturns();
                monthlyRebalance = true;
            });
        }

        private void UpdateAssetsReturns()
        {
            var dateRequest = new DateTime(Time.Year - 1, Time.Month, Time.Day);
            // I ask for some days before just in case the selected day hasn't historical prices record.
            var history = History(symbols, dateRequest.AddDays(-5), dateRequest.AddDays(1), Resolution.Daily);
            foreach (var symbol in symbols)
            {
                Slice slice;
                try
                {
                    slice = history.Last(s => s.ContainsKey(symbol));
                    excessReturns[symbol] = (Securities[symbol].Price / slice[symbol].Price - 1m) * 100m -
                                            riskFreeRetun;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    excessReturns[symbol] = 0m;
                }
            }
        }

        public override void OnData(Slice slice)
        {
            if (monthlyRebalance)
            {
                var assetsToLong = excessReturns.Where(s => s.Value > 0m)
                    .ToDictionary(pair => pair.Key,
                        pair => Securities[pair.Key].VolatilityModel.Volatility);
                var assetsToShort = excessReturns.Where(s => s.Value < 0m)
                    .ToDictionary(pair => pair.Key,
                        pair => Securities[pair.Key].VolatilityModel.Volatility);

                var tradesToClose = Securities.Where(
                    secPair => Portfolio[secPair.Key].Invested &&
                                   !assetsToShort.Keys.Concat(assetsToLong.Keys).Contains(secPair.Key))
                                               .Select(s => s.Key);

                foreach (var symbol in tradesToClose)
                {
                    Liquidate(symbol);
                }

                var divisor = assetsToLong.Values.Sum();
                foreach (var keyValuePair in assetsToLong)
                    SetHoldings(keyValuePair.Key, keyValuePair.Value / divisor);
                divisor = assetsToShort.Values.Sum();
                foreach (var keyValuePair in assetsToShort)
                    SetHoldings(keyValuePair.Key, -keyValuePair.Value / divisor);
                monthlyRebalance = false;
            }
        }

        public void OnData(Quandl data)
        {
            riskFreeRetun = data.Price;
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