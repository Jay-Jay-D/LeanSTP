using System;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public partial class TradingStrategiesBasedOnGeneticAlgorithms
    {
        private readonly int _indicatorSignalCount = 5;

        /// <summary>
        ///     Sets up the trading rule from the Config first, then form the QCAlgorithm Parameters.
        /// </summary>
        /// <param name="pair">The pair.</param>
        /// <param name="isEntryRule">
        ///     if set to <c>true</c> [is entry rule]. Only used to differentiate the genes for entry and
        ///     exit
        /// </param>
        /// <returns></returns>
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

        /// <summary>
        ///     Sets up indicator signal. This method is where the Technical indicator rules are defined.
        /// </summary>
        /// <param name="pair">The pair.</param>
        /// <param name="indicatorN">The number if indicator.</param>
        /// <param name="ruleAction">
        ///     The rule action. Should be 'Entry' or 'Exit' and is only used to differentiate the genes for
        ///     entry and exit
        /// </param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException">WIP</exception>
        private ITechnicalIndicatorSignal SetUpIndicatorSignal(Symbol pair, int indicatorN, string ruleAction)
        {
            var oscillatorThresholds = new OscillatorThresholds {Lower = 20, Upper = 80};
            var key = ruleAction + "Indicator" + indicatorN + "Direction";
            var intDirection = GetGeneIntFromKey(key);

            var direction = intDirection == 0
                ? TradeRuleDirection.LongOnly
                : TradeRuleDirection.ShortOnly;

            key = ruleAction + "Indicator" + indicatorN;
            var indicatorId = GetGeneIntFromKey(key);
            var indicator = (TechicalIndicators) indicatorId;
            ITechnicalIndicatorSignal technicalIndicator = null;
            switch (indicator)
            {
                case TechicalIndicators.SimpleMovingAverage:
                    // Canonical cross moving average parameters.
                    var fast = SMA(pair, period: 50);
                    var slow = SMA(pair, period: 200);
                    technicalIndicator = new CrossingMovingAverages(fast, slow, direction);
                    break;

                case TechicalIndicators.MovingAverageConvergenceDivergence:
                    var macd = MACD(pair, fastPeriod: 12, slowPeriod: 26, signalPeriod: 9);
                    technicalIndicator = new CrossingMovingAverages(macd, macd.Signal, direction);
                    break;

                case TechicalIndicators.Stochastic:
                    var sto = STO(pair, period: 14);
                    technicalIndicator = new OscillatorSignal(sto.StochD, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.RelativeStrengthIndex:
                    var rsi = RSI(pair, period: 14);
                    technicalIndicator = new OscillatorSignal(rsi, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.CommodityChannelIndex:
                    var cci = CCI(pair, period: 20);
                    oscillatorThresholds.Lower = -100;
                    oscillatorThresholds.Lower = 100;
                    technicalIndicator = new OscillatorSignal(cci, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.MomentumPercent:
                    var pm = MOMP(pair, period: 60);
                    oscillatorThresholds.Lower = -5;
                    oscillatorThresholds.Lower = 5;
                    technicalIndicator = new OscillatorSignal(pm, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.WilliamsPercentR:
                    var wr = WILR(pair, period: 14);
                    technicalIndicator = new OscillatorSignal(wr, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.PercentagePriceOscillator:
                    var ppo = MACD(pair, fastPeriod: 12, slowPeriod: 26, signalPeriod: 9).Over(EMA(pair, period: 26))
                        .Plus(constant: 100m);
                    var signal = new SimpleMovingAverage(period: 9).Of(ppo);
                    technicalIndicator = new CrossingMovingAverages(ppo, signal, direction);
                    break;

                case TechicalIndicators.BollingerBands:
                    throw new NotImplementedException("WIP");
                    break;
            }

            return technicalIndicator;
        }

        /// <summary>
        ///     Gets the gene int from key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">The gene " + key + " is not present either as Config or as Parameter</exception>
        /// <remarks>
        ///     This method makes the algorithm working with the genes defined from the Config (as in the Lean Optimization) and
        ///     from the Parameters (as the Lean Caller).
        /// </remarks>
        private int GetGeneIntFromKey(string key)
        {
            var intGene = Config.GetInt(key, int.MinValue);
            if (intGene == int.MinValue)
            {
                try
                {
                    intGene = int.Parse(GetParameter(key));
                    Log(string.Format("Parameter {0} set to {1}", key, intGene));
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