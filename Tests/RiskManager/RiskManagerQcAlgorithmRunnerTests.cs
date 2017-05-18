using NUnit.Framework;

namespace QuantConnect.Tests.RiskManager
{
    [TestFixture]
    public class RiskManagerQcAlgorithmRunnerTests
    {
        [Test]
        [Ignore("QC account data should be configured in the config.json file.")]
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
