namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Creates a chart containing one manually selected color.
/// </summary>
public sealed class ManualSingleColorChartProvider : IChartProvider
{
    public string Id => "manual-single-color";

    public string NameResourceKey => "Chart.ManualSingleColor.Name";

    public string DescriptionResourceKey => "Chart.ManualSingleColor.Description";

    public ChartDefinition Create(ChartGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var color = options.ManualColor ?? throw new ArgumentException(
            "Manual color is required for the manual single color chart.",
            nameof(options));

        string hex = color.ToHexString();
        string patchId = "manual-" + hex[1..].ToLowerInvariant();

        var patch = new ColorPatchDefinition(
            patchId,
            hex,
            color,
            "manual",
            1.0,
            null);

        return new ChartDefinition(
            Id,
            "Manual Single Color",
            [patch],
            options.Layout,
            new Dictionary<string, string>
            {
                ["providerId"] = Id
            });
    }
}

