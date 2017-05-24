using System.Linq;
using System.Text;
using DynamicExpresso;

namespace QuantConnect.Algorithm.CSharp
{
    public class TradingRule
    {
        public bool IsReady {
            get
            {
                var isReady = true;
                foreach (var indicator in _technicalIndicatorSignals)
                {
                    isReady = indicator.IsReady && isReady;
                }
                return isReady;
            }
        }
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
}