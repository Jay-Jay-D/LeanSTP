using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Orders;
using QuantConnect.Parameters;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     Every month, the investor considers whether the excess return of each asset over the past 12
    ///     months is positive or negative and goes long on the contract if it is positive and short if
    ///     negative. The position size is set to be inversely proportional to the instrument’s volatility.
    ///     Source: http://quantpedia.com/Screener/Details/118
    /// </summary>
    /// <seealso cref="QuantConnect.Algorithm.QCAlgorithm" />
    public class ForexVanillaMomentum : QCAlgorithm
    {
        #region Investment Universe

        private readonly string[] forexTickers =
        {
            "AUDCAD", "AUDCHF", "AUDJPY", "AUDNZD", "AUDUSD", "CADCHF", "CADJPY",
            "CHFJPY", "EURAUD", "EURCAD", "EURCHF", "EURGBP", "EURJPY", "EURNOK",
            "EURNZD", "EURSEK", "EURTRY", "EURUSD", "GBPAUD", "GBPCAD", "GBPCHF",
            "GBPJPY", "GBPNZD", "GBPUSD", "NZDCAD", "NZDCHF", "NZDJPY", "NZDUSD",
            "TRYJPY", "USDCAD", "USDCHF", "USDJPY", "USDMXN", "USDNOK", "USDSEK",
            "USDTRY" // "USDCNH", "ZARJPY", "USDZAR", 
        };

        #endregion

        #region Auxiliary Methods

        private void UpdateAssetsReturns()
        {
            var dateRequest = new DateTime(Time.Year - 1, Time.Month, Time.Day);
            // I ask for some days before just in case the selected day hasn't historical prices records.
            var history = History(symbols, dateRequest.AddDays(-5), dateRequest.AddDays(1), Resolution.Daily);
            foreach (var symbol in symbols)
                try
                {
                    var slice = history.Last(s => s.ContainsKey(symbol));
                    excessReturns[symbol] = Securities[symbol].Price / slice[symbol].Price - 1m;
                }
                catch (Exception e)
                {
                    Console.WriteLine(symbol + " hasn't data to estimate excess returns.");
                    excessReturns[symbol] = 0m;
                }
        }

        #endregion

        #region Algorithm Parameters

        [Parameter("broker")] private readonly string forexMarket = "oanda";

        [Parameter("max_exposure")] private readonly decimal maxExposure = 0.5m;

        [Parameter("leverage")] private readonly int leverage = 20;

        [Parameter("initial_cash")] private readonly int cash = 10000;

        [Parameter("pairs_to_trade")] private readonly int pairsToTrade = 3;

        #endregion

        #region Fields

        private readonly List<Symbol> symbols = new List<Symbol>();
        private readonly Dictionary<Symbol, decimal> excessReturns = new Dictionary<Symbol, decimal>();

        private DateTime monthFirstTradableDay;
        private DateTime monthLastTradableDay;

        private Symbol[] symbolsToShort;
        private Symbol[] symbolsToLong;
        private bool readytoTrade;

        #endregion

        #region QCAlgorithm Methods

        public override void Initialize()
        {
            // Set the basic algorithm parameters.
            SetStartDate(2010, 01, 01);
            SetEndDate(2017, 03, 30);

            SetCash(cash);

            var brokerage = forexMarket == "fxcm" ? BrokerageName.FxcmBrokerage : BrokerageName.OandaBrokerage;
            SetBrokerageModel(brokerage);

            foreach (var ticker in forexTickers)
            {
                var security = AddForex(ticker, Resolution.Daily, forexMarket, leverage: leverage);
                var symbol = security.Symbol;
                symbols.Add(security.Symbol);
            }

            Schedule.On(DateRules.MonthStart(), TimeRules.At(0, 0), () =>
            {
                var monthLastDay = new DateTime(Time.Year, Time.Month, DateTime.DaysInMonth(Time.Year, Time.Month));
                var monthTradingdays = TradingCalendar
                    .GetDaysByType(TradingDayType.BusinessDay, Time, monthLastDay)
                    .ToArray();

                monthFirstTradableDay = monthTradingdays.First().Date;
                monthLastTradableDay = monthTradingdays.Last().Date;
                UpdateAssetsReturns();
                symbolsToShort = excessReturns.OrderBy(pair => pair.Value)
                    .Take(pairsToTrade)
                    .Select(pair => pair.Key)
                    .ToArray();
                symbolsToLong = excessReturns.OrderBy(pair => pair.Value)
                    .Skip(excessReturns.Count - pairsToTrade)
                    .Select(pair => pair.Key)
                    .ToArray();
                readytoTrade = true;
            });

            Schedule.On(DateRules.EveryDay(), TimeRules.At(9, 00), () =>
            {
                if (!readytoTrade) return;
                var symbolsToTrade = symbolsToLong.Concat(symbolsToShort);
                foreach (var symbol in symbolsToTrade)
                {
                    if (Time.Date == monthFirstTradableDay)
                    {
                        if (Portfolio[symbol].Invested)
                            throw new NotImplementedException("The asset wasn't liquidated previously!!");
                        var unitValue = new MarketOrder(symbol, 1, Time).GetValue(Securities[symbol]);
                        if (unitValue == 0) return;
                        var orderValue = maxExposure * Portfolio.TotalPortfolioValue * leverage /
                                         (2 * pairsToTrade);
                        var quantity = (int) (Math.Sign(excessReturns[symbol]) * orderValue / unitValue);
                        if (quantity != 0)
                        {
                            MarketOrder(symbol, quantity);
                        }
                    }
                }
            });

            Schedule.On(DateRules.EveryDay(), TimeRules.At(16, 55), () =>
            {
                if (Time.Date == monthLastTradableDay) Liquidate();
            });
        }

        public override void OnEndOfDay()
        {
            Debug(string.Format("=== EOD {0} ===", Time.Date.ToShortDateString()));
            //Log(string.Format("=== EOD {0} ===", Time.Date.ToShortDateString()));
        }

        #endregion
    }
}