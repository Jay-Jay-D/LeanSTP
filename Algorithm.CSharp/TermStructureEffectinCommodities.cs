using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect
{
    /*
    *   Term Structure Effect in Commodities
    *   http://quantpedia.com/Screener/Details/22
    *
    *   It is generally accepted that futures markets provide insurance to
    *   hedgers by ensuring the transfer of price risk to speculators. 
    *   The insurance that net hedgers are willing to pay equals the premium 
    *   earned by speculators for this risk bearing. As commodity futures 
    *   returns directly relate to the propensity of hedgers to be net long or
    *   net short, it becomes natural to design an active strategy that buys 
    *   mostly backwardated contracts and shorts mostly contangoed contracts - 
    *   the strategy which exploits the term structure in commodities.
    *
    *   This simple strategy buys each month the 20% of commodities with
    *   the highest roll-returns and shorts the 20% of commodities with the
    *   lowest roll-returns and holds the long-short positions for one month. 
    *   The contracts in each quintile are equally-weighted. 
    *   The investment universe is all commodity futures contracts.
    */
    public class Quantpedia22 : QCAlgorithm
    {
        private readonly FuturesChains _chains = new FuturesChains();

        public override void Initialize()
        {
            SetStartDate(2016, 1, 1);
            SetEndDate(2016, 8, 20);
            SetCash(1000000);

            var tickers = new[]
            {
                Futures.Softs.Cocoa,
                Futures.Softs.Coffee,
                Futures.Grains.Corn,
                Futures.Softs.Cotton2,
                Futures.Grains.Oats,
                Futures.Softs.OrangeJuice,
                Futures.Grains.SoybeanMeal,
                Futures.Grains.SoybeanOil,
                Futures.Grains.Soybeans,
                Futures.Softs.Sugar11,
                Futures.Grains.Wheat,
                Futures.Meats.FeederCattle,
                Futures.Meats.LeanHogs,
                Futures.Meats.LiveCattle,
                Futures.Energies.CrudeOilWTI,
                Futures.Energies.HeatingOil,
                Futures.Energies.NaturalGas,
                Futures.Energies.Gasoline,
                Futures.Metals.Gold,
                Futures.Metals.Palladium,
                Futures.Metals.Platinum,
                Futures.Metals.Silver
            };

            foreach (var ticker in tickers)
            {
                var future = AddFuture(ticker);
                future.SetFilter(TimeSpan.Zero, TimeSpan.FromDays(90));
            }
        }

        // Saves the Futures Chains 
        public override void OnData(Slice slice)
        {
            foreach (var chain in slice.FutureChains)
            {
                if (chain.Value.Contracts.Count < 2) continue;

                if (!_chains.ContainsKey(chain.Key))
                    _chains.Add(chain.Key, chain.Value);

                _chains[chain.Key] = chain.Value;
            }
        }

        // Trades are only defined on end of day
        public override void OnEndOfDay()
        {
            /*
            * We are going to use TradingCalendar object to learn which are the 
            * next futures' expiration date and, if today is one of these days 
            * the algorithm will select the universe to open long-short positions
            */
            var expiryDates = TradingCalendar.GetDaysByType(TradingDayType.FutureExpiration, Time, EndDate);
            if (!expiryDates.Select(x => x.Date).Contains(Time.Date)) return;

            Liquidate();

            var quintile = (int) Math.Floor(_chains.Count / 5.0);
            var rollReturns = new Dictionary<Symbol, double>();

            foreach (var chain in _chains)
            {
                var contracts = chain.Value.OrderBy(x => x.Expiry);
                if (contracts.Count() < 2) continue;

                // R = (log(Pn) - log(Pd)) * 365 / (Td - Tn)
                // R - Roll returns
                // Pn - Nearest contract price
                // Pd - Distant contract price
                // Tn - Nearest contract expire date
                // Pd - Distant contract expire date

                var nearestContract = contracts.FirstOrDefault();
                var distantContract = contracts.ElementAtOrDefault(1);
                var priceNearest = nearestContract.LastPrice > 0
                    ? nearestContract.LastPrice
                    : (nearestContract.AskPrice + nearestContract.BidPrice) / 2m;
                var priceDistant = distantContract.LastPrice > 0
                    ? distantContract.LastPrice
                    : (distantContract.AskPrice + distantContract.BidPrice) / 2m;
                var logPriceNearest = Math.Log((double) priceNearest);
                var logPriceDistant = Math.Log((double) priceDistant);

                if (distantContract.Expiry == nearestContract.Expiry)
                {
                    Log("ERROR: Nearest and distant contracts with same expire!" + nearestContract);
                    continue;
                }

                var expireRange = 365 / (distantContract.Expiry - nearestContract.Expiry).TotalDays;

                rollReturns.Add(chain.Key, (logPriceNearest - logPriceDistant) * expireRange);
            }

            // Order positive roll returns
            var backwardation = rollReturns
                .OrderByDescending(x => x.Value)
                .Where(x => x.Value > 0)
                .Take(quintile)
                .ToDictionary(x => x.Key, y => y.Value);

            var contango = rollReturns
                .OrderBy(x => x.Value)
                .Where(x => x.Value < 0)
                .Take(quintile)
                .ToDictionary(x => x.Key, y => y.Value);

            // 
            var count = Math.Min(backwardation.Count(), contango.Count());
            if (count != quintile)
            {
                backwardation = backwardation.Take(count).ToDictionary(x => x.Key, y => y.Value);
                contango = contango.Take(count).ToDictionary(x => x.Key, y => y.Value);
            }

            //Log("backwardation: " + string.Join(", ", backwardation));
            //Log("     contango: " + string.Join(", ", contango));

            // We cannot long-short if count is zero
            if (count == 0)
            {
                _chains.Clear();
                return;
            }

            var weight = 1m / count;

            // Buy top backwardation
            foreach (var symbol in backwardation.Keys)
            {
                var contractSymbol = _chains[symbol].ElementAtOrDefault(1).Symbol;
                SetHoldings(contractSymbol, weight);
            }

            // Sell top contango
            foreach (var symbol in contango.Keys)
            {
                var contractSymbol = _chains[symbol].ElementAtOrDefault(1).Symbol;
                SetHoldings(contractSymbol, -weight);
            }

            _chains.Clear();
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            Log(orderEvent.ToString());
        }
    }
}