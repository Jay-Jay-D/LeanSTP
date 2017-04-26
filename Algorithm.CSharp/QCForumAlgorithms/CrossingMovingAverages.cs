using System;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public enum CrossingMovingAveragesSignals
    {
        Bullish = 1,
        FastCrossSlowFromAbove = -2,
        Bearish = -1,
        FastCrossSlowFromBelow = 2,
    }

    public class CrossingMovingAverages
    {
        CompositeIndicator<IndicatorDataPoint> _moving_average_difference;
        public CrossingMovingAveragesSignals Signal { get; private set; }
        private bool _isReady;
        int _lastSignal;


        public bool IsReady
        {
            get { return _isReady; }
        }


        public CrossingMovingAverages(IndicatorBase<IndicatorDataPoint> fast_moving_average, IndicatorBase<IndicatorDataPoint> slow_moving_average)
        {
            _moving_average_difference = fast_moving_average.Minus(slow_moving_average);
            _moving_average_difference.Updated += ma_Updated;
        }

        private void ma_Updated(object sender, IndicatorDataPoint updated)
        {
            if (!_isReady)
            {
                _isReady = _moving_average_difference.Right.IsReady;
                return;
            }
            var actualSignal = Math.Sign(_moving_average_difference);
            if (actualSignal == _lastSignal || _lastSignal == 0)
            {
                Signal = (CrossingMovingAveragesSignals)actualSignal;
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
