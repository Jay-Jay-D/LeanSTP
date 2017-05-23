using System;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
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

    public class OscillatorSignal : ITechnicalIndicatorSignal
    {
        private decimal _previousIndicatorValue;
        private OscillatorSignals _previousSignal;
        private OscillatorThresholds _thresholds;
        private TradeRuleDirection _tradeRuleDirection;

        public OscillatorSignal(dynamic indicator, OscillatorThresholds thresholds, TradeRuleDirection tradeRuleDirection)
        {
            SetUpClass(ref indicator, ref thresholds, tradeRuleDirection);
        }



        public OscillatorSignal(dynamic indicator, OscillatorThresholds thresholds)
        {
            SetUpClass(ref indicator, ref thresholds);
        }

        public OscillatorSignal(dynamic indicator)
        {
            var defaultThresholds = new OscillatorThresholds {Lower = 20, Upper = 80};
            SetUpClass(ref indicator, ref defaultThresholds);
        }

        public dynamic Indicator { get; private set; }

        public bool IsReady
        {
            get { return Indicator.IsReady; }
        }

        public OscillatorSignals Signal { get; private set; }

        public bool GetSignal()
        {
            throw new NotImplementedException();
        }

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

            var actualSignal = GetaActualSignal(_previousSignal, actualPositionSignal);

            Signal = actualSignal;
            _previousIndicatorValue = updated.Value;
            _previousSignal = actualSignal;
        }

        private OscillatorSignals GetaActualSignal(OscillatorSignals previousSignal, OscillatorSignals actualPositionSignal)
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
                if (previousSignalInt * actualPositionSignalInt <= 0 || Math.Abs(previousSignalInt + actualPositionSignalInt)==3)
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

        private void SetUpClass(ref dynamic indicator, ref OscillatorThresholds thresholds, TradeRuleDirection? tradeRuleDirection=null)
        {
            _thresholds = thresholds;
            Indicator = indicator;
            indicator.Updated += new IndicatorUpdatedHandler(Indicator_Updated);
            if (tradeRuleDirection != null) _tradeRuleDirection = (TradeRuleDirection)tradeRuleDirection;
        }
    }
}