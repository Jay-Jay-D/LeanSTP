using System;
using System.Linq;
using System.Text;
using DynamicExpresso;
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

    public class TradingRule
    {
        private readonly string[] _logicalOperators;
        private readonly ITechnicalIndicatorSignal[] _technicalIndicatorSignals;
        private Interpreter _interpreter;

        public TradingRule(ITechnicalIndicatorSignal[] technicalIndicatorSignals,
            string[] logicalOperators)
        {
            _technicalIndicatorSignals = technicalIndicatorSignals;
            _logicalOperators = logicalOperators;
            _interpreter = new Interpreter();
        }

        public bool TradeRuleSignal
        {
            get { return GetTradeRuleSignal(); }
        }

        private bool GetTradeRuleSignal()
        {
            string stringSignal;
            var condition = new StringBuilder();

            for (int i = 0; i < _logicalOperators.Length; i++)
            {
                var stringOperator = _logicalOperators[i] == "and" ? "&&" : "||";
                stringSignal = _technicalIndicatorSignals[i].GetSignal().ToString().ToLower();
                condition.Append(stringSignal + stringOperator);
            }
            stringSignal = _technicalIndicatorSignals.Last().GetSignal().ToString().ToLower();
            condition.Append(stringSignal);
            return _interpreter.Eval<bool>(condition.ToString());
        }
    }


    public class OscillatorSignal : ITechnicalIndicatorSignal
    {
        public bool GetSignal()
        {
            throw new NotImplementedException();
        }
    }


    public interface ITechnicalIndicatorSignal
    {
        bool GetSignal();
    }
}