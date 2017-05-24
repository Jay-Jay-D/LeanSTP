using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Configuration;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public partial class TradingStrategiesBasedOnGeneticAlgorithms
    {
        private TradingRule SetTradingRule(Symbol pair, bool isEntryRule)
        {
            var technicalIndicatorSignals = new List<ITechnicalIndicatorSignal>();
            var logicalOperators = new List<string>();
            var ruleAction = isEntryRule ? "Entry" : "Exit";

            for (var i = 1; i <= _indicatorSignalCount; i++)
            {
                var indicatorSignal = SetUpIndicatorSignal(pair, i, ruleAction);
                technicalIndicatorSignals.Add(indicatorSignal);

                if (i != _indicatorSignalCount)
                {
                    var parsedOperator = Config.GetInt(ruleAction + "Operator" + i) == 0 ? "or" : "and";
                    logicalOperators.Add(parsedOperator);
                }
            }
            return new TradingRule(technicalIndicatorSignals.ToArray(), logicalOperators.ToArray());
        }

        private ITechnicalIndicatorSignal SetUpIndicatorSignal(Symbol pair, int indicatorN, string ruleAction)
        {
            var oscillatorThresholds = new OscillatorThresholds { Lower = 20, Upper = 80 };
            var direction = Config.GetInt(ruleAction + "Indicator" + indicatorN + "Direction") == 0
                ? TradeRuleDirection.LongOnly
                : TradeRuleDirection.ShortOnly;

            var indicatorId = Config.GetInt(ruleAction + "Indicator" + indicatorN, -1);
            //TODO: make the right stuff here.
            if (indicatorId == -1)
                throw new ArgumentException(
                    "Please check the optimization.json! There are not as many indicators as it should.");
            var indicator = (TechicalIndicators)indicatorId;
            ITechnicalIndicatorSignal technicalIndicator = null;
            switch (indicator)
            {
                case TechicalIndicators.SimpleMovingAverage:
                    // Canonical cross moving average parameters.
                    var fast = SMA(pair, 50);
                    var slow = SMA(pair, 200);
                    technicalIndicator = new CrossingMovingAverages(fast, slow, direction);
                    break;

                case TechicalIndicators.MovingAverageConvergenceDivergence:
                    var macd = MACD(pair, 12, 26, 9);
                    technicalIndicator = new CrossingMovingAverages(macd, macd.Signal, direction);
                    break;

                case TechicalIndicators.Stochastic:
                    var sto = STO(pair, 14);
                    technicalIndicator = new OscillatorSignal(sto.StochD, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.RelativeStrengthIndex:
                    var rsi = RSI(pair, 14);
                    technicalIndicator = new OscillatorSignal(rsi, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.CommodityChannelIndex:
                    var cci = CCI(pair, 20);
                    oscillatorThresholds.Lower = -100;
                    oscillatorThresholds.Lower = 100;
                    technicalIndicator = new OscillatorSignal(cci, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.MomentumPercent:
                    var pm = MOMP(pair, 60);
                    oscillatorThresholds.Lower = -5;
                    oscillatorThresholds.Lower = 5;
                    technicalIndicator = new OscillatorSignal(pm, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.WilliamsPercentR:
                    var wr = WILR(pair, 14);
                    technicalIndicator = new OscillatorSignal(wr, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.PercentagePriceOscillator:
                    var ppo = MACD(pair, 12, 26, 9).Over(EMA(pair, 26)).Plus(100m);
                    var signal = new SimpleMovingAverage(9).Of(ppo);
                    technicalIndicator = new CrossingMovingAverages(ppo, signal, direction);
                    break;

                case TechicalIndicators.BollingerBands:
                    throw new NotImplementedException("WIP");
                    break;
            }

            return technicalIndicator;
        }
    }
}
