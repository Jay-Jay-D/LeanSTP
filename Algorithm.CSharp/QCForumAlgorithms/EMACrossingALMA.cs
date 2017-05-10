using System;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    /// Basic strategy as detailed in https://tradingsim.com/blog/5-strategies-day-trading-arnaud-legoux-moving-average/
    /// as "Using EMA Buy/Sell Signals"
    /// </summary>
    /// <seealso cref="QuantConnect.Algorithm.QCAlgorithm" />
    public class EMACrossingALMA : QCAlgorithm
    {
        private ArnaudLegouxMovingAverage alma;
        private int almaPeriod = 50;
        private ExponentialMovingAverage fastEma;
        private readonly int fastEmaPeriod = 15;

        private CrossingMovingAverages flag;

        private ExponentialMovingAverage slowEma;
        private readonly int slowEmaPeriod = 15;
        private Symbol symbol;
        private CrossingMovingAverages trigger;

        public override void Initialize()
        {
            SetStartDate(2010, 01, 01);
            SetEndDate(2017, 03, 30);
            SetCash(100000);

            symbol = AddEquity("AAPL", Resolution.Daily).Symbol;
            //symbol = AddForex("EURUSD", Resolution.Daily).Symbol;

            fastEma = EMA(symbol, fastEmaPeriod);
            slowEma = EMA(symbol, slowEmaPeriod);

            alma = ALMA(symbol, 50, 6);

            flag = new CrossingMovingAverages(fastEma, slowEma);
            trigger = new CrossingMovingAverages(slowEma, alma);
        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(symbol) || !trigger.IsReady) return;

            if (trigger.Signal == CrossingMovingAveragesSignals.FastCrossSlowFromAbove &&
                flag.Signal == CrossingMovingAveragesSignals.Bearish
                || trigger.Signal == CrossingMovingAveragesSignals.FastCrossSlowFromBelow &&
                flag.Signal == CrossingMovingAveragesSignals.Bullish)
            {
                if (Portfolio[symbol].IsLong && trigger.Signal == CrossingMovingAveragesSignals.FastCrossSlowFromAbove
                    || Portfolio[symbol].IsShort && trigger.Signal ==
                    CrossingMovingAveragesSignals.FastCrossSlowFromBelow)
                {
                    Liquidate(symbol);
                }
                else if (!Portfolio[symbol].Invested)
                {
                    var signal = trigger.Signal;
                    SetHoldings(symbol, 1 * Math.Sign((int) signal));
                }
            }
        }

        public override void OnEndOfDay()
        {
            Plot("ALMA strategy", "AAPL", Securities[symbol].Close);
            if (fastEma.IsReady) Plot("ALMA strategy", "Fast EMA", fastEma.Current.Value);
            if (slowEma.IsReady) Plot("ALMA strategy", "Slow EMA", slowEma.Current.Value);
            if (alma.IsReady) Plot("ALMA strategy", "ALMA strategy", alma.Current.Value);
        }
    }
}