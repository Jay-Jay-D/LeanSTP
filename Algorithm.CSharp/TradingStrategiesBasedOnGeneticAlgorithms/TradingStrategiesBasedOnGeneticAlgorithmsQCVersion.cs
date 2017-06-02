using System;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     This is a truncated version of the algorithm made for the Trading Strategies Based on Genetic Algorithms project
    ///     for the QuantConnect platform.
    ///     The main difference is that the rules are hard coded in the TradingRule class, instead if being dynamically
    ///     generated as in the original.
    /// </summary>
    /// <seealso cref="QuantConnect.Algorithm.QCAlgorithm" />
    internal class TradingStrategiesBasedOnGeneticAlgorithmsQCVersion : QCAlgorithm
    {
        private readonly int _indicatorSignalCount = 5;
        private TradingRuleQCVersion _entryradingRule;
        private TradingRuleQCVersion _exitTradingRule;
        private Symbol _pair;

        /// <summary>
        ///     Here are the parameters of the individual with the best in-sample fitness.
        /// </summary>
        private readonly Dictionary<string, string> parametersToBacktest = new Dictionary<string, string>
        {
            {"EntryIndicator1", "0"},
            {"EntryIndicator2", "2"},
            {"EntryIndicator3", "3"},
            {"EntryIndicator4", "1"},
            {"EntryIndicator5", "5"},
            {"EntryIndicator1Direction", "0"},
            {"EntryIndicator2Direction", "0"},
            {"EntryIndicator3Direction", "0"},
            {"EntryIndicator4Direction", "1"},
            {"EntryIndicator5Direction", "1"},
            {"ExitIndicator1", "4"},
            {"ExitIndicator2", "3"},
            {"ExitIndicator3", "7"},
            {"ExitIndicator4", "1"},
            {"ExitIndicator5", "2"},
            {"ExitIndicator1Direction", "1"},
            {"ExitIndicator2Direction", "0"},
            {"ExitIndicator3Direction", "1"},
            {"ExitIndicator4Direction", "0"},
            {"ExitIndicator5Direction", "0"}
        };

        public override void Initialize()
        {
            SetCash(startingCash: 1e6);
            var startDate = new DateTime(year: 2017, month: 1, day: 1);
            SetStartDate(startDate);
            SetEndDate(startDate.AddMonths(months: 1));

            _pair = AddForex("EURUSD", leverage: 10).Symbol;

            SetParameters(parametersToBacktest);

            SetUpRules();
        }

        public override void OnData(Slice slice)
        {
            if (!_entryradingRule.IsReady) return;
            if (!Portfolio.Invested)
            {
                if (_entryradingRule.TradeRuleSignal) SetHoldings(_pair, percentage: 0.1m);
            }
            else
            {
                if (_exitTradingRule.TradeRuleSignal) Liquidate(_pair);
            }
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
        public ITechnicalIndicatorSignal SetUpIndicatorSignal(Symbol pair, int indicatorN, string ruleAction)
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
                }
                catch (ArgumentNullException e)
                {
                    throw new ArgumentNullException(key,
                        "The gene " + key + " is not present either as Config or as Parameter");
                }
            }
            return intGene;
        }

        private void SetUpRules()
        {
            bool[] bools = {true, false};

            foreach (var isEntryRule in bools)
            {
                var technicalIndicatorSignals = new List<ITechnicalIndicatorSignal>();
                var ruleAction = isEntryRule ? "Entry" : "Exit";
                for (var i = 1; i <= _indicatorSignalCount; i++)
                {
                    var indicatorSignal = SetUpIndicatorSignal(_pair, i, ruleAction);
                    technicalIndicatorSignals.Add(indicatorSignal);
                }

                if (isEntryRule)
                {
                    _entryradingRule = new TradingRuleQCVersion(technicalIndicatorSignals.ToArray(), isEntryRule);
                }
                else
                {
                    _exitTradingRule = new TradingRuleQCVersion(technicalIndicatorSignals.ToArray(), isEntryRule);
                }
            }
        }
    }
}