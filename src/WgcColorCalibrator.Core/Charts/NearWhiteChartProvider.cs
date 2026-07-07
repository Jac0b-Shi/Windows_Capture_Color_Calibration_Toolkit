using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Creates a small near-white chart for high-value, low-saturation samples.
/// </summary>
public sealed class NearWhiteChartProvider : IChartProvider
{
    public string Id => "near-white";

    public string NameResourceKey => "Chart.NearWhite.Name";

    public string DescriptionResourceKey => "Chart.NearWhite.Description";

    public ChartDefinition Create(ChartGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        Rgb8[] colors =
        [
            new(255, 255, 255),
            new(250, 250, 250),
            new(248, 248, 248),
            new(255, 248, 240),
            new(240, 248, 255),
            new(255, 252, 248),
            new(248, 252, 255),
            new(255, 250, 250)
        ];

        ColorPatchDefinition[] patches = colors
            .Select((color, index) => new ColorPatchDefinition(
                $"near-white-{index:00}",
                color.ToHexString(),
                color,
                "near-white",
                1.0,
                null))
            .ToArray();

        var renderingParameters = new ChartRenderingParameters(
            options.OutputMode,
            options.ToneMappingParameters ?? ToneMappingParameters.Default,
            ColorSpaceConverter.ScrgbReferenceWhiteNits,
            ColorEncoding.SrgbEncoded);

        return new ChartDefinition(
            Id,
            "Near White",
            patches,
            options.Layout,
            new Dictionary<string, string>
            {
                ["providerId"] = Id
            },
            renderingParameters);
    }
}

