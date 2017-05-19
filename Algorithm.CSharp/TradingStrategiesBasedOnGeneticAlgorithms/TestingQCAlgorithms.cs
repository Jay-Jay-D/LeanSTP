using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public abstract class TradingStrategiesBasedOnGeneticAlgorithmsBaseForTesting : QCAlgorithm
    {
        public Symbol spy;
        public override void Initialize()
        {
            SetStartDate(2014, 10, 01);
            SetEndDate(2014, 12, 30);
            SetCash(10000);
            spy = AddEquity("SPY", Resolution.Daily).Symbol;
        }
    }

    public class WriteSomeLogToTestOscillatorBehavior : TradingStrategiesBasedOnGeneticAlgorithmsBaseForTesting
    {
        private bool isFirst = true;
        private RelativeStrengthIndex rsi;
        private StringBuilder CsvOutput;

        public override void OnData(Slice slice)
        {
            if (isFirst)
            {
                rsi = RSI(spy, 14, MovingAverageType.Wilders);
                CsvOutput = new StringBuilder("time,SPY,RSI(14)\n");
                isFirst = false;
            }

            if (rsi.IsReady)
            {
                CsvOutput.AppendLine(string.Format("{0},{1},{2}", Time.Date.ToShortDateString(), slice[spy].Price, rsi.Current.Value));
            }
        }

        public override void OnEndOfAlgorithm()
        {
            File.WriteAllText(@"C:\Users\jjd\Desktop\test.csv", CsvOutput.ToString());
        }
    }

    public class RsiInstantiatedAndRegisteredCorrectly : TradingStrategiesBasedOnGeneticAlgorithmsBaseForTesting
    {
        private bool isFirst = true;
        private RelativeStrengthIndex actualRsi;
        private OscillatorSignal oscillatorRsi;

        public override void OnData(Slice slice)
        {
            if (isFirst)
            {
                actualRsi = RSI(spy, 14, MovingAverageType.Wilders);
                oscillatorRsi = new OscillatorSignal(actualRsi);
                isFirst = false;
            }

            if (actualRsi.IsReady)
            {
                var rsiIsCorrectlyInstantitadedAndRegistered = oscillatorRsi.Indicator.IsReady;
                if (RuntimeStatistics.ContainsKey("RsiIsCorrectlyInstantitadedAndRegistered"))
                {
                    var lastTest = bool.Parse(RuntimeStatistics["RsiIsCorrectlyInstantitadedAndRegistered"]);
                    RuntimeStatistics["RsiIsCorrectlyInstantitadedAndRegistered"] =
                        (lastTest && rsiIsCorrectlyInstantitadedAndRegistered).ToString();
                }
                else
                {
                    RuntimeStatistics["RsiIsCorrectlyInstantitadedAndRegistered"] = rsiIsCorrectlyInstantitadedAndRegistered.ToString();

                }

                var rsiIsCorrectlyEstimated = actualRsi.Current.Value == oscillatorRsi.Indicator.Current.Value;
                if (RuntimeStatistics.ContainsKey("RsiIsCorrectlyEstimated"))
                {
                    var lastTest = bool.Parse(RuntimeStatistics["RsiIsCorrectlyEstimated"]);
                    RuntimeStatistics["RsiIsCorrectlyEstimated"] =
                        (lastTest && rsiIsCorrectlyEstimated).ToString();
                }
                else
                {
                    RuntimeStatistics["RsiIsCorrectlyEstimated"] = rsiIsCorrectlyEstimated.ToString();

                }
            }
        }
    }

    public class OscillatorGivesTheSignalsCorrectly : TradingStrategiesBasedOnGeneticAlgorithmsBaseForTesting
    {
        private bool isFirst = true;
        private OscillatorSignal oscillatorRsi;

        public override void OnEndOfAlgorithm()
        {
            base.OnEndOfAlgorithm();
        }

        public override void OnData(Slice slice)
        {
            if (isFirst)
            {
                var injectedRsi = RSI(spy, 14, MovingAverageType.Wilders);
                var threshodls = new OscillatorThresholds { Lower = 40, Upper = 60 };
                oscillatorRsi = new OscillatorSignal(injectedRsi, threshodls);
                isFirst = false;
            }

            if (Time.Date == new DateTime(2014, 10, 30) || Time.Date == new DateTime(2014, 12, 12) || Time.Date == new DateTime(2014, 12, 19))
            {
                var betweenTheThresholds = oscillatorRsi.Signal == OscillatorSignals.BetweenThresholds;
                if (RuntimeStatistics.ContainsKey("BetweenTheThresholdsCorrectlyEstimated"))
                {
                    var lastTest = bool.Parse(RuntimeStatistics["BetweenTheThresholdsCorrectlyEstimated"]);
                    RuntimeStatistics["BetweenTheThresholdsCorrectlyEstimated"] =
                        (lastTest && betweenTheThresholds).ToString();
                }
                else
                {
                    RuntimeStatistics["BetweenTheThresholdsCorrectlyEstimated"] = betweenTheThresholds.ToString();

                }
            }

            if (Time.Date == new DateTime(2014, 10, 31))
            {
                var crossUpperThresholdFromBelow = oscillatorRsi.Signal == OscillatorSignals.CrossUpperThresholdFromBelow;
                RuntimeStatistics["CrossUpperThresholdFromBelowCorrectlyEstimated"] = crossUpperThresholdFromBelow.ToString();
            }

            if (Time.Date == new DateTime(2014, 11, 01) || Time.Date == new DateTime(2014, 12, 10))
            {
                var aboveUpperThreshold = oscillatorRsi.Signal == OscillatorSignals.AboveUpperThreshold;
                if (RuntimeStatistics.ContainsKey("AboveUpperThresholdCorrectlyEstimated"))
                {
                    var lastTest = bool.Parse(RuntimeStatistics["AboveUpperThresholdCorrectlyEstimated"]);
                    RuntimeStatistics["AboveUpperThresholdCorrectlyEstimated"] =
                        (lastTest && aboveUpperThreshold).ToString();
                }
                else
                {
                    RuntimeStatistics["AboveUpperThresholdCorrectlyEstimated"] = aboveUpperThreshold.ToString();

                }
            }

            if (Time.Date == new DateTime(2014, 12, 11))
            {
                var crossUpperThresholdFromAbove = oscillatorRsi.Signal == OscillatorSignals.CrossUpperThresholdFromAbove;
                RuntimeStatistics["CrossUpperThresholdFromAboveCorrectlyEstimated"] = crossUpperThresholdFromAbove.ToString();
            }

            if (Time.Date == new DateTime(2014, 12, 13))
            {
                var crossLowerThresholdFromAbove = oscillatorRsi.Signal == OscillatorSignals.CrossLowerThresholdFromAbove;
                RuntimeStatistics["CrossLowerThresholdFromAboveCorrectlyEstimated"] = crossLowerThresholdFromAbove.ToString();
            }

            if (Time.Date == new DateTime(2014, 12, 16) || Time.Date == new DateTime(2014, 12, 17))
            {
                var bellowLowerThreshold = oscillatorRsi.Signal == OscillatorSignals.BellowLowerThreshold;
                if (RuntimeStatistics.ContainsKey("BellowLowerThresholdCorrectlyEstimated"))
                {
                    var lastTest = bool.Parse(RuntimeStatistics["BellowLowerThresholdCorrectlyEstimated"]);
                    RuntimeStatistics["BellowLowerThresholdCorrectlyEstimated"] =
                        (lastTest && bellowLowerThreshold).ToString();
                }
                else
                {
                    RuntimeStatistics["BellowLowerThresholdCorrectlyEstimated"] = bellowLowerThreshold.ToString();

                }
            }

            if (Time.Date == new DateTime(2014, 12, 18))
            {
                var crossLowerThresholdFromBelow = oscillatorRsi.Signal == OscillatorSignals.CrossLowerThresholdFromBelow;
                RuntimeStatistics["CrossLowerThresholdFromBelowCorrectlyEstimated"] = crossLowerThresholdFromBelow.ToString();
            }
        }
    }
}
