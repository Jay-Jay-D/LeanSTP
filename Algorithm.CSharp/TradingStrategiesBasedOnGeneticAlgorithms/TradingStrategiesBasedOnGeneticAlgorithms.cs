using System;
using System.Collections.Generic;
using QuantConnect.Configuration;
using QuantConnect.Data;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public partial class TradingStrategiesBasedOnGeneticAlgorithms : QCAlgorithm
    {
        private bool IsOutOfSampleRun = true;
        private TradingRule _entryradingRule;
        private TradingRule _exitTradingRule;
        private Symbol _pair;

        public override void Initialize()
        {
            SetCash(1e6);
            SetStartDate(2016, 07, 01);
            SetEndDate(2016, 12, 31);

            if (IsOutOfSampleRun)
            {
                var startDate = new DateTime(2017, 1, 1);
                SetEndDate(startDate.AddMonths(1));
                SetStartDate(startDate);
                RuntimeStatistics["ID"] = GetParameter("ID");
            }

            _pair = AddForex("EURUSD", leverage:10).Symbol;
            _entryradingRule = SetTradingRule(_pair, isEntryRule: true);
            _exitTradingRule = SetTradingRule(_pair, isEntryRule: false);
        }

        public override void OnData(Slice slice)
        {
            if (!_entryradingRule.IsReady) return;
            if (!Portfolio.Invested)
            {
                if (_entryradingRule.TradeRuleSignal) SetHoldings(_pair, 0.1m);
            }
            else
            {
                if (_exitTradingRule.TradeRuleSignal) Liquidate(_pair);
            }
        }
    }
}