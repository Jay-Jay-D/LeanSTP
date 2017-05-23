using System;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public enum CrossingMovingAveragesSignals
    {
        Bullish = 1,
        FastCrossSlowFromAbove = -2,
        Bearish = -1,
        FastCrossSlowFromBelow = 2
    }

    public class CrossingMovingAverages : ITechnicalIndicatorSignal
    {
        private int _lastSignal;
        private readonly CompositeIndicator<IndicatorDataPoint> _moving_average_difference;
        private TradeRuleDirection _tradeRuleDirection;

        public CrossingMovingAverages(IndicatorBase<IndicatorDataPoint> fast_moving_average,
            IndicatorBase<IndicatorDataPoint> slow_moving_average, TradeRuleDirection? tradeRuleDirection = null)
        {
            _moving_average_difference = fast_moving_average.Minus(slow_moving_average);
            _moving_average_difference.Updated += ma_Updated;
            if (tradeRuleDirection != null) _tradeRuleDirection = (TradeRuleDirection) tradeRuleDirection;
        }

        public CrossingMovingAveragesSignals Signal { get; private set; }

        public bool IsReady { get; private set; }

        public bool GetSignal()
        {
            var signal = false;
            switch (_tradeRuleDirection)
            {
                case TradeRuleDirection.LongOnly:
                    signal = Signal == CrossingMovingAveragesSignals.FastCrossSlowFromBelow;
                    break;

                case TradeRuleDirection.ShortOnly:
                    signal = Signal == CrossingMovingAveragesSignals.FastCrossSlowFromAbove;
                    break;
            }
            return signal;
        }

        private void ma_Updated(object sender, IndicatorDataPoint updated)
        {
            if (!IsReady)
            {
                IsReady = _moving_average_difference.Right.IsReady;
                return;
            }
            var actualSignal = Math.Sign(_moving_average_difference);
            if (actualSignal == _lastSignal || _lastSignal == 0)
            {
                Signal = (CrossingMovingAveragesSignals) actualSignal;
            }
            else if (_lastSignal == -1 && actualSignal == 1)
            {
                Signal = CrossingMovingAveragesSignals.FastCrossSlowFromBelow;
            }
            else if (_lastSignal == 1 && actualSignal == -1)
            {
                Signal = CrossingMovingAveragesSignals.FastCrossSlowFromAbove;
            }

            _lastSignal = actualSignal;
        }
    }
}