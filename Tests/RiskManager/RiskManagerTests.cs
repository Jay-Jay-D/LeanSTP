using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Tests.RiskManager;
using System;
using System.Collections.Generic;

namespace QuantConnect.Tests
{
    [TestFixture]
    public class RiskManagerTests
    {

        DateTime _firstObservation = new DateTime(2011, 11, 02);
        Dictionary<string, decimal[]> _rawData = new Dictionary<string, decimal[]>
        {
            {"EURUSD",  new decimal[] { 1.094175m, 1.094205m, 1.094365m, 1.09473m, 1.09414m,  1.09441m,  1.094605m, 1.094465m, 1.09452m,  1.09431m,
                                        1.09412m,  1.09417m,  1.09387m,  1.09394m, 1.094455m, 1.094465m, 1.09438m,  1.09469m,  1.094995m, 1.09499m } },
            {"USDJPY",  new decimal[] {99.289m,  99.2895m, 99.2835m, 99.283m,  99.2805m, 99.2785m, 99.281m,  99.276m,  99.2715m,  99.2475m,
                                       99.2405m, 99.2495m, 99.231m,  99.2285m, 99.219m,  99.221m,  99.222m,  99.222m,  99.2395m,  99.2325m } }
        };

        private decimal _maxExposure = 0.8m;
        private decimal _maxExposurePerTrade = 0.3m;
        private decimal _riskPerTrade = 0.02m;

        private SecurityPortfolioManager _portfolio;
        private SecurityManager _securities;
        private SecurityTransactionManager _securityTransactionManager;
        private SubscriptionManager _subscriptionManager;
        private MarketHoursDatabase _marketHoursDatabase;
        private SymbolPropertiesDatabase _symbolPropertiesDatabase;
        private ISecurityInitializer _securityInitializer;

        [TestFixtureSetUp]
        public void Setup()
        {
            var timeKeeper = new TimeKeeper(_firstObservation);
            _securities = new SecurityManager(timeKeeper);
            _portfolio = new SecurityPortfolioManager(_securities, _securityTransactionManager);
            _securityTransactionManager = new SecurityTransactionManager(_securities);
            _subscriptionManager = new SubscriptionManager(timeKeeper);
            _marketHoursDatabase = MarketHoursDatabase.FromDataFolder();
            _symbolPropertiesDatabase = SymbolPropertiesDatabase.FromDataFolder();
            _securityInitializer = SecurityInitializer.Null;

            foreach (var ticker in _rawData.Keys)
            {
                var forexSymbol = Symbol.Create(ticker, SecurityType.Forex, Market.FXCM);
                var forexMarketHoursDbEntry = _marketHoursDatabase.GetEntry(forexSymbol.ID.Market, forexSymbol, SecurityType.Forex);
                var forexDefaultQuoteCurrency = forexSymbol.Value.Substring(3);

                var forexSymbolProperties = _symbolPropertiesDatabase.GetSymbolProperties(forexSymbol.ID.Market, forexSymbol, forexSymbol.ID.SecurityType, forexDefaultQuoteCurrency);
                var subscriptionTypes = new List<Type> { typeof(QuoteBar) };

                var forex = SecurityManager.CreateSecurity(subscriptionTypes, _portfolio, _subscriptionManager, forexMarketHoursDbEntry.ExchangeHours,
                                                            forexMarketHoursDbEntry.DataTimeZone, forexSymbolProperties, _securityInitializer,
                                                            forexSymbol, Resolution.Minute,
                                                            true, 1m, false, false, false, false);
                forex.MarginModel = new SecurityMarginModel(10m);
                forex.FeeModel = new ConstantFeeModel(0m);
                forex.FillModel = new ImmediateFillModel();
                var vol = 0.01m * (ticker == "USDJPY" ? 100 : 1);
                forex.VolatilityModel = new ConstantVolatilityModel(vol);
                _securities.Add(forex);
            }
            _portfolio.SetCash(10000m);
        }



        [Test]
        public void IfTheUsedMarginIsBiggerThanTheMaxExposureThenTheEntryOrderQuantityMustBeZero()
        {
            // Arrange
            var ticker = "EURUSD";
            var symbol = _securities[ticker].Symbol;
            var time = _firstObservation;
            var fill = new OrderEvent(1, symbol, time, OrderStatus.Filled, OrderDirection.Buy, _rawData[ticker][0], 65000, 0);
            _portfolio.ProcessFill(fill);
            var riskManager = new FxRiskManagment(_portfolio, _riskPerTrade, _maxExposurePerTrade, _maxExposure);
            // Act
            var entryOrders = riskManager.CalculateEntryOrders(symbol, EntryMarketDirection.GoLong);
            // Assert
            Assert.AreEqual(0, entryOrders.Item1);
        }

        /// <summary>
        /// Case 1
        /// </summary>
        [Test]
        public void EntryQuantityAndStopLossIsCorrectlyEstimatedWhenUsdISBaseCurrency()
        {
            // Arrange
            var ticker = "EURUSD";
            var symbol = _securities[ticker].Symbol;
            var time = _firstObservation;
            _securities[ticker].SetMarketPrice(new TradeBar
            {
                Time = time,
                Close = _rawData[ticker][0]
            });
            var riskManager = new FxRiskManagment(_portfolio, _riskPerTrade, _maxExposurePerTrade, _maxExposure);
            var expectedQuantity = 20000;
            var expectedStopLossPrice = _rawData[ticker][0] - 0.01m;

            // Act
            var entryOrders = riskManager.CalculateEntryOrders(symbol, EntryMarketDirection.GoLong);
            // Assert
            Assert.AreEqual(expectedQuantity, entryOrders.Item1);
            Assert.AreEqual(expectedStopLossPrice, entryOrders.Item2);
        }

        /// <summary>
        /// Case 2
        /// </summary>
        [Test]
        [Ignore("The exchange rate isn't estimated yet.")]
        public void EntryQuantityAndStopLossIsCorrectlyEstimatedWhenUsdIsNOTBaseCurrency()
        {
            // Arrange
            var ticker = "USDJPY";
            var symbol = _securities[ticker].Symbol;
            var time = _firstObservation;
            _securities[ticker].SetMarketPrice(new TradeBar
            {
                Time = time,
                Close = _rawData[ticker][0]
            });
            var riskManager = new FxRiskManagment(_portfolio, _riskPerTrade, _maxExposurePerTrade, _maxExposure);
            var expectedQuantity = 19000;
            var expectedStopLossPrice = _rawData[ticker][0] - 1m;

            // Act
            var entryOrders = riskManager.CalculateEntryOrders(symbol, EntryMarketDirection.GoLong);
            // Assert
            Assert.AreEqual(expectedQuantity, entryOrders.Item1);
            Assert.AreEqual(expectedStopLossPrice, entryOrders.Item2);
        }
    }
}