using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using QuantConnect.Brokerages;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;

namespace QuantConnect.Algorithm.CSharp
{
    public class SmaSarStrategy:QCAlgorithm
    {
        private readonly string[] forexTickers =
        {
            "EURUSD", "USDJPY", "USDCHF", "GBPUSD", "EURJPY", "EURCHF", "EURGBP", "GBPJPY"
        };

        private SimpleMovingAverage sma;
        private ParabolicStopAndReverse sar;
        private Symbol symbol;

        private bool closeAboveSma;
        private bool sarAboveClose;

        public override void Initialize()
        {
            // Set the basic algorithm parameters.
            SetStartDate(2015, 01, 01);
            SetEndDate(2016, 06, 30);
            SetCash(100000);

            SetBrokerageModel(BrokerageName.OandaBrokerage);

            symbol = AddForex("EURUSD", Resolution.Minute, Market.Oanda).Symbol;
            sma = SMA(symbol, 60);
            sar = PSAR(symbol);
        }

        public override void OnData(Slice slice)
        {
            if(!(slice.ContainsKey(symbol) && (sma.IsReady || sar.IsReady))) return;

            var price = (decimal)slice[symbol].Price;
            var pip = Securities[symbol].SymbolProperties.MinimumPriceVariation * 10m;
            var longEntrySignal = price > sma && sar > price;
            var shortEntrySignal = price < sma && sar < price;


            if (!Portfolio[symbol].Invested)
            {
                int entrydirection = 0;
                var quantity = 10000;
                if (longEntrySignal)
                {
                    entrydirection = 1;
                }
                if (shortEntrySignal)
                {
                    entrydirection = -1;
                }
                if (entrydirection != 0)
                {
                    quantity *= entrydirection;
                    var stopLoss = Math.Round(price - 15 * pip, 4);
                    var takeProfit = Math.Round(price + 10 * pip, 4);
                    MarketOrder(symbol, quantity, tag: "Entry");
                    StopMarketOrder(symbol, -quantity,stopLoss , "StopLoss");
                    StopMarketOrder(symbol, -quantity, takeProfit, "TakeProfit");
                }
            }
        }

        public override void OnOrderEvent(OrderEvent orderEvent)
        {
            switch (orderEvent.Status)
            {
                case OrderStatus.New:
                    break;
                case OrderStatus.Submitted:
                    break;
                case OrderStatus.PartiallyFilled:
                    break;
                case OrderStatus.Filled:
                    var order = Transactions.GetOrderById(orderEvent.OrderId);
                    if (order.Tag != "Entry")
                    {
                        Liquidate(orderEvent.Symbol);
                    }
                    break;
                case OrderStatus.Canceled:
                    break;
                case OrderStatus.None:
                    break;
                case OrderStatus.Invalid:
                    break;
                case OrderStatus.CancelPending:
                    break;
                default:
                    break;
            }


        }
    }
}
