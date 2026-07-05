namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// RGB channel statistics for a sampled patch.
/// </summary>
public sealed record ChannelStatistics(
    ChannelStatistic R,
    ChannelStatistic G,
    ChannelStatistic B);

