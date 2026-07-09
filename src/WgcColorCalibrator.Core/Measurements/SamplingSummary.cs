namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Describes the sample method and number of source pixels used.
/// </summary>
public sealed record SamplingSummary
{
    public SamplingSummary(SampleMethod method, int sampleCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(sampleCount);

        Method = method;
        SampleCount = sampleCount;
    }

    public SampleMethod Method { get; }

    public int SampleCount { get; }
}
