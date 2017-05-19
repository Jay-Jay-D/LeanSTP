using System;
using System.Linq;
using System.Text;
using DynamicExpresso;
using NUnit.Framework;
using QuantConnect.Algorithm.CSharp;
using QuantConnect.Indicators;
using QuantConnect.Tests.Indicators;

namespace QuantConnect.Tests.TradingStrategiesBasedOnGeneticAlgorithms
{
    internal class TrueIndicatorSignal : ITechnicalIndicatorSignal
    {
        public bool GetSignal()
        {
            return true;
        }
    }

    internal class FalseIndicatorSignal : ITechnicalIndicatorSignal
    {
        public bool GetSignal()
        {
            return false;
        }
    }

    [TestFixture]
    public class TradingStrategiesBasedOnGeneticAlgorithmsTest
    {
        [Test]
        public void DynamoExpressoStringBuilderTests()
        {
            var interpreter = new Interpreter();
            bool[] booleanArray = {true, false, true};

            string[] operators = {"and", "or"};

            var condition = new StringBuilder();

            for (var i = 0; i < operators.Length; i++)
            {
                var stringOperator = operators[i] == "and" ? "&&" : "||";
                condition.Append(booleanArray[i].ToString().ToLower() + stringOperator);
            }
            condition.Append(booleanArray.Last().ToString().ToLower());
            Assert.AreEqual("true&&false||true", condition.ToString());
            Assert.True(interpreter.Eval<bool>(condition.ToString()));
        }

        [Test]
        public void DynamoExpressoTests()
        {
            var interpreter = new Interpreter();
            var result = interpreter.Eval("true && false || true");
            Assert.True((bool) result);
        }

        [Test]
        public void EvaluateConditionalForFiveThechincalIndicator()
        {
            // Arrange
            ITechnicalIndicatorSignal[] IndicatorSignals =
            {
                new TrueIndicatorSignal(),
                new FalseIndicatorSignal(),
                new TrueIndicatorSignal(),
                new FalseIndicatorSignal(),
                new TrueIndicatorSignal()
            };
            string[] operators = {"and", "or", "and", "or"};
            var tradeRule = new TradingRule(IndicatorSignals, operators);
            // Act
            var actual = tradeRule.TradeRuleSignal;
            // Assert
            Assert.True(actual);
        }

        [Test]
        public void EvaluateConditionalForFourThechincalIndicator()
        {
            // Arrange
            ITechnicalIndicatorSignal[] IndicatorSignals =
            {
                new TrueIndicatorSignal(),
                new FalseIndicatorSignal(),
                new FalseIndicatorSignal(),
                new TrueIndicatorSignal()
            };
            string[] operators = {"and", "and", "and"};
            var tradeRule = new TradingRule(IndicatorSignals, operators);
            // Act
            var actual = tradeRule.TradeRuleSignal;
            // Assert
            Assert.False(actual);
        }

        [Test]
        [Ignore("Testing tests to tests :)")]
        public void SelectedOscillatorsCanBeHandledAsIIndicatorOfIBaseData()
        {
            var actualRsi = new RelativeStrengthIndex(14);
            var indicatorSIgnal = new OscillatorSignal(actualRsi);
            Assert.IsNotNull(indicatorSIgnal.Indicator);
            Assert.IsInstanceOf<RelativeStrengthIndex>(indicatorSIgnal.Indicator);
        }


        private static TestCaseData[] GetTestingAlgorithmNames()
        {
            //// Arrange
            string[] testsNames = {
                "RsiInstantiatedAndRegisteredCorrectly",
                "OscillatorGivesTheSignalsCorrectly"
            };

            return testsNames.Select(t => new TestCaseData(t).SetName(t)).ToArray();
        }

        //[Ignore("QC account data should be configured in the config.json file.")]
        [Category("TravisExclude")]
        [Test, TestCaseSource("GetTestingAlgorithmNames")]
        public void RunRiskManagerAlgorithm(string test)
        {
            Assert.DoesNotThrow(() => AlgorithmRunner.RunLocalBacktest(test), "Fail at " + test);
        }




    }
}