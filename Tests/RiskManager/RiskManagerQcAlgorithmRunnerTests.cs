using NUnit.Framework;

namespace QuantConnect.Tests.RiskManager
{
    [TestFixture]
    public class RiskManagerQcAlgorithmRunnerTests
    {
        [Test]
        public void RunRiskManagerAlgorithm()
        {
            // Arrange
            string[] testsNames = { "IfUsedMarginIsAboveMaxExposureThenQuantityIsZero",
                                    "EntryQuantityAndStopLossIsCorrectlyEstimatedWhenUsdIsBaseCurrency",
                                    "EntryQuantityAndStopLossIsCorrectlyEstimatedWhenUsdIsNotBaseCurrency",
                                    "MaxEposurePerTradeIsObserved",
                                    "MaxRiskPerTradeIsLimitedByAnAmountOfMoney"};

            // Act and Assert 
            foreach (var test in testsNames)
            {
                Assert.DoesNotThrow(() => AlgorithmRunner.RunLocalBacktest(test), "Fail at " + test);
            }
        }
    }
}
