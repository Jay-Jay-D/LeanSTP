namespace QuantConnect.Algorithm.CSharp
{
    internal class TradingRuleQCVersion
    {
        private readonly bool _isEntryRule;
        private readonly ITechnicalIndicatorSignal[] _technicalIndicatorSignals;

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
        ///     Gets the trade rule signal for the best in-sample performance individual.
        /// </summary>
        /// <returns></returns>
        private bool GetTradeRuleSignal()
        {
            var signal = false;
            if (_isEntryRule)
            {
                signal = _technicalIndicatorSignals[0].GetSignal() && //  Long SimpleMovingAverage
                         _technicalIndicatorSignals[1].GetSignal() || //  Long Stochastic
                         _technicalIndicatorSignals[2].GetSignal() && //  Long RelativeStrengthIndex
                         _technicalIndicatorSignals[3].GetSignal() || //  Short MovingAverageConvergenceDivergence
                         _technicalIndicatorSignals[4].GetSignal(); //  Short MomentumPercent
            }
            else
            {
                signal = _technicalIndicatorSignals[0].GetSignal() || //  Short CommodityChannelIndex
                         _technicalIndicatorSignals[1].GetSignal() && //  Long RelativeStrengthIndex
                         _technicalIndicatorSignals[2].GetSignal() || //  Short Stochastic
                         _technicalIndicatorSignals[3].GetSignal() && //  Long MovingAverageConvergenceDivergence
                         _technicalIndicatorSignals[4].GetSignal(); //  Long Stochastic
            }
            return signal;
        }
    }
}