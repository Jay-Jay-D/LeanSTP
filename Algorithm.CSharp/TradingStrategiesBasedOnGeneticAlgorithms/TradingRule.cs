using System.Linq;
using System.Text;
using DynamicExpresso;

namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     This class wires and evaluates dynamically a set of <see cref="ITechnicalIndicatorSignal" /> and a set of logical
    ///     operators in string format in the form:
    ///     Indicator1|Operator1|Indicator2|Operator2|...|IndicatorN|OperatorN|
    /// </summary>
    public class TradingRule
    {
        private readonly string[] _logicalOperators;
        private readonly ITechnicalIndicatorSignal[] _technicalIndicatorSignals;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TradingRule" /> class.
        /// </summary>
        /// <param name="technicalIndicatorSignals">The technical indicator signals.</param>
        /// <param name="logicalOperators">The logical operators.</param>
        public TradingRule(ITechnicalIndicatorSignal[] technicalIndicatorSignals,
            string[] logicalOperators)
        {
            _technicalIndicatorSignals = technicalIndicatorSignals;
            _logicalOperators = logicalOperators;
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is ready.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is ready; otherwise, <c>false</c>.
        /// </value>
        public bool IsReady
        {
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

        /// <summary>
        ///     Gets a value indicating true value of the chain of <see cref="ITechnicalIndicatorSignal" /> and logical operators.
        /// </summary>
        /// <value>
        ///     <c>true</c> if the chain of <see cref="ITechnicalIndicatorSignal" /> and logical operators is true; otherwise,
        ///     <c>false</c>.
        /// </value>
        public bool TradeRuleSignal
        {
            get { return GetTradeRuleSignal(); }
        }

        /// <summary>
        ///     Dynamically evaluates the chain of <see cref="ITechnicalIndicatorSignal" /> and logical operators.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the chain of <see cref="ITechnicalIndicatorSignal" /> and logical operators is true; otherwise,
        ///     <c>false</c>.
        /// </returns>
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