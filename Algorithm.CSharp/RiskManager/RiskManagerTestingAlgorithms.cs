using QuantConnect.Data;
using QuantConnect.Orders.Fees;
using QuantConnect.Orders.Fills;

namespace QuantConnect.Algorithm.CSharp
{
    public abstract class RiskManagetTestsBaseAlgorithm : QCAlgorithm
    {
        public decimal _maxExposure = 0.8m;
        public decimal _maxExposurePerTrade = 0.3m;
        public decimal _riskPerTrade = 0.02m;

        public FxRiskManagment RiskManager { get; set; }

        public override void Initialize()
        {
            SetStartDate(2008, 11, 17);  //Set Start Date
            SetEndDate(2008, 11, 28);    //Set End Date
            SetCash(10000);               //Set Strategy Cash
            AddForex("USDJPY", Resolution.Daily, market: "oanda", leverage: 10);
            AddForex("EURUSD", Resolution.Daily, market: "oanda", leverage: 10);

            foreach (var pair in Securities.Keys)
            {
                Securities[pair].FeeModel = new ConstantFeeModel(0m);
                Securities[pair].FillModel = new ImmediateFillModel();
                var vol = 0.01m * (pair.Value == "USDJPY" ? 100 : 1);
                Securities[pair].VolatilityModel = new ConstantVolatilityModel(vol);
            }
            RiskManager = new FxRiskManagment(Portfolio, _riskPerTrade, _maxExposurePerTrade, _maxExposure);
        }
    }

    public class IfUsedMarginIsAboveMaxExposureThenQuantityIsZero : RiskManagetTestsBaseAlgorithm
    {
        public override void OnData(Slice slice)
        {
            // Arrange
            if (slice.Time.Day == 17)
            {
                MarketOrder("EURUSD", 65000);
            }
            // Act
            if (slice.Time.Day == 18)
            {
                var isMarginUsedBiggerThanMaxExposure = Portfolio.TotalPortfolioValue * _maxExposure < Portfolio.TotalMarginUsed;
                RuntimeStatistics["MarginUsedBiggerThanMaxExposure"] = isMarginUsedBiggerThanMaxExposure.ToString();
                var entryOrders = RiskManager.CalculateEntryOrders("EURUSD", EntryMarketDirection.GoLong);
                var isQuantityZero = entryOrders.Item1 == 0;
                RuntimeStatistics["IfUsedMarginIsAboveMaxExposureThenQuantityIsZero"] = isQuantityZero.ToString();
            }
        }
    }

    public class EntryQuantityAndStopLossIsCorrectlyEstimatedWhenUsdISBaseCurrency : RiskManagetTestsBaseAlgorithm
    {
        public override void OnData(Slice slice)
        {
            // Arrange
            var ticker = "EURUSD";
            // Act
            if (slice.Time.Day == 19)
            {
                var entryOrders = RiskManager.CalculateEntryOrders(ticker, EntryMarketDirection.GoLong);
                var stopLossPrice = slice[ticker].Close - Securities[ticker].VolatilityModel.Volatility;
                var isQuantityEstimatedCorrectly = entryOrders.Item1 == 20000;
                var isStopLossEstimatedCorrectly = entryOrders.Item2 == stopLossPrice;
                RuntimeStatistics["QuantityEstimatedCorrectly"] = isQuantityEstimatedCorrectly.ToString();
                RuntimeStatistics["StopLossEstimatedCorrectly"] = isStopLossEstimatedCorrectly.ToString();
            }
        }
    }

    public class EntryQuantityAndStopLossIsCorrectlyEstimatedWhenUsdIsNOTBaseCurrency : RiskManagetTestsBaseAlgorithm
    {
        public override void OnData(Slice slice)
        {
            // Arrange
            var ticker = "USDJPY";
            // Act
            if (slice.Time.Day == 19)
            {
                var entryOrders = RiskManager.CalculateEntryOrders(ticker, EntryMarketDirection.GoShort);
                var stopLossPrice = slice[ticker].Close + Securities[ticker].VolatilityModel.Volatility;
                var isQuantityEstimatedCorrectly = entryOrders.Item1 == -19000;
                var isStopLossEstimatedCorrectly = entryOrders.Item2 == stopLossPrice;
                RuntimeStatistics["QuantityEstimatedCorrectly"] = isQuantityEstimatedCorrectly.ToString();
                RuntimeStatistics["StopLossEstimatedCorrectly"] = isStopLossEstimatedCorrectly.ToString();
            }
        }
    }

    public class MaxEposurePerTradeIsObserved : RiskManagetTestsBaseAlgorithm
    {
        public override void OnData(Slice slice)
        {
            // Arrange
            _maxExposurePerTrade = 0.1m;
            RiskManager = new FxRiskManagment(Portfolio, _riskPerTrade, _maxExposurePerTrade, _maxExposure);
            var ticker = "EURUSD";
            // Act
            if (slice.Time.Day == 19)
            {
                var entryOrders = RiskManager.CalculateEntryOrders(ticker, EntryMarketDirection.GoLong);
                var isQuantityEstimatedCorrectly = entryOrders.Item1 == 7000;
                RuntimeStatistics["QuantityEstimatedCorrectly"] = isQuantityEstimatedCorrectly.ToString();
            }
        }
    }

}
