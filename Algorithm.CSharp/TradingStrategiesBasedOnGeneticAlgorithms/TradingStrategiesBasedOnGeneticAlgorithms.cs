using System;
using System.Collections.Generic;
using QuantConnect.Configuration;

namespace QuantConnect.Algorithm.CSharp
{
    public class TradingStrategiesBasedOnGeneticAlgorithms : QCAlgorithm
    {
        private readonly int indicatorSignalCount = 5;
        private TradingRule tradingRule;

        public override void Initialize()
        {
            var pair = AddForex("EURUSD").Symbol; 
            tradingRule = SetTradingRule(pair);
        }

        private TradingRule SetTradingRule(Symbol pair)
        {
            var technicalIndicatorSignals = new List<ITechnicalIndicatorSignal>();
            var logicalOperators = new List<string>();

            for (var i = 1; i <= indicatorSignalCount; i++)
            {
                var indicatorSignal = SetUpIndicatorSignal(pair, i);
                technicalIndicatorSignals.Add(indicatorSignal);

                if (i != indicatorSignalCount)
                {
                    var parsedOperator = Config.GetInt("Operator" + i) == 0 ? "or" : "and";
                    logicalOperators.Add(parsedOperator);
                }
            }
            return new TradingRule(technicalIndicatorSignals.ToArray(), logicalOperators.ToArray());
        }

        private ITechnicalIndicatorSignal SetUpIndicatorSignal(Symbol pair, int indicatorN)
        {
            var oscillatorThresholds = new OscillatorThresholds { Lower = 20, Upper = 80 };
            var direction = Config.GetInt("Indicator" + indicatorN + "Direction") == 0
                ? TradeRuleDirection.LongOnly
                : TradeRuleDirection.ShortOnly;

            var indicatorId = Config.GetInt("Indicator" + indicatorN, -1);
            //TODO: make the right stuff here.
            if (indicatorId == -1)
                throw new ArgumentException(
                    "Please check the optimization.json! There are not as many indicators as it should.");
            var indicator = (TechicalIndicators) indicatorId;
            ITechnicalIndicatorSignal technicalIndicator;
            switch (indicator)
            {
                case TechicalIndicators.SimpleMovingAverage:
                    // Canonical cross moving average parameters.
                    var fast = SMA(pair, 50);
                    var slow = SMA(pair, 200);
                    technicalIndicator = new CrossingMovingAverages(fast,slow, direction);
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
                    oscillatorThresholds.Lower = - 100;
                    oscillatorThresholds.Lower = 100;
                    technicalIndicator = new OscillatorSignal(cci, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.MomentumPercent:
                    var pm = MOMP(pair, 60);
                    oscillatorThresholds.Lower = -5;
                    oscillatorThresholds.Lower = 5;
                    technicalIndicator = new OscillatorSignal(pm, oscillatorThresholds, direction);
                    break;

                case TechicalIndicators.BollingerBands:
                    break;

                case TechicalIndicators.WilliamsPercentR:
                    break;

                case TechicalIndicators.PercentagePriceOscillator:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            throw new NotImplementedException();
        }
    }
}