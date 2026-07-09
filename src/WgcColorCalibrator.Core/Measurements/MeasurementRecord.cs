namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Records expected, optional display-observed, and captured values for one patch.
/// </summary>
public sealed record MeasurementRecord
{
    public MeasurementRecord(
        string patchId,
        ColorValue expected,
        ColorValue? displayObserved,
        ColorValue captured,
        SamplingSummary sampling,
        ChannelStatistics channelStatistics,
        MeasurementValidity validity,
        IReadOnlyList<string> warnings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchId);
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(captured);
        ArgumentNullException.ThrowIfNull(sampling);
        ArgumentNullException.ThrowIfNull(channelStatistics);
        ArgumentNullException.ThrowIfNull(warnings);

        PatchId = patchId;
        Expected = expected;
        DisplayObserved = displayObserved;
        Captured = captured;
        Sampling = sampling;
        ChannelStatistics = channelStatistics;
        Validity = validity;
        Warnings = warnings;
    }

    public string PatchId { get; }

    public ColorValue Expected { get; }

    public ColorValue? DisplayObserved { get; }

    public ColorValue Captured { get; }

    public SamplingSummary Sampling { get; }

    public ChannelStatistics ChannelStatistics { get; }

    public MeasurementValidity Validity { get; }

    public IReadOnlyList<string> Warnings { get; }
}
