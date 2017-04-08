using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                                    "EntryQuantityAndStopLossIsCorrectlyEstimatedWhenUsdISBaseCurrency",
                                    "EntryQuantityAndStopLossIsCorrectlyEstimatedWhenUsdIsNOTBaseCurrency",
                                    "MaxEposurePerTradeIsObserved"};

            // Act and Assert 
            foreach (var test in testsNames)
            {
                Assert.DoesNotThrow(() => AlgorithmRunner.RunLocalBacktest(test), "Fail at " + test);
            }
        }
    }
}
