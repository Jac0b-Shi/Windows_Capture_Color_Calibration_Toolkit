using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Defines a stable expected color patch in a chart.
/// </summary>
public sealed record ColorPatchDefinition
{
    public ColorPatchDefinition(
        string id,
        string label,
        Rgb8 expectedColor,
        string? category,
        double weight,
        IReadOnlyDictionary<string, string>? metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentOutOfRangeException.ThrowIfNegative(weight);

        Id = id;
        Label = label;
        ExpectedColor = expectedColor;
        Category = category;
        Weight = weight;
        Metadata = metadata;
    }

    public string Id { get; }

    public string Label { get; }

    public Rgb8 ExpectedColor { get; }

    public string? Category { get; }

    public double Weight { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }
}
