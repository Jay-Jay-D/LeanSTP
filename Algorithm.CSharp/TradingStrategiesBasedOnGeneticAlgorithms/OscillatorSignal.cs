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
        CrossUpperThresholdFromBelow = 1,
        AboveUpperThreshold = 2,
        CrossUpperThresholdFromAbove = 3
    }

    public struct OscillatorThresholds
    {
        public decimal Lower;
        public decimal Upper;
    }

    public class OscillatorSignal : ITechnicalIndicatorSignal
    {
        public delegate void DinamicIndicatorUdateHandler();

        private decimal _previousIndicatorValue;
        private OscillatorSignals _previousSignal;
        private OscillatorThresholds _thresholds;

        public OscillatorSignal(dynamic indicator, OscillatorThresholds thresholds)
        {
            SetUpClass(ref indicator, ref thresholds);
        }

        public OscillatorSignal(dynamic indicator)
        {
            var defaultThresholds = new OscillatorThresholds {Lower = 30, Upper = 70};
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
            OscillatorSignals actualSignal;
            var actualPositionSignal = GetActualPositionSignal(updated);
            if (!Indicator.IsReady)
            {
                _previousIndicatorValue = updated.Value;
                _previousSignal = actualPositionSignal;
                Signal = _previousSignal;
                return;
            }

            var indicatorCurrentValue = updated;
            var previousSignalInt = (int) _previousSignal;
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
                if (Math.Abs(previousSignalInt) > 1)
                {
                    actualSignal = (OscillatorSignals) (Math.Sign(previousSignalInt) * 2);
                }
                else
                {
                    actualSignal = (OscillatorSignals) (Math.Sign(previousSignalInt) * 3);
                }
            }

            _previousIndicatorValue = updated.Value;
            _previousSignal = actualSignal;
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

        private void SetUpClass(ref dynamic indicator, ref OscillatorThresholds thresholds)
        {
            _thresholds = thresholds;
            Indicator = indicator;
            indicator.Updated += new IndicatorUpdatedHandler(Indicator_Updated);
        }
    }
}