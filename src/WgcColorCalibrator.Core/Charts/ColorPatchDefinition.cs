using System.Text.Json.Serialization;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering;

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
        : this(id, label, expectedColor, category, weight, metadata, ColorEncoding.SrgbEncoded, null)
    {
    }

    [JsonConstructor]
    public ColorPatchDefinition(
        string id,
        string label,
        Rgb8 expectedColor,
        string? category,
        double weight,
        IReadOnlyDictionary<string, string>? metadata,
        ColorEncoding sourceEncoding = ColorEncoding.SrgbEncoded,
        HdrColor? hdrColor = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentOutOfRangeException.ThrowIfNegative(weight);

        if (sourceEncoding is ColorEncoding.LinearScRgb or ColorEncoding.Hdr10St2084 && hdrColor is null)
        {
            throw new ArgumentException("HDR color is required for HDR source encoding.", nameof(hdrColor));
        }

        if (hdrColor is not null && (!hdrColor.Value.IsFinite || !hdrColor.Value.IsNonNegative))
        {
            throw new ArgumentOutOfRangeException(nameof(hdrColor), "HDR color values must be finite and non-negative.");
        }

        Id = id;
        Label = label;
        ExpectedColor = expectedColor;
        Category = category;
        Weight = weight;
        Metadata = metadata;
        SourceEncoding = sourceEncoding;
        HdrColor = hdrColor;
    }

    public string Id { get; }

    public string Label { get; }

    public Rgb8 ExpectedColor { get; }

    public string? Category { get; }

    public double Weight { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }

    public ColorEncoding SourceEncoding { get; }

    public HdrColor? HdrColor { get; }

    public bool IsHdrOnly => SourceEncoding is ColorEncoding.LinearScRgb or ColorEncoding.Hdr10St2084;
}
