using System;
using System.Collections.Generic;
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
            SetStartDate(2013,02,01);
            SetEndDate(2013,04,30);
            SetCash(10000);
            spy = AddEquity("SPY", Resolution.Daily).Symbol;
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
                oscillatorRsi = new OscillatorSignal(spy, "RelativeStrengthIndex", new object[] { 14 });
                isFirst = false;
            }

            if (actualRsi.IsReady)
            {
                var rsiIsCorrectlyInstantitadedAndRegistered = oscillatorRsi.Indicator.IsReady;
                var rsiIsCorrectlyEstimated = actualRsi.Current.Value == oscillatorRsi.Indicator.Current.Value;
                RuntimeStatistics["RsiIsCorrectlyInstantitadedAndRegistered"] = rsiIsCorrectlyInstantitadedAndRegistered.ToString();
                RuntimeStatistics["RsiIsCorrectlyEstimated"] = rsiIsCorrectlyEstimated.ToString();
            }
          

        }
    }
}
