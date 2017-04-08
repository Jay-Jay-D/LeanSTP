using System;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Algorithm.CSharp.RiskManager;
using QuantConnect.Data;
using QuantConnect.Indicators;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable PossibleInvalidOperationException

namespace QuantConnect.Algorithm.CSharp
{
    public class RiskManagerTestingAlgorithm : QCAlgorithm
    {
        private const decimal MaxExposure = 0.8m;
        private const decimal MaxExposurePerTrade = 0.3m;
        private const decimal RiskPerTrade = 0.02m;

        private bool _runMarginTest = true;

        public List<Identity> PriceIndicators;

        private FxRiskManagment _riskManager;

        public override void Initialize()
        {
            SetStartDate(2008, 11, 17); //Set Start Date
            SetEndDate(2008, 11, 28); //Set End Date
            SetCash(10000); //Set Strategy Cash
            AddForex("USDJPY", Resolution.Daily, "oanda", leverage: 10);
            AddForex("EURUSD", Resolution.Daily, "oanda", leverage: 10);
            var gbpResolution = LiveMode ? Resolution.Minute : Resolution.Daily;
            AddForex("GBPUSD", gbpResolution, "oanda", leverage: 20);

            PriceIndicators = new List<Identity>();

            foreach (var pair in Securities.Keys)
            {
                Securities[pair].FeeModel = new ConstantFeeModel(0m);
                Securities[pair].FillModel = new ImmediateFillModel();
                var vol = 0.01m * (pair.Value == "USDJPY" ? 100 : 1);
                Securities[pair].VolatilityModel = new ConstantVolatilityModel(vol);
                PriceIndicators.Add(Identity(pair));
            }
            _riskManager = new FxRiskManagment(Portfolio, RiskPerTrade, MaxExposurePerTrade, MaxExposure);
        }

        public override void OnData(Slice slice)
        {
            Tuple<int, decimal> entryOrders;

            if (!LiveMode)
            {
                #region Test: if the used margin is bigger than the max exposure, then the entry order quantity must be zero.

                // Buy EURUSD beyond the max exposure.
                if (slice.Time.Day == 17)
                {
                    MarketOrder("EURUSD", 65000);
                }

                if (slice.Time.Day == 18)
                {
                    // Test 1: if the used margin is bigger than the max exposure, then the entry order quantity must be zero.
                    entryOrders = _riskManager.CalculateEntryOrders("EURUSD", EntryMarketDirection.GoLong);
                    if (entryOrders.Item1 != 0)
                    {
                        throw new Exception("The RiskManager allows operations beyond the max exposure.");
                    }
                    // Clean Stuff and set up the next test.
                    Liquidate("EURUSD");
                }

                #endregion Test: if the used margin is bigger than the max exposure, then the entry order quantity must be zero.

                #region Tests: Happy path.

                decimal stopLossPrice;
                if (slice.Time.Day == 19)
                {
                    Portfolio.SetCash(10000m);
                    // Test 2: happy path with a long entry with USD as base currency.
                    entryOrders = _riskManager.CalculateEntryOrders("EURUSD", EntryMarketDirection.GoLong);
                    stopLossPrice = slice["EURUSD"].Close - Securities["EURUSD"].VolatilityModel.Volatility;
                    if (entryOrders.Item1 != 20000 || entryOrders.Item2 != stopLossPrice)
                    {
                        throw new Exception(
                            "Quantity or StopLoss price estimated incorrectly when USD is the base currency.");
                    }

                    // Test 3: estimate a short entry orders with JPY as base currency.

                    entryOrders = _riskManager.CalculateEntryOrders("USDJPY", EntryMarketDirection.GoShort);
                    stopLossPrice = slice["USDJPY"].Close + Securities["USDJPY"].VolatilityModel.Volatility;
                    if (entryOrders.Item1 != -19000 || entryOrders.Item2 != stopLossPrice)
                    {
                        throw new Exception(
                            "Quantity or StopLoss price estimated incorrectly when USD is not the base currency.");
                    }
                    MarketOrder("USDJPY", entryOrders.Item1);
                    // Stop price high to avoid execution.
                    StopMarketOrder("USDJPY", -entryOrders.Item1, 98);
                }

                if (slice.Time.Day == 20)
                {
                    // Test 4: estimate a new long entry order with USD as base currency.
                    entryOrders = _riskManager.CalculateEntryOrders("EURUSD", EntryMarketDirection.GoLong);
                    stopLossPrice = slice["EURUSD"].Close - Securities["EURUSD"].VolatilityModel.Volatility;
                    if (entryOrders.Item1 != 21000 || entryOrders.Item2 != stopLossPrice)
                    {
                        throw new Exception(
                            "Quantity or StopLoss price estimated incorrectly when USD is the base currency.");
                    }
                    MarketOrder("EURUSD", 2 * entryOrders.Item1);
                    StopMarketOrder("EURUSD", -entryOrders.Item1, entryOrders.Item2);
                }

                #endregion Tests: Happy path.

                #region Test: Update trailing orders.

                if (slice.Time.Day == 21)
                {
                    // Test 5: update trailing orders
                    _riskManager.UpdateTrailingStopOrders();
                    var tickets =
                        Transactions.GetOrderTickets(o => o.OrderType == OrderType.StopMarket &&
                                                          o.Status == OrderStatus.Submitted);
                    foreach (var ticket in tickets)
                    {
                        var actualStopLossPrice = ticket.UpdateRequests.First().StopPrice;
                        var expectedStopLossPrice = Securities[ticket.Symbol].Price +
                                                    Securities[ticket.Symbol].VolatilityModel.Volatility *
                                                    (ticket.Quantity < 0 ? -1 : 1);
                        var areAlmostEqual = Math.Abs((decimal) actualStopLossPrice - expectedStopLossPrice) < 0.0001m;
                        if (!areAlmostEqual)
                        {
                            throw new Exception("Trailing stop loss fail.");
                        }
                    }
                    // Clean all stuff for the next test.
                    Liquidate("EURUSD");
                    Liquidate("USDJPY");
                }

                #endregion Test: Update trailing orders.
            }

            #region Test: Margin use and Leverage.

            var testDay = 23;
            if (LiveMode) testDay = DateTime.Today.Day;

            if (slice.Time.Day == testDay && _runMarginTest)
            {
                // Get the actual portfolio value
                var marginPreOrder = Portfolio.MarginRemaining;
                entryOrders = _riskManager.CalculateEntryOrders("GBPUSD", EntryMarketDirection.GoLong);
                var orderQuantity = entryOrders.Item1;
                var pairLeverage = Securities["GBPUSD"].Leverage;
                var pairRate = Securities["GBPUSD"].Price;
                var expectedMarginPostOrder = marginPreOrder - orderQuantity * pairRate / pairLeverage;
                var orderTicket = MarketOrder("GBPUSD", entryOrders.Item1);

                if (LiveMode)
                    do
                    {
                    } while (orderTicket.Status == OrderStatus.Filled);

                // The difference can be one because of the default fees.

                var actualdMarginPostOrder = Portfolio.MarginRemaining;
                var difference = Math.Abs((double) (actualdMarginPostOrder - expectedMarginPostOrder));
                var differenceTolerance = 5;
                if (difference > differenceTolerance)
                {
                    Liquidate("GBPUSD");
                    var errorMsg = string.Format(
                        @"Expected margin post order fill: {0}\nActual margin: {1}.\nDifference: {2}",
                        expectedMarginPostOrder, actualdMarginPostOrder, difference);
                    throw new Exception(errorMsg);
                }
                _runMarginTest = false;
                Liquidate("GBPUSD");
            }

            #endregion Test: Margin use and Leverage.
        }
    }
}