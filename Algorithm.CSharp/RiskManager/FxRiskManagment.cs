using System;
using System.Collections.Generic;
using QuantConnect.Orders;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.CSharp.RiskManager
{

    public enum LotSize
    {
        Standard = 100000,
        Mini = 10000,
        Micro = 1000,
        Nano = 100,
    }

    public class FxRiskManagment
    {
        // Maximum equity proportion to put at risk in a single operation.
        private decimal _riskPerTrade;

        // Maximum equity proportion at risk in open positions in a given time.
        private decimal _maxExposure;

        // Maximum equity proportion at risk in a single trade.
        private decimal _maxExposurePerTrade;

        private int _lotSize;

        private int _minQuantity;

        private Dictionary<Symbol, int> RoundToPip;

        private SecurityPortfolioManager _portfolio;

        /// <summary>
        /// Initializes a new instance of the <see cref="FxRiskManagment"/> class.
        /// </summary>
        /// <param name="portfolio">The QCAlgorithm Portfolio.</param>
        /// <param name="riskPerTrade">The max risk per trade.</param>
        /// <param name="maxExposurePerTrade">The maximum exposure per trade.</param>
        /// <param name="maxExposure">The maximum exposure in all trades.</param>
        /// <param name="lotsize">The minimum quantity to trade.</param>
        /// <exception cref="System.NotImplementedException">The pairs should be added to the algorithm before initialize the risk manager.</exception>
        public FxRiskManagment(SecurityPortfolioManager portfolio, decimal riskPerTrade, decimal maxExposurePerTrade,
                               decimal maxExposure, LotSize lotsize = LotSize.Micro, int minQuantity = 5)
        {
            _portfolio = portfolio;
            if (_portfolio.Securities.Count == 0)
            {
                throw new NotImplementedException("The pairs should be added to the algorithm before initialize the risk manager.");
            }
            this._riskPerTrade = riskPerTrade;
            _maxExposurePerTrade = maxExposurePerTrade;
            this._maxExposure = maxExposure;
            _lotSize = (int)lotsize;
            _minQuantity = minQuantity;

            RoundToPip = new Dictionary<Symbol, int>();

            foreach (var symbol in _portfolio.Securities.Keys)
            {
                RoundToPip[symbol] = -(int)Math.Log10((double)_portfolio.Securities[symbol].SymbolProperties.MinimumPriceVariation * 10);
            }

        }

        /// <summary>
        /// Calculates the entry orders and stop-loss price.
        /// </summary>
        /// <param name="pair">The Forex pair Symbol.</param>
        /// <param name="action">The order direction.</param>
        /// <returns>a Tuple with the quantity as Item1 and the stop-loss price as Item2. If quantity is zero, then means that no trade must be done.</returns>
        public Tuple<int, decimal> CalculateEntryOrders(Symbol pair, EntryMarketDirection action, decimal? maxMoneyAtRisk = null)
        {
            // If exposure is greater than the max exposure, then return zero.
            if (_portfolio.TotalMarginUsed > _portfolio.TotalPortfolioValue * _maxExposure)
            {
                return Tuple.Create(0, 0m);
            }

            var quantity = 0;
            var stopLossPrice = 0m;
            try
            {
                var closePrice = _portfolio.Securities[pair].Price;
                var leverage = _portfolio.Securities[pair].Leverage;
                var exchangeRate = _portfolio.Securities[pair].QuoteCurrency.ConversionRate;
                var volatility = _portfolio.Securities[pair].VolatilityModel.Volatility;

                // Estimate the maximum entry order quantity given the risk per trade.
                var moneyAtRisk = _portfolio.TotalPortfolioValue * _riskPerTrade;
                if (maxMoneyAtRisk != null)
                {
                    moneyAtRisk = Math.Min(moneyAtRisk, (decimal)maxMoneyAtRisk);
                }

                decimal maxQuantitybyRisk;
                try
                {
                    maxQuantitybyRisk = moneyAtRisk / (volatility * exchangeRate);
                }
                catch (DivideByZeroException)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(string
                                          .Format("Dividing by zero estimating maxQuantitybyRisk. Volatility: {0:F6}, ExchangeRate: {1:F6}",
                                                  volatility, exchangeRate));
                    Console.WriteLine("Set maxQuantitybyRisk equal to min quantity.");
                    Console.ResetColor();
                    maxQuantitybyRisk = _lotSize * _minQuantity;
                }

                // Estimate the maximum entry order quantity given the exposure per trade.
                var maxBuySize = Math.Min(_portfolio.MarginRemaining, _portfolio.TotalPortfolioValue * _maxExposurePerTrade) * leverage;
                decimal maxQuantitybyExposure;
                try
                {
                    maxQuantitybyExposure = maxBuySize / (closePrice * exchangeRate);
                }
                catch
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine(string
                                          .Format("Dividing by zero estimating maxQuantitybyExposure. ClosePrice: {0:F6}, ExchangeRate: {1:F6}",
                                                  closePrice, exchangeRate));
                    Console.WriteLine("Set maxQuantitybyExposure equal to min quantity.");
                    Console.ResetColor();
                    maxQuantitybyExposure = _lotSize * _minQuantity;
                }

                // The final quantity is the lowest of both.
                quantity = (int)(Math.Floor(Math.Min(maxQuantitybyRisk, maxQuantitybyExposure) / _lotSize) * _lotSize);
                // If the final quantity is lower than the minimum quantity of the given lot size, then return zero.
                if (quantity < _lotSize * _minQuantity) return Tuple.Create(0, 0m);

                quantity = action == EntryMarketDirection.GoLong ? quantity : -quantity;
                stopLossPrice = closePrice + (action == EntryMarketDirection.GoLong ? -volatility : volatility);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return Tuple.Create(quantity, stopLossPrice);
        }

        /// <summary>
        /// Updates the stop-loss price of all open StopMarketOrders.
        /// </summary>
        public void UpdateTrailingStopOrders()
        {
            // Get all the open
            var openStopLossOrders = _portfolio.Transactions.GetOrderTickets(o => o.OrderType == OrderType.StopMarket && o.Status == OrderStatus.Submitted);
            foreach (var ticket in openStopLossOrders)
            {
                var stopLossPrice = ticket.SubmitRequest.StopPrice;
                var volatility = _portfolio.Securities[ticket.Symbol].VolatilityModel.Volatility;
                var actualPrice = _portfolio.Securities[ticket.Symbol].Price;
                // The StopLossOrder has the opposite direction of the original order.
                var originalOrderDirection = ticket.Quantity > 0 ? OrderDirection.Sell : OrderDirection.Buy;
                var newStopLossPrice = actualPrice + (volatility * (originalOrderDirection == OrderDirection.Buy ? -1 : 1));
                if ((originalOrderDirection == OrderDirection.Buy && newStopLossPrice > stopLossPrice)
                    || (originalOrderDirection == OrderDirection.Sell && newStopLossPrice < stopLossPrice))
                {
                    ticket.Update(new UpdateOrderFields { StopPrice = Math.Round(newStopLossPrice, RoundToPip[ticket.Symbol]) });
                }
            }
        }
    }
}
