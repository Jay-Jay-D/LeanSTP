namespace QuantConnect.Algorithm.CSharp
{
    public interface ITechnicalIndicatorSignal
    {
        bool IsReady { get; }
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
        WilliamsPercentR = 6,
        PercentagePriceOscillator=7,
        BollingerBands = 8,
    }
}