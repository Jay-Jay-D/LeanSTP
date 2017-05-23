namespace QuantConnect.Algorithm.CSharp
{
    public interface ITechnicalIndicatorSignal
    {
        bool GetSignal();
    }

    public enum TradeRuleDirection
    {
        LongOnly = 1,
        ShortOnly = -1
    }

    public enum TechicalIndicators
    {
        SimpleMovingAverage = 0,
        MovingAverageConvergenceDivergence = 1,
        Stochastic = 2,
        RelativeStrengthIndex = 3,
        CommodityChannelIndex = 4,
        MomentumPercent = 5,
        BollingerBands = 6,
        WilliamsPercentR = 7,
        PercentagePriceOscillator=9,
    }
}