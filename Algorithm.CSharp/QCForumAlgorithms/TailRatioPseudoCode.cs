using System;
using System.Collections.Generic;
using MathNet.Numerics.Statistics;
using QuantConnect.Indicators;

namespace QuantConnect.Algorithm.CSharp
{
    public class TailRatioPseudoCode : QCAlgorithm
    {
        private List<double> dailyResults;
        private LogReturn equityLogReturn;

        public override void Initialize()
        {
            equityLogReturn = new LogReturn(1);
            dailyResults = new List<double>();
        }

        public override void OnEndOfDay()
        {
            equityLogReturn.Update(new IndicatorDataPoint
            {
                Time = Time.Date,
                Value = Portfolio.TotalPortfolioValue
            });
            dailyResults.Add((double) equityLogReturn.Current.Value);
        }

        public override void OnEndOfAlgorithm()
        {
            var tailRatio = dailyResults.Percentile(95) / Math.Abs(dailyResults.Percentile(5));
        }
    }
}