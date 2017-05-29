namespace QuantConnect.Algorithm.CSharp
{
    /// <summary>
    ///     Interface used by the <see cref="TradingRule" /> class to flag technical indicator signals as crossing moving
    ///     averages or oscillators crossing its thresholds.
    /// </summary>
    public interface ITechnicalIndicatorSignal
    {
        /// <summary>
        ///     Gets a value indicating whether this instance is ready.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this instance is ready; otherwise, <c>false</c>.
        /// </value>
        bool IsReady { get; }

        /// <summary>
        ///     Gets the signal. Only used if the instance will be part of a <see cref="TradingRule" /> class.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the actual <see cref="Signal" /> correspond with the instance <see cref="TradeRuleDirection" />.
        ///     <c>false</c>
        ///     otherwise.
        /// </returns>
        bool GetSignal();
    }

    /// <summary>
    ///     The <see cref="TradingStrategiesBasedOnGeneticAlgorithms" /> implementation requires a direction for every
    ///     technical indicator.
    /// </summary>
    public enum TradeRuleDirection
    {
        LongOnly = 1,
        ShortOnly = -1
    }

    /// <summary>
    ///     List of the technical indicator implemented... well not really, Bollinger bands wasn't implemented.
    /// </summary>
    public enum TechicalIndicators
    {
        SimpleMovingAverage = 0,
        MovingAverageConvergenceDivergence = 1,
        Stochastic = 2,
        RelativeStrengthIndex = 3,
        CommodityChannelIndex = 4,
        MomentumPercent = 5,
        WilliamsPercentR = 6,
        PercentagePriceOscillator = 7,
        BollingerBands = 8
    }
}