using QuantConnect.Configuration;
using QuantConnect.Indicators;
using System;
using System.Collections.Generic;

namespace QuantConnect.Algorithm.CSharp
{
    public partial class TradingStrategiesBasedOnGeneticAlgorithms
    {
        private readonly int _indicatorSignalCount = 5;

        private TradingRule SetTradingRule(Symbol pair, bool isEntryRule)
        {
            var technicalIndicatorSignals = new List<ITechnicalIndicatorSignal>();
            var logicalOperators = new List<string>();
            var ruleAction = isEntryRule ? "Entry" : "Exit";

            for (var i = 1; i <= _indicatorSignalCount; i++)
            {
                var indicatorSignal = SetUpIndicatorSignal(pair, i, ruleAction);
                technicalIndicatorSignals.Add(indicatorSignal);

                // As the operators are always one less than the indicator, this 'if' skips the operator after the last indicator.
                if (i == _indicatorSignalCount) continue;
                var key = ruleAction + "Operator" + i;
                var intOperator = GetGeneIntFromKey(key);
                var parsedOperator = intOperator == 0 ? "or" : "and";
                logicalOperators.Add(parsedOperator);
            }
            return new TradingRule(technicalIndicatorSignals.ToArray(), logicalOperators.ToArray());
        }

        private ITechnicalIndicatorSignal SetUpIndicatorSignal(Symbol pair, int indicatorN, string ruleAction)
        {
            var oscillatorThresholds = new OscillatorThresholds { Lower = 20, Upper = 80 };
            var key = ruleAction + "Indicator" + indicatorN + "Direction";
            var intDirection = GetGeneIntFromKey(key);

            var direction = intDirection == 0
                ? TradeRuleDirection.LongOnly
                : TradeRuleDirection.ShortOnly;

            key = ruleAction + "Indicator" + indicatorN;
            var indicatorId = GetGeneIntFromKey(key);
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

        /// <summary>
        /// Gets the gene int from key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">The gene " + key + " is not present either as Config or as Parameter</exception>
        /// <remarks>
        /// This method makes the algorithm working with the genes defined from the Config (as in the Lean Optimization) and from the Parameters (as the Lean Caller).
        /// </remarks>
        private int GetGeneIntFromKey(string key)
        {
            var intGene = Config.GetInt(key, int.MinValue);
            if (intGene == int.MinValue)
            {
                try
                {
                    intGene = int.Parse(GetParameter(key));
                }
                catch (ArgumentNullException e)
                {
                    throw new ArgumentNullException(key,
                        "The gene " + key + " is not present either as Config or as Parameter");
                }
            }
            return intGene;
        }
    }
}