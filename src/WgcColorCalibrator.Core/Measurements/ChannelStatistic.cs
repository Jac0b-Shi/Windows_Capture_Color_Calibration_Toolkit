namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Statistics for one channel of a sampled patch.
/// </summary>
public sealed record ChannelStatistic(
    double Min,
    double Max,
    double Mean,
    double Median,
    double StandardDeviation,
    int UniqueValueCount);

