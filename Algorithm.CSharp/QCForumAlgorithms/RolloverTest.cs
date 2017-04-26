using System;
using System.Collections;
using System.Collections.Generic;
using QuantConnect.Scheduling;
using QuantConnect.Securities;
using NodaTime;
using QuantConnect.Data;
using QuantConnect.Data.Market;
//using QuantConnect.Models;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using QuantConnect.Algorithm;
using QuantConnect.Indicators;


namespace QuantConnect.Algorithm.CSharp
{
    public class RolloverTest : QCAlgorithm
    {
        #region Variables
        //
        Symbol symbol = QuantConnect.Symbol
            .Create("EURUSD", SecurityType.Forex, Market.Oanda);
        //   
        private int candlesBack = 30; // Default setting for CandlesBack
        private int rSIPeriod = 21; // Default setting for RSIPeriod
        private int momentumPeriod = 20; // Default setting for MomentumPeriod
        private decimal firstTradeLots = 1M; // Default setting for FirstTradeLots
        private decimal secondTradeLots = 3M; // Default setting for SecondTradeLots
        private int pipsToTrade2 = 20; // Default setting for PipsToTrade2
        private decimal firstTradeProfitDollars = 1.000M; // Default setting for FirstTradeProfitDollars
        private decimal secondTradeProfitDollars = 1.000M; // Default setting for SecondTradeProfitDollars
        private decimal stopLossInDollars = 0M;
        private bool breakEven = false;
        private decimal breakEvenAtProfit = 50M;
        // User defined variables (add any user defined variables below)
        private RelativeStrengthIndex _rsi;
        private Momentum _mom;
        private RollingWindow<int> mp, cc;
        public RollingWindow<IndicatorDataPoint> _rsiwindow;
        public RollingWindow<IndicatorDataPoint> _momwindow;
        RollingWindow<QuoteBar> _datawindow;
        //
        public DateTime myTime0, myTime1, theTime;
        //
        private int lastKDS = 0, lastKDB = 0, tradesTotal = 0;
        private decimal priceForSecond = 0, profits = 0;
        private bool _breakEven = false, buysNotAllowed = false, sellsNotAllowed = false;
        private string orderType = "";
        //
        int CurrentBar = 0;
        int LastBar = 0;
        decimal TickSize = 0.0001M;
        #endregion

        public override void Initialize()
        {
            //Initialize
            SetStartDate(2013, 1, 1);
            SetEndDate(2014, 12, 31);
            SetCash(25000);
            //
            var forex = AddForex(symbol, Resolution.Minute);
            //
            var pipette = Securities[symbol].SymbolProperties.MinimumPriceVariation;
            // pipetteEURUSD is 0.00001
            // pipetteUSDJPY is 0.001
            TickSize = pipette * 10M;
            firstTradeLots = firstTradeLots * (int)Securities[symbol].SymbolProperties.LotSize;
            secondTradeLots = secondTradeLots * (int)Securities[symbol].SymbolProperties.LotSize;
            //
            _rsi = RSI(symbol, rSIPeriod, MovingAverageType.Simple, Resolution.Minute);
            _mom = MOM(symbol, momentumPeriod, Resolution.Minute);
            //
            _rsiwindow = new RollingWindow<IndicatorDataPoint>(candlesBack);
            _momwindow = new RollingWindow<IndicatorDataPoint>(candlesBack);
            _datawindow = new RollingWindow<QuoteBar>(candlesBack);
            //
            // Here is where the RollingWindow is updated with the latest  observation.
            _rsi.Updated += (object sender, IndicatorDataPoint updated) =>
            {
                _rsiwindow.Add(updated);
            };
            //
            // Here is where the RollingWindow is updated with the latest  observation.
            _mom.Updated += (object sender, IndicatorDataPoint updated) =>
            {
                _momwindow.Add(updated);
            };
            //
            mp = new RollingWindow<int>(candlesBack);
            cc = new RollingWindow<int>(candlesBack);
            //
            myTime0 = new DateTime(0);
            myTime1 = new DateTime(0);
            theTime = new DateTime(0);
            //
            Name = "";
            //
        }

        private void OnData(TradeBars data)
        {
            if (!_rsiwindow.IsReady) return;
            if (!_momwindow.IsReady) return;
            if (!_datawindow.IsReady) return;
            //
            TradeBar SymbolTradebar = data[symbol];
            //
            mp.Add(Portfolio[symbol].IsLong ? 1 : Portfolio[symbol].IsShort ? -1 : 0);
            cc.Add(Portfolio[symbol].Quantity);
            mp[0] = Portfolio[symbol].IsLong ? 1 : Portfolio[symbol].IsShort ? -1 : 0;
            cc[0] = Portfolio[symbol].Quantity;
            //
            myTime1 = myTime0;
            myTime0 = data[symbol].Time;
            //
            profits = Portfolio.TotalUnrealizedProfit;
            //
            LastBar = CurrentBar;
            CurrentBar++;
            //
            if (CurrentBar < candlesBack + 10)
                return;
            //
            if (mp[0] > 0)
            {
                if (mp[1] < 1 || cc[1] < cc[0])
                    tradesTotal++;
                priceForSecond = Securities[symbol].Holdings.AveragePrice - pipsToTrade2 * TickSize;
            }
            else if (mp[0] < 0)
            {
                if (mp[1] > -1 || cc[1] < cc[0])
                    tradesTotal++;
                priceForSecond = Securities[symbol].Holdings.AveragePrice + pipsToTrade2 * TickSize;
            }
            //
            if (tradesTotal == 0)
            {
                _breakEven = false;
                orderType = "BuySell";
            }
            else if (tradesTotal == 1 && profits >= firstTradeProfitDollars)
            {
                CloseAllTrades();
                _breakEven = false;
                tradesTotal = 0;
                orderType = "BuySell";
                theTime = myTime1;
            }
            else if (tradesTotal == 2 && profits >= secondTradeProfitDollars)
            {
                CloseAllTrades();
                _breakEven = false;
                tradesTotal = 0;
                orderType = "BuySell";
                theTime = myTime1;
            }
            //
            if (stopLossInDollars > 0 && tradesTotal > 0 && profits <= -stopLossInDollars)
            {
                CloseAllTrades();
                _breakEven = false;
                tradesTotal = 0;
                orderType = "BuySell";
                theTime = myTime1;
            }
            //
            if (breakEven && !_breakEven && profits >= breakEvenAtProfit)
                _breakEven = true;
            //
            if (_breakEven && profits <= 0)
            {
                CloseAllTrades();
                _breakEven = false;
                tradesTotal = 0;
                orderType = "BuySell";
                theTime = myTime1;
            }
            //
            if (tradesTotal < 2)
            {
                if (theTime < myTime1)
                {
                    if (!buysNotAllowed && ((orderType == "BuySell" && tradesTotal == 0) || (orderType == "Buy" && tradesTotal == 1 && data[symbol].Close <= priceForSecond)) && RSIBuyCheck(1, SymbolTradebar))
                        OpenTrade("Buy", data[symbol].Close);
                    if (!sellsNotAllowed && ((orderType == "BuySell" && tradesTotal == 0) || (orderType == "Sell" && tradesTotal == 1 && data[symbol].Close >= priceForSecond)) && RSISellCheck(1, SymbolTradebar))
                        OpenTrade("Sell", data[symbol].Close);
                    theTime = myTime1;
                }
            }
        }

        private void OpenTrade(string type, decimal Close)
        {
            int lots = (int)firstTradeLots;
            //
            if (tradesTotal == 1)
                lots = (int)secondTradeLots;
            //
            if (type == "Buy")
            {
                MarketOrder(symbol, lots);
                priceForSecond = Close - pipsToTrade2 * TickSize;
                orderType = "Buy";
            }
            else if (type == "Sell")
            {

                MarketOrder(symbol, -lots);
                priceForSecond = Close + pipsToTrade2 * TickSize;
                orderType = "Sell";
            }
        }

        private void CloseAllTrades()
        {
            Liquidate(symbol);
        }

        private bool RSIBuyCheck(int loc, TradeBar data)
        {
            decimal rsiMain = _rsiwindow[loc];
            int s;
            bool ob;
            decimal mom1;
            decimal mom2;
            //
            if (rsiMain > 50)
                return false;
            for (int x = loc; x <= loc + 2; x++)
            {
                if (_datawindow[x].Low < _datawindow[loc].Low)
                    return false;
            }
            for (int x = loc + 4; x < loc + candlesBack; x++)
            {
                if (_datawindow[x].Low < _datawindow[loc].Low)
                    break;
                s = x;
                for (int y = x - 2; y <= x + 2; y++)
                {
                    if (_datawindow[y].Low < _datawindow[x].Low)
                    {
                        x++;
                        break;
                    }
                }
                if (s != x)
                {
                    x--;
                    continue;
                }
                ob = false;
                for (int y = loc; y <= x; y++)
                {
                    if (_rsiwindow[y] < 30)
                    {
                        ob = true;
                        break;
                    }
                }
                if (!ob)
                    continue;
                mom1 = _momwindow[loc];
                mom2 = _momwindow[x];
                if (mom1 < mom2)
                    continue;
                lastKDB = x;
                return true;
            }
            return false;
        }

        private bool RSISellCheck(int loc, TradeBar data)
        {
            decimal rsiMain = _rsiwindow[loc];
            int s;
            bool ob;
            decimal mom1;
            decimal mom2;
            //
            if (rsiMain < 50)
                return false;
            //
            for (int x = loc; x <= loc + 2; x++)
            {
                if (_datawindow[x].High > _datawindow[loc].High)
                    return false;
            }
            //
            for (int x = loc + 4; x < loc + candlesBack; x++)
            {
                if (_datawindow[x].High > _datawindow[loc].High)
                    break;
                s = x;
                for (int y = x - 2; y <= x + 2; y++)
                {
                    if (_datawindow[y].High > _datawindow[x].High)
                    {
                        x++;
                        break;
                    }
                }
                if (s != x)
                {
                    x--;
                    continue;
                }
                ob = false;
                for (int y = loc; y <= x; y++)
                {
                    if (_rsiwindow[y] > 70)
                    {
                        ob = true;
                        break;
                    }
                }
                if (!ob)
                    continue;
                mom1 = _momwindow[loc];
                mom2 = _momwindow[x];
                if (mom1 > mom2)
                    continue;
                lastKDS = x;
                return true;
            }
            return false;
        }

        private double ToJulianDate(DateTime date)
        {
            return date.ToOADate() + 2415018.5;
        }

        #region Properties

        #endregion
    }
}