using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
        private StringBuilder CsvOutput;
        private bool isFirst = true;
        private OscillatorSignal oscillatorRsi;
        private RelativeStrengthIndex rsi;

        public override void OnData(Slice slice)
        {
            if (isFirst)
            {
                rsi = RSI(spy, 14, MovingAverageType.Wilders);
                var threshodls = new OscillatorThresholds {Lower = 40, Upper = 60};
                oscillatorRsi = new OscillatorSignal(rsi, threshodls);
                CsvOutput = new StringBuilder("Date,RSI,OscillatorSignal\n");
                isFirst = false;
            }

            if (rsi.IsReady)
            {
                CsvOutput.AppendLine(string.Format("{0:yyyy-MM-dd},{1},{2}", Time.Date,
                    rsi.Current.Value.SmartRounding(), oscillatorRsi.Signal));
            }
        }

        public override void OnEndOfAlgorithm()
        {
            File.WriteAllText(@"D:\REPOS\LeanSTP\Tests\TestData\testSignals.csv", CsvOutput.ToString());
        }
    }

    public class RsiInstantiatedAndRegisteredCorrectly : TradingStrategiesBasedOnGeneticAlgorithmsBaseForTesting
    {
        private RelativeStrengthIndex actualRsi;
        private bool isFirst = true;
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
                    RuntimeStatistics["RsiIsCorrectlyInstantitadedAndRegistered"] =
                        rsiIsCorrectlyInstantitadedAndRegistered.ToString();
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
        private Dictionary<DateTime, OscillatorSignals> testingData = new Dictionary<DateTime, OscillatorSignals>();

        public override void OnData(Slice slice)
        {
            if (isFirst)
            {
                var injectedRsi = RSI(spy, 14, MovingAverageType.Wilders);
                var threshodls = new OscillatorThresholds {Lower = 40, Upper = 60};
                oscillatorRsi = new OscillatorSignal(injectedRsi, threshodls);
                var csv = File.ReadAllLines(@".\TestData\testSignals.csv");
                testingData = csv.Skip(1).Select(l => l.Split(','))
                    .ToDictionary(l => DateTime.ParseExact(l[0], "yyyy-MM-dd", CultureInfo.InvariantCulture).Date,
                        l => (OscillatorSignals) Enum.Parse(typeof(OscillatorSignals), l[2]));

                isFirst = false;
            }

            if (testingData.ContainsKey(Time.Date))
            {
                var expectedSignal = testingData[Time.Date];
                var testStringName = expectedSignal + "CorrectlyEstimated";
                var testingActualSignal = oscillatorRsi.Signal == expectedSignal;
                if (RuntimeStatistics.ContainsKey(testStringName))
                {
                    var lastTest = bool.Parse(RuntimeStatistics[testStringName]);
                    RuntimeStatistics[testStringName] =
                        (lastTest && testingActualSignal).ToString();
                }
                else
                {
                    RuntimeStatistics[testStringName] = testingActualSignal.ToString();
                }
            }
        }
    }
}