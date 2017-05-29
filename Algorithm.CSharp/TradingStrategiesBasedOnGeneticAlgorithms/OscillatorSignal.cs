using System;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     Possibles states of an oscillator respect to its thresholds.
    /// </summary>
    public enum OscillatorSignals
    {
        CrossLowerThresholdFromAbove = -3,
        BellowLowerThreshold = -2,
        CrossLowerThresholdFromBelow = -1,
        BetweenThresholds = 0,
        CrossUpperThresholdFromBelow = 3,
        AboveUpperThreshold = 2,
        CrossUpperThresholdFromAbove = 1
    }

    public struct OscillatorThresholds
    {
        public decimal Lower;
        public decimal Upper;
    }

    /// <summary>
    ///     This class keeps track of an oscillator respect to its thresholds and updates an <see cref="OscillatorSignal" />
    ///     for each given state.
    /// </summary>
    /// <seealso cref="QuantConnect.Algorithm.CSharp.ITechnicalIndicatorSignal" />
    public class OscillatorSignal : ITechnicalIndicatorSignal
    {
        private decimal _previousIndicatorValue;
        private OscillatorSignals _previousSignal;
        private OscillatorThresholds _thresholds;
        private TradeRuleDirection _tradeRuleDirection;

        /// <summary>
        ///     Initializes a new instance of the <see cref="OscillatorSignal" /> class.
        /// </summary>
        /// <param name="indicator">The indicator.</param>
        /// <param name="thresholds">The thresholds.</param>
        /// <param name="tradeRuleDirection">
        ///     The trade rule direction. Only used if the instance will be part of a
        ///     <see cref="TradingRule" /> class
        /// </param>
        /// <remarks>The oscillator must be registered BEFORE being used by this constructor.</remarks>
        public OscillatorSignal(dynamic indicator, OscillatorThresholds thresholds,
            TradeRuleDirection tradeRuleDirection)
        {
            SetUpClass(ref indicator, ref thresholds, tradeRuleDirection);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OscillatorSignal" /> class.
        /// </summary>
        /// <param name="indicator">The indicator.</param>
        /// <param name="thresholds">The thresholds.</param>
        /// <remarks>The oscillator must be registered BEFORE being used by this constructor.</remarks>
        public OscillatorSignal(dynamic indicator, OscillatorThresholds thresholds)
        {
            SetUpClass(ref indicator, ref thresholds);
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="OscillatorSignal" /> class.
        /// </summary>
        /// <param name="indicator">The indicator.</param>
        /// <remarks>The oscillator must be registered BEFORE being used by this constructor.</remarks>
        public OscillatorSignal(dynamic indicator)
        {
            var defaultThresholds = new OscillatorThresholds {Lower = 20, Upper = 80};
            SetUpClass(ref indicator, ref defaultThresholds);
        }

        /// <summary>
        ///     The underlying indicator, must be an oscillator.
        /// </summary>
        public dynamic Indicator { get; private set; }

        /// <summary>
        ///     Gets the actual state of the oscillator.
        /// </summary>
        public OscillatorSignals Signal { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether this instance is ready.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is ready; otherwise, <c>false</c>.
        /// </value>
        public bool IsReady
        {
            get { return Indicator.IsReady; }
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
                        signal = Signal == OscillatorSignals.CrossLowerThresholdFromBelow;
                        break;

                    case TradeRuleDirection.ShortOnly:
                        signal = Signal == OscillatorSignals.CrossUpperThresholdFromAbove;
                        break;
                }
            }
            return signal;
        }

        /// <summary>
        ///     Updates the <see cref="Signal" /> status.
        /// </summary>
        private void Indicator_Updated(object sender, IndicatorDataPoint updated)
        {
            var actualPositionSignal = GetActualPositionSignal(updated);
            if (!Indicator.IsReady)
            {
                _previousIndicatorValue = updated.Value;
                _previousSignal = actualPositionSignal;
                Signal = _previousSignal;
                return;
            }

            var actualSignal = GetActualSignal(_previousSignal, actualPositionSignal);

            Signal = actualSignal;
            _previousIndicatorValue = updated.Value;
            _previousSignal = actualSignal;
        }

        /// <summary>
        ///     Gets the actual position respect to the thresholds.
        /// </summary>
        /// <param name="indicatorCurrentValue">The indicator current value.</param>
        /// <returns></returns>
        private OscillatorSignals GetActualPositionSignal(decimal indicatorCurrentValue)
        {
            var positionSignal = OscillatorSignals.BetweenThresholds;
            if (indicatorCurrentValue > _thresholds.Upper)
            {
                positionSignal = OscillatorSignals.AboveUpperThreshold;
            }
            else if (indicatorCurrentValue < _thresholds.Lower)
            {
                positionSignal = OscillatorSignals.BellowLowerThreshold;
            }
            return positionSignal;
        }

        /// <summary>
        ///     Gets the actual signal from the actual position respect to the thresholds and the last signal.
        /// </summary>
        /// <param name="previousSignal">The previous signal.</param>
        /// <param name="actualPositionSignal">The actual position signal.</param>
        /// <returns></returns>
        private OscillatorSignals GetActualSignal(OscillatorSignals previousSignal,
            OscillatorSignals actualPositionSignal)
        {
            OscillatorSignals actualSignal;
            var previousSignalInt = (int) previousSignal;
            var actualPositionSignalInt = (int) actualPositionSignal;

            if (actualPositionSignalInt == 0)
            {
                if (Math.Abs(previousSignalInt) > 1)
                {
                    actualSignal = (OscillatorSignals) Math.Sign(previousSignalInt);
                }
                else
                {
                    actualSignal = OscillatorSignals.BetweenThresholds;
                }
            }
            else
            {
                if (previousSignalInt * actualPositionSignalInt <= 0 ||
                    Math.Abs(previousSignalInt + actualPositionSignalInt) == 3)
                {
                    actualSignal = (OscillatorSignals) (Math.Sign(actualPositionSignalInt) * 3);
                }
                else
                {
                    actualSignal = (OscillatorSignals) (Math.Sign(actualPositionSignalInt) * 2);
                }
            }
            return actualSignal;
        }

        /// <summary>
        ///     Sets up class.
        /// </summary>
        /// <param name="indicator">The indicator.</param>
        /// <param name="thresholds">The thresholds.</param>
        /// <param name="tradeRuleDirection">The trade rule direction.</param>
        private void SetUpClass(ref dynamic indicator, ref OscillatorThresholds thresholds,
            TradeRuleDirection? tradeRuleDirection = null)
        {
            _thresholds = thresholds;
            Indicator = indicator;
            indicator.Updated += new IndicatorUpdatedHandler(Indicator_Updated);
            if (tradeRuleDirection != null) _tradeRuleDirection = (TradeRuleDirection) tradeRuleDirection;
        }
    }
}