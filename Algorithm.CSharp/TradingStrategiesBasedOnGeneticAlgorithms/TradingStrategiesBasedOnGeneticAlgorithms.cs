using System;
using System.Linq;
using System.Text;
using DynamicExpresso;
using QuantConnect.Data;
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

        public TradingRule(ITechnicalIndicatorSignal[] technicalIndicatorSignals,
            string[] logicalOperators)
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
            string stringSignal;
            var condition = new StringBuilder();

            for (var i = 0; i < _logicalOperators.Length; i++)
            {
                var stringOperator = _logicalOperators[i] == "and" ? "&&" : "||";
                stringSignal = _technicalIndicatorSignals[i].GetSignal().ToString().ToLower();
                condition.Append(stringSignal + stringOperator);
            }
            stringSignal = _technicalIndicatorSignals.Last().GetSignal().ToString().ToLower();
            condition.Append(stringSignal);

            var interpreter = new Interpreter();
            return interpreter.Eval<bool>(condition.ToString());
        }
    }

    public class OscillatorSignal : ITechnicalIndicatorSignal
    {
        private Symbol spy;
        private string v1;
        private object[] v2;

        public dynamic Indicator { get; private set; }

        public OscillatorSignal(Symbol symbol, string indicatorName, object[] indicatorParameters)
        {
            var indicatorType = typeof(Indicator).Assembly
                .GetTypes()
                .FirstOrDefault(i => i.Name == indicatorName);
             Indicator = Activator.CreateInstance(indicatorType, indicatorParameters);


        }

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