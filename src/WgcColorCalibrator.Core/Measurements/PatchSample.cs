using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Represents the sampled value for one chart patch.
/// </summary>
public sealed record PatchSample(
    string PatchId,
    SampleMethod Method,
    int SampleCount,
    Rgb8? Rgb8Value,
    RgbaFloat? FloatValue,
    ChannelStatistics Statistics,
    IReadOnlyList<string> Warnings);

