using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Data.Consolidators;
using QuantConnect.Data.Market;
using QuantConnect.Indicators;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp
{
    public class ConsolidationAlgorithm : QCAlgorithm
    {
        private const decimal stop_loss = 0.25m;
        private const decimal take_profit = 0.50m;

        // Tradebar  quoteBar;

        private const string RootSP500 = Futures.Indices.SP500EMini;
        private readonly HashSet<Symbol> _futureContracts = new HashSet<Symbol>();

        // private decimal new_SL = 0.0m ;
        // private decimal new_TP = 0.0m ;
        // int quantity = 1;
        // int count = 0;
        // int loss= 0 ;
        private TradeBar _spyMinutes;
        public Symbol _symbol = QuantConnect.Symbol.Create(RootSP500, SecurityType.Future, Market.USA);
        private Dictionary<FuturesContract, WilliamsPercentR> _williamsRs = new Dictionary<FuturesContract, WilliamsPercentR>();

        public override void Initialize()
        {
            SetStartDate(year: 2013, month: 10, day: 8);
            SetEndDate(year: 2013, month: 10, day: 11);
            SetCash(startingCash: 25000);
            var futureSP500 = AddFuture(RootSP500);
            futureSP500.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(value: 91));
            SetBenchmark(x => 0);
        }

        public override void OnData(Slice slice)
        {
            foreach (var chain in slice.FutureChains)
            {
                foreach (var contract in chain.Value)
                {
                    if (!_futureContracts.Contains(contract.Symbol))
                    {
                        _futureContracts.Add(contract.Symbol);
                        var consolidator = new TradeBarConsolidator(TimeSpan.FromMinutes(value: 5));

                        SubscriptionManager.AddConsolidator(contract.Symbol, consolidator);
                        consolidator.DataConsolidated += OnDataConsolidated;


                        _williamsRs[contract] = (new WilliamsPercentR(14));

                        RegisterIndicator(contract.Symbol, _williamsRs[contract], consolidator);

                        Log("Added new consolidator for " + contract.Symbol.Value);
                    }
                }
            }
        }

        private void OnDataConsolidated(object sender, TradeBar quoteBar)
        {
            var indicators = _williamsRs.Where(x => x.Key.Symbol == quoteBar.Symbol).ToList();
            

            foreach (var willR in indicators)
            {
                
                if (willR.Key.Expiry > Time.Date)
                {
                    // Drop expired indicators 
                    _williamsRs.Remove(willR.Key);
                }
                else
                {
                    if (willR.Value.IsReady)
                    {
                        // Implement your logic here 
                        if (willR.Value > 20)
                        {
                            MarketOrder(willR.Key.Symbol, 10);
                        }
                    }
                }
            }
        }
    }
}