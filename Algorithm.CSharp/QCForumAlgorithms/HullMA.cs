using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class HullMA:QCAlgorithm
    {
        private int hullMaPeriod = 4;
        private int slowEmaPeriod = 15;

        private ExponentialMovingAverage slowEma;
        private IndicatorBase<IndicatorDataPoint> hullMa;

        private CrossingMovingAverages MovingAverageCross;
        private Symbol symbol;

        public override void Initialize()
        {
            // Set the basic algorithm parameters.
            SetStartDate(2010, 01, 01);
            SetEndDate(2017, 03, 30);
            SetCash(100000);

            symbol = AddEquity("AAPL", Resolution.Daily).Symbol;


            // ==================================
            // Hull Moving Average implementation
            var slowLWMA = LWMA(symbol, (int) (hullMaPeriod * 1m / 2));
            var fastLWMA = LWMA(symbol, hullMaPeriod);
            var HMA = new LinearWeightedMovingAverage((int) Math.Sqrt(hullMaPeriod * 1d));
            hullMa = HMA.Of(fastLWMA.Times(2).Minus(slowLWMA));
            // ==================================

            slowEma = EMA(symbol, slowEmaPeriod);

            MovingAverageCross = new CrossingMovingAverages(hullMa, slowEma);
        }

        public override void OnData(Slice slice)
        {
            if (!slice.ContainsKey(symbol) || !MovingAverageCross.IsReady) return;
            var signal = MovingAverageCross.Signal;
            if (signal == CrossingMovingAveragesSignals.FastCrossSlowFromAbove
                || signal == CrossingMovingAveragesSignals.FastCrossSlowFromBelow)
            {
                if ((Portfolio[symbol].IsLong && signal == CrossingMovingAveragesSignals.FastCrossSlowFromAbove)
                    || (Portfolio[symbol].IsShort && signal == CrossingMovingAveragesSignals.FastCrossSlowFromBelow))
                {
                    Liquidate(symbol);
                }
                else if (!Portfolio[symbol].Invested)
                {

                    SetHoldings(symbol, 1*Math.Sign((int)signal));
                }

            }
        }

        public override void OnEndOfDay()
        {
            Plot("Hull Moving Average", "AAPL", Securities[symbol].Close);
            if (hullMa.IsReady) Plot("Hull Moving Average", "Hull Moving Average", hullMa.Current.Value);
            if (slowEma.IsReady) Plot("Hull Moving Average", "EMA", slowEma.Current.Value);
        }
    }
}
