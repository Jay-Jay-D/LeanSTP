using System;
using QuantConnect.Data;

namespace QuantConnect.Algorithm.CSharp
{
    public partial class TradingStrategiesBasedOnGeneticAlgorithms : QCAlgorithm
    {
        private TradingRule _entryradingRule;
        private TradingRule _exitTradingRule;
        private Symbol _pair;
        private readonly bool IsOutOfSampleRun = true;
        private readonly int oosPeriod = 1;

        public override void Initialize()
        {
            SetCash(startingCash: 1e6);
            SetStartDate(year: 2016, month: 07, day: 01);
            SetEndDate(year: 2016, month: 12, day: 31);

            if (IsOutOfSampleRun)
            {
                var startDate = new DateTime(year: 2017, month: 1, day: 1);
                SetEndDate(startDate.AddMonths(oosPeriod));
                SetStartDate(startDate);
                RuntimeStatistics["ID"] = GetParameter("ID");
            }

            _pair = AddForex("EURUSD", leverage: 10).Symbol;

            _entryradingRule = SetTradingRule(_pair, isEntryRule: true);
            _exitTradingRule = SetTradingRule(_pair, isEntryRule: false);
        }

        public override void OnData(Slice slice)
        {
            if (!_entryradingRule.IsReady) return;
            if (!Portfolio.Invested)
            {
                if (_entryradingRule.TradeRuleSignal) SetHoldings(_pair, percentage: 1m);
            }
            else
            {
                if (_exitTradingRule.TradeRuleSignal) Liquidate(_pair);
            }
        }
    }
}