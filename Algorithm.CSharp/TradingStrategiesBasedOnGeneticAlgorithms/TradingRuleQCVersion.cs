namespace QuantConnect.Algorithm.CSharp
{
    internal class TradingRuleQCVersion
    {
        private readonly ITechnicalIndicatorSignal[] _technicalIndicatorSignals;
        private readonly bool _isEntryRule;

        public TradingRuleQCVersion(ITechnicalIndicatorSignal[] technicalIndicatorSignals, bool isEntryRule)
        {
            _technicalIndicatorSignals = technicalIndicatorSignals;
            _isEntryRule = isEntryRule;
        }

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

        public bool TradeRuleSignal
        {
            get { return GetTradeRuleSignal(); }
        }

        private bool GetTradeRuleSignal()
        {
            var signal = false;
            if (_isEntryRule)
            {
                signal = _technicalIndicatorSignals[0].GetSignal() &&
                         _technicalIndicatorSignals[1].GetSignal() ||
                         _technicalIndicatorSignals[2].GetSignal() &&
                         _technicalIndicatorSignals[3].GetSignal() ||
                         _technicalIndicatorSignals[4].GetSignal();
            }
            else
            {
                signal = _technicalIndicatorSignals[0].GetSignal() ||
                         _technicalIndicatorSignals[1].GetSignal() &&
                         _technicalIndicatorSignals[2].GetSignal() ||
                         _technicalIndicatorSignals[3].GetSignal() &&
                         _technicalIndicatorSignals[4].GetSignal();
            }
            return signal;
        }
    }
}