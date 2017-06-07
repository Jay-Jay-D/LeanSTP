using System;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     Possibles states of two moving averages.
    /// </summary>
    public enum CrossingMovingAveragesSignals
    {
        Bullish = 1,
        FastCrossSlowFromAbove = -2,
        Bearish = -1,
        FastCrossSlowFromBelow = 2
    }

    /// <summary>
    ///     This class keeps track of two crossing moving averages and updates a <see cref="CrossingMovingAveragesSignals" />
    ///     for each given state.
    /// </summary>
    /// <seealso cref="QuantConnect.Algorithm.CSharp.ITechnicalIndicatorSignal" />
    public class CrossingMovingAverages : ITechnicalIndicatorSignal
    {
        private readonly CompositeIndicator<IndicatorDataPoint> _moving_average_difference;
        private readonly TradeRuleDirection _tradeRuleDirection;
        private int _lastSignal;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CrossingMovingAverages" /> class.
        /// </summary>
        /// <param name="fast_moving_average">The fast moving average.</param>
        /// <param name="slow_moving_average">The slow moving average.</param>
        /// <param name="tradeRuleDirection">
        ///     The trade rule direction. Only used if the instance will be part of a
        ///     <see cref="TradingRule" /> class
        /// </param>
        /// <remarks>
        ///     Both Moving Averages must be registered BEFORE being used by this constructor.
        /// </remarks>
        public CrossingMovingAverages(IndicatorBase<IndicatorDataPoint> fast_moving_average,
            IndicatorBase<IndicatorDataPoint> slow_moving_average, TradeRuleDirection? tradeRuleDirection = null)
        {
            _moving_average_difference = fast_moving_average.Minus(slow_moving_average);
            _moving_average_difference.Updated += ma_Updated;
            if (tradeRuleDirection != null) _tradeRuleDirection = (TradeRuleDirection) tradeRuleDirection;
        }

        /// <summary>
        ///     Gets the actual state of both moving averages.
        /// </summary>
        public CrossingMovingAveragesSignals Signal { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this instance is ready.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is ready; otherwise, <c>false</c>.
        /// </value>
        public bool IsReady
        {
            get { return _moving_average_difference.IsReady; }
        }

        /// <summary>
        ///     Gets the signal. Only used if the instance will be part of a <see cref="TradingRule" /> class.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the actual <see cref="Signal" /> correspond with the instance <see cref="TradeRuleDirection" />.
        ///     <c>false</c>
        ///     otherwise.
        /// </returns>
        public bool GetSignal()
        {
            var signal = false;
            if (IsReady)
            {
                switch (_tradeRuleDirection)
                {
                    case TradeRuleDirection.LongOnly:
                        signal = Signal == CrossingMovingAveragesSignals.FastCrossSlowFromBelow;
                        break;

                    case TradeRuleDirection.ShortOnly:
                        signal = Signal == CrossingMovingAveragesSignals.FastCrossSlowFromAbove;
                        break;
                }
            }
            return signal;
        }

        private void ma_Updated(object sender, IndicatorDataPoint updated)
        {
            if (!IsReady)
            {
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