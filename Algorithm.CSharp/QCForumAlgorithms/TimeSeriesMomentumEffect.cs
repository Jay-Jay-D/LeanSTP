﻿using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Data.Custom;
using QuantConnect.Indicators;
using QuantConnect.Orders;
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
        #region Algorithm Parameters

        private const decimal maxExposure = 0.8m;
        private readonly string forexMarket = "oanda";
        private readonly string cfdMarket = "oanda";

        private const int cfdLeverage = 10;
        private const decimal forexLeverage = 20m;

        private const int volatilityWindow = 30;

        #endregion

        #region Investment Universe

        /*
         The investment universe consists of:
            - 22 commodity futures
            - 12 cross-currency pairs (with 9 underlying currencies)
            - 9 developed equity indexes
            - 4 developed government bond futures. 
        */

        // The 10 year maturity Treasury Yield Curve is used as a proxy of the risk free rate.
        private const string riskFreeReturnQuandlCode = "USTREASURY/YIELD";

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
            "USDCNH", "NZDUSD", "EURJPY", "EURCHF", "EURGBP", "GBPJPY"
        };

        private readonly string[] cfdTickers =
        {
            "AU200AUD", // Australia 200 - Australian Dollar
            "DE30EUR", // Germany 30 - Euro Dollar
            "EU50EUR", // Europe 50 - Euro Dollar
            "CH20CHF", // Swiss 20 - Swiss Frank
            "FR40EUR", // France 40 - Euro Dollar
            "HK33HKD", // Hong Kong 33 - Hk Dollar
            "JP225USD", // Japan 225 - Us Dollar
            "UK100GBP", // Uk 100 - English Pound
            "SPX500USD" // S&p 500 - Us Dollar
        };

        private readonly string[] bondsFuturesTickers =
        {
            "SCF/CME_TU1_ON", // CBOT 2-year US Treasury Note Futures
            "SCF/CME_FV1_ON", // CBOT 5-year US Treasury Note Futures
            "SCF/CME_TY1_ON", // CBOT 10-year US Treasury Note Futures
            "SCF/CME_US2_ON", // CBOT 30-year US Treasury Bond Futures
            "SCF/EUREX_FGBL2_EN", // EUREX Euro-Bund Futures (German debt security)
            "SCF/EUREX_FBTP2_EN", // EUREX Euro-BTP Futures (Italian debt security)
            "SCF/EUREX_FOAT2_EN" // EUREX Euro-OAT Futures (French debt security)
        };

        #endregion

        #region Fields

        private readonly List<Symbol> symbols = new List<Symbol>();
        private readonly Dictionary<Symbol, decimal> excessReturns = new Dictionary<Symbol, decimal>();

        private readonly Dictionary<SecurityType, decimal> portfolioShareToAssetType =
            new Dictionary<SecurityType, decimal>();

        private bool liquidateAllPositions;
        private bool monthlyRebalance;

        private decimal riskFreeRetun;

        #endregion

        #region QCAlgorithm Methods

        public override void Initialize()
        {
            // Set the basic algorithm parameters.
            SetStartDate(2008, 06, 01);
            SetEndDate(2017, 03, 30);
            SetCash(100000);

            var brokerage = forexMarket == "fxcm" ? BrokerageName.FxcmBrokerage : BrokerageName.OandaBrokerage;
            // SetBrokerageModel(brokerage);

            AddData<QuandlUSTeasuryYield>(riskFreeReturnQuandlCode, Resolution.Daily);

            foreach (var ticker in forexTickers)
            {
                var security = AddForex(ticker, Resolution.Daily, forexMarket, leverage: forexLeverage);
                symbols.Add(security.Symbol);
            }

            foreach (var ticker in cfdTickers)
            {
                var security = AddCfd(ticker, Resolution.Daily, cfdMarket, leverage: cfdLeverage);
                symbols.Add(security.Symbol);
            }

            foreach (var symbol in symbols)
            {
                // Given the big diversity in currencies and prices,, variance can't be used as volatility proxy because the values will not be comparable.
                // That's why I use https://en.wikipedia.org/wiki/Coefficient_of_variation
                Securities[symbol].VolatilityModel =
                    new IndicatorVolatilityModel<IndicatorDataPoint>(
                        STD(symbol, volatilityWindow, Resolution.Daily)
                            .Over(SMA(symbol, volatilityWindow, Resolution.Daily)));
                if (!portfolioShareToAssetType.ContainsKey(symbol.SecurityType))
                    portfolioShareToAssetType[symbol.SecurityType] = 1;
            }


            /*
            foreach (var ticker in comoditiesFuturesTickers)
                symbols.Add(AddFuture(ticker, Resolution.Daily).Symbol);
            */

            var assets = portfolioShareToAssetType.Keys.ToArray();
            foreach (var assetType in assets)
            {
                portfolioShareToAssetType[assetType] = 1m / assets.Length;
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
                var newOrders = EstimateNewOrders();
                foreach (var order in newOrders)
                {
                    var unitValue = new MarketOrder(order.Symbol, 1, Time).GetValue(Securities[order.Symbol]);
                    if (unitValue == 0) continue;
                    var orderValue = Portfolio.TotalPortfolioValue * order.TargetHolding;
                    var quantity = (int) (orderValue / unitValue);
                    if (quantity != 0)
                    {
                        MarketOrder(order.Symbol, quantity);
                    }
                    //SetHoldings(order.Symbol, order.TargetHolding);
                }
                monthlyRebalance = false;
            }
        }

        public void OnData(Quandl data)
        {
            riskFreeRetun = data.Price;
        }

        #endregion

        #region Auxiliary Methods

        private List<SecuritiesOrders> EstimateNewOrders()
        {
            var orders = new List<SecuritiesOrders>();

            var volatilitySumByAsset = from s in symbols
                group s by s.SecurityType
                into grouped
                select new
                {
                    AssetType = grouped.Key,
                    VolatilitySum = grouped.Sum(s => Securities[s].VolatilityModel.Volatility)
                };

            foreach (var symbol in symbols)
            {
                var volatility = Securities[symbol].VolatilityModel.Volatility;
                var weightedVolatility = volatility / volatilitySumByAsset
                                             .First(v => v.AssetType == symbol.SecurityType)
                                             .VolatilitySum;
                var leverage = Securities[symbol].Leverage;
                var portfolioShareAsAsset = portfolioShareToAssetType[symbol.SecurityType];
                var orderDirection = Math.Sign(excessReturns[symbol]);
                var targetHoldings = maxExposure * portfolioShareAsAsset * orderDirection * weightedVolatility *
                                     leverage;
                orders.Add(new SecuritiesOrders
                {
                    Symbol = symbol,
                    Direction = (EntryMarketDirection) orderDirection,
                    TargetHolding = targetHoldings
                });
            }
            return orders;
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
                    Console.WriteLine(symbol + " hasn't data to estimate excess returns.");
                    excessReturns[symbol] = 0m;
                }
        }

        #endregion
    }
}