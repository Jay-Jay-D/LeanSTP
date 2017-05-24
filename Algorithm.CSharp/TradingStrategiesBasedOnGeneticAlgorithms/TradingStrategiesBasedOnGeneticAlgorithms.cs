using System;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public partial class TradingStrategiesBasedOnGeneticAlgorithms : QCAlgorithm
    {
        private TradingRule _entryradingRule;
        private TradingRule _exitTradingRule;
        private readonly int _indicatorSignalCount = 5;
        private Symbol _pair;

        public override void Initialize()
        {
            SetCash(10000);
            SetStartDate(2015, 01, 01);
            SetEndDate(2015, 01, 15);

            _pair = AddForex("EURUSD").Symbol;
            _entryradingRule = SetTradingRule(_pair, isEntryRule: true);
            _exitTradingRule = SetTradingRule(_pair, isEntryRule: false);
        }

        public override void OnData(Slice slice)
        {
            if (!_entryradingRule.IsReady) return;
            if (!Portfolio.Invested)
            {
                if (_entryradingRule.TradeRuleSignal) SetHoldings(_pair, 1m);
            }
            else
            {
                if (_exitTradingRule.TradeRuleSignal) Liquidate(_pair);
            }
        }
    }
}