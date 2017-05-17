using System;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{

    public enum TradeRuleDirection
    {
        LongOnly = 1,
        ShortOnly = -1
    }



    public class TradingStrategiesBasedOnGeneticAlgorithms : QCAlgorithm
    {
    }

    internal class TradingRule
    {
        private readonly Tuple<string, string> _logicalOperators;
        private readonly ITechnicalIndicatorSignal[] _technicalIndicatorSignals;

        public TradingRule(ITechnicalIndicatorSignal[] technicalIndicatorSignals,
            Tuple<string, string> logicalOperators)
        {
            _technicalIndicatorSignals = technicalIndicatorSignals;
            _logicalOperators = logicalOperators;
        }

        public bool TradeRuleSignal
        {
            get { return GetTradeRuleSignal(); }
        }

        private bool GetTradeRuleSignal()
        {
            var tradeRuleSignal = false;
            if (_logicalOperators.Item1 == "and" && _logicalOperators.Item2 == "and")
            {
                tradeRuleSignal = _technicalIndicatorSignals[0].GetSignal() &&
                                  _technicalIndicatorSignals[1].GetSignal() &&
                                  _technicalIndicatorSignals[2].GetSignal();
            }
            else if (_logicalOperators.Item1 == "and" && _logicalOperators.Item2 == "or")
            {
                tradeRuleSignal = _technicalIndicatorSignals[0].GetSignal() &&
                                  _technicalIndicatorSignals[1].GetSignal() ||
                                  _technicalIndicatorSignals[2].GetSignal();
            }
            else if (_logicalOperators.Item1 == "or" && _logicalOperators.Item2 == "and")
            {
                tradeRuleSignal = _technicalIndicatorSignals[0].GetSignal() ||
                                  _technicalIndicatorSignals[1].GetSignal() &&
                                  _technicalIndicatorSignals[2].GetSignal();
            }
            else if (_logicalOperators.Item1 == "or" && _logicalOperators.Item2 == "or")
            {
                tradeRuleSignal = _technicalIndicatorSignals[0].GetSignal() ||
                                  _technicalIndicatorSignals[1].GetSignal() ||
                                  _technicalIndicatorSignals[2].GetSignal();
            }
            return tradeRuleSignal;
        }
    }

    

    public interface ITechnicalIndicatorSignal
    {
        bool GetSignal();
    }
}