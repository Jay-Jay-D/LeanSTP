using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuantConnect.Algorithm.CSharp.TradingStrategiesBasedOnGeneticAlgorithms
{
    public class TradingStrategiesBasedOnGeneticAlgorithms : QCAlgorithm
    {

    }

    internal class TradingRule
    {
        private ITechnicalIndicatorSignal[] _technicalIndicatorSignals;
        Tuple<string, string> _booleanOperators;

        public bool TradeRuleSignal
        {
            get { return GetTradeRuleSignal(); }
        }

        public TradingRule(ITechnicalIndicatorSignal[] technicalIndicatorSignals,
            Tuple<string, string> booleanOperators)
        {
            _technicalIndicatorSignals = technicalIndicatorSignals;
            _booleanOperators = booleanOperators;
        }

        public bool GetTradeRuleSignal()
        {
            bool tradeRuleSignal = false;
            if (_booleanOperators.Item1 == "and" && _booleanOperators.Item2 == "and")
            {
                tradeRuleSignal = _technicalIndicatorSignals[0].GetSignal() &&
                                  _technicalIndicatorSignals[1].GetSignal() &&
                                  _technicalIndicatorSignals[2].GetSignal();
            }
            else if (_booleanOperators.Item1 == "and" && _booleanOperators.Item2 == "or")
            {
                tradeRuleSignal = _technicalIndicatorSignals[0].GetSignal() &&
                                  _technicalIndicatorSignals[1].GetSignal() ||
                                  _technicalIndicatorSignals[2].GetSignal();
            }
            else if (_booleanOperators.Item1 == "or" && _booleanOperators.Item2 == "and")
            {
                tradeRuleSignal = _technicalIndicatorSignals[0].GetSignal() ||
                                  _technicalIndicatorSignals[1].GetSignal() &&
                                  _technicalIndicatorSignals[2].GetSignal();
            }
            else if (_booleanOperators.Item1 == "or" && _booleanOperators.Item2 == "or")
            {
                tradeRuleSignal = _technicalIndicatorSignals[0].GetSignal() ||
                                  _technicalIndicatorSignals[1].GetSignal() ||
                                  _technicalIndicatorSignals[2].GetSignal();
            }
            return tradeRuleSignal;

        }




    }

    interface ITechnicalIndicatorSignal
    {
        bool GetSignal();
    }
}