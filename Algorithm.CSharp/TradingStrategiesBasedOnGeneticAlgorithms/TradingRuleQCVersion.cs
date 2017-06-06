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

        /// <summary>
        /// Gets the trade rule signal for the best in-sample performance individual.
        /// </summary>
        /// <returns></returns>
        private bool GetTradeRuleSignal()
        {
            var signal = false;
            if (_isEntryRule)
            {
                signal = _technicalIndicatorSignals[0].GetSignal() &&   // Long SMA signal
                         _technicalIndicatorSignals[1].GetSignal() ||   // Long Stochastic
                         _technicalIndicatorSignals[2].GetSignal() &&   // Long RelativeStrengthIndex
                         _technicalIndicatorSignals[3].GetSignal() ||   // Short MovingAverageConvergenceDivergence
                         _technicalIndicatorSignals[4].GetSignal();     // Short MovingAverageConvergenceDivergence
            }
            else
            {
                signal = _technicalIndicatorSignals[0].GetSignal() ||   // Short CommodityChannelIndex
                         _technicalIndicatorSignals[1].GetSignal() &&   // Long RelativeStrengthIndex
                         _technicalIndicatorSignals[2].GetSignal() ||   // Short PercentagePriceOscillator
                         _technicalIndicatorSignals[3].GetSignal() &&   // Short MovingAverageConvergenceDivergence
                         _technicalIndicatorSignals[4].GetSignal();     // Short Stochastic
            }
            return signal;
        }
    }
}