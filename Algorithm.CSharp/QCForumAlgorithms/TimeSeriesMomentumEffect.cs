using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Indicators;
using QuantConnect.Parameters;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     Every month, the investor considers whether the excess return of each asset over the past 12
    ///     months is positive or negative and goes long on the contract if it is positive and short if
    ///     negative. The position size is set to be inversely proportional to the instrument’s volatility.
    ///     Source: http://quantpedia.com/Screener/Details/118
    /// </summary>
    /// <seealso cref="QuantConnect.Algorithm.QCAlgorithm" />
    public class TimeSeriesMomentumEffect : QCAlgorithm
    {
        #region Investment Universe

        /*
         The investment universe consists of:
            - 22 commodity futures
            - 12 cross-currency pairs (with 9 underlying currencies)
            - 9 developed equity indexes
            - 4 developed government bond futures. 
        */

        private readonly string[] comoditiesFuturesTickers =
        {
            Futures.Metals.Gold,
            Futures.Metals.Platinum,
            Futures.Metals.Silver,
            Futures.Metals.Palladium,
            Futures.Energies.CrudeOilWTI,
            Futures.Energies.HeatingOil,
            Futures.Energies.Gasoline,
            Futures.Energies.NaturalGas,
            Futures.Grains.Corn,
            Futures.Grains.Soybeans,
            Futures.Grains.SoybeanMeal,
            Futures.Grains.SoybeanOil,
            Futures.Grains.Wheat,
            Futures.Grains.Oats,
            Futures.Meats.LeanHogs,
            Futures.Meats.LiveCattle,
            Futures.Meats.FeederCattle,
            Futures.Softs.OrangeJuice,
            Futures.Softs.Cocoa,
            Futures.Softs.Coffee,
            Futures.Softs.Cotton2,
            Futures.Softs.Sugar11
        };

        private readonly string[] forexTickers =
        {
            "EURUSD", "USDJPY", "USDCHF", "GBPUSD", "USDCAD", "AUDUSD",
            "USDCNH", "NZDUSD", "EURJPY", "EURCHF", "EURGBP", "GBPJPY",
        };

        private readonly string[] cfdTickers =
        {
            "AU200AUD",  // Australia 200 - Australian Dollar
            "DE30EUR",   // Germany 30 - Euro Dollar
            "EU50EUR",   // Europe 50 - Euro Dollar
            "CH20CHF",   // Swiss 20 - Swiss Frank
            "FR40EUR",   // France 40 - Euro Dollar
            "HK33HK",    // Hong Kong 33 - Hk Dollar
            "JP225USD",  // Japan 225 - Us Dollar
            "UK100GBP",  // Uk 100 - English Pound
            "SPX500USD", // S&p 500 - Us Dollar
        };

        private readonly string[] bondsFuturesTickers =
        {
            "SCF/CME_TU1_ON",       // CBOT 2-year US Treasury Note Futures
            "SCF/CME_FV1_ON",       // CBOT 5-year US Treasury Note Futures
            "SCF/CME_TY1_ON",       // CBOT 10-year US Treasury Note Futures
            "SCF/CME_US2_ON",       // CBOT 30-year US Treasury Bond Futures
            "SCF/EUREX_FGBL2_EN",   // EUREX Euro-Bund Futures (German debt security)
            "SCF/EUREX_FBTP2_EN",   // EUREX Euro-BTP Futures (Italian debt security)
            "SCF/EUREX_FOAT2_EN",   // EUREX Euro-OAT Futures (French debt security)
        };

        #endregion

        [Parameter("broker")]
        private string forexMarket = "fxcm";
        private string cfdMarket = "oanda";


        private const int cfdLeverage = 10;
        private const int forexLeverage = 20;

        // The 10 year maturity is used as a proxy of the risk free rate.
        private const string riskFreeReturnQuandlCode = "USTREASURY/YIELD";

        // "fxcm"
        private const int volatilityWindow = 90;

        private readonly Dictionary<Symbol, decimal> excessReturns = new Dictionary<Symbol, decimal>();


        // The full list of Futures can be found in Table 1
        // at http://pages.stern.nyu.edu/~lpederse/papers/TimeSeriesMomentum.pdf
        // Here I'll use only commodities already listed in the Futures Class.


        private readonly List<Symbol> symbols = new List<Symbol>();

        private bool liquidateAllPositions;
        private bool monthlyRebalance;

        private decimal riskFreeRetun;

        public override void Initialize()
        {
            // Set the basic algorithm parameters.
            SetStartDate(2008, 06, 01);
            SetEndDate(2017, 03, 30);
            SetCash(100000);

            var brokerage = forexMarket == "fxcm" ? BrokerageName.FxcmBrokerage : BrokerageName.OandaBrokerage;
            SetBrokerageModel(brokerage);

            AddData<QuandlUSTeasuryYield>(riskFreeReturnQuandlCode, Resolution.Daily);

            //foreach (var ticker in comoditiesFuturesTickers)
            //    symbols.Add(AddFuture(ticker, Resolution.Daily).Symbol);

            foreach (var ticker in forexTickers)
            {
                var forex = AddForex(ticker, Resolution.Daily, forexMarket, leverage: forexLeverage);
                symbols.Add(forex.Symbol);
                // https://en.wikipedia.org/wiki/Coefficient_of_variation
                forex.VolatilityModel =
                    new IndicatorVolatilityModel<IndicatorDataPoint>(
                        STD(forex.Symbol, volatilityWindow, Resolution.Daily)
                            .Over(SMA(forex.Symbol, volatilityWindow, Resolution.Daily)));
            }

            Schedule.On(DateRules.MonthStart(), TimeRules.At(0, 0), () =>
            {
                UpdateAssetsReturns();
                liquidateAllPositions = true;
            });

            SetWarmUp(TimeSpan.FromDays(volatilityWindow));
        }

        public override void OnData(Slice slice)
        {
            if (IsWarmingUp) return;

            if (liquidateAllPositions)
            {
                // Liquidate all existing holdings.
                Liquidate();
                liquidateAllPositions = false;
                monthlyRebalance = true;
            }

            if (monthlyRebalance && !Portfolio.Invested)
            {
                var assetsToLong = excessReturns.Where(s => s.Value > 0m)
                    .ToDictionary(pair => pair.Key,
                        pair => Securities[pair.Key].VolatilityModel.Volatility);
                var assetsToShort = excessReturns.Where(s => s.Value < 0m)
                    .ToDictionary(pair => pair.Key,
                        pair => Securities[pair.Key].VolatilityModel.Volatility);

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

        private void UpdateAssetsReturns()
        {
            var dateRequest = new DateTime(Time.Year - 1, Time.Month, Time.Day);
            // I ask for some days before just in case the selected day hasn't historical prices record.
            var history = History(symbols, dateRequest.AddDays(-5), dateRequest.AddDays(1), Resolution.Daily);
            foreach (var symbol in symbols)
                try
                {
                    var slice = history.Last(s => s.ContainsKey(symbol));
                    excessReturns[symbol] = (Securities[symbol].Price / slice[symbol].Price - 1m) * 100m -
                                            riskFreeRetun;
                }
                catch (Exception e)
                {
                    Console.WriteLine(symbol, " hasn't data to estimate excess returns.");
                    excessReturns[symbol] = 0m;
                }
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