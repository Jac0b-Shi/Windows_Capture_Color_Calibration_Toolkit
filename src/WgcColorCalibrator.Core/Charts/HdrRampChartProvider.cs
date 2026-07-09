using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Creates a linear scRGB HDR ramp for probing WGC HDR-to-BGRA8 mapping and clipping behavior.
/// </summary>
public sealed class HdrRampChartProvider : IChartProvider
{
    private static readonly float[] RampValues =
    [
        0.0f,
        0.125f,
        0.25f,
        0.5f,
        0.75f,
        1.0f,
        1.25f,
        1.5f,
        2.0f,
        4.0f
    ];

    public string Id => "hdr-ramp";

    public string NameResourceKey => "Chart.HdrRamp.Name";

    public string DescriptionResourceKey => "Chart.HdrRamp.Description";

    public ChartDefinition Create(ChartGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        ColorPatchDefinition[] patches = RampValues
            .Select(v => CreatePatch(v))
            .ToArray();

        var renderingParameters = new ChartRenderingParameters(
            options.OutputMode,
            options.ToneMappingParameters ?? ToneMappingParameters.Default,
            ColorSpaceConverter.ScrgbReferenceWhiteNits,
            ColorEncoding.LinearScRgb,
            options.ToneMappingMode);

        return new ChartDefinition(
            Id,
            "HDR scRGB Ramp",
            patches,
            options.Layout,
            new Dictionary<string, string>
            {
                ["providerId"] = Id,
                ["encoding"] = "linear-scRGB",
                ["rampValues"] = string.Join(
                    ",",
                    RampValues.Select(v => v.ToString(System.Globalization.CultureInfo.InvariantCulture)))
            },
            renderingParameters);
    }

    private static ColorPatchDefinition CreatePatch(float value)
    {
        // ExpectedColor is a low-dynamic-range surrogate for preview/export compatibility only.
        // The actual rendered HDR value is carried by HdrColor.
        byte preview = (byte)Math.Clamp(
            (int)Math.Round(value * 255.0f, MidpointRounding.AwayFromZero),
            0,
            255);

        Rgb8 previewColor = new(preview, preview, preview);
        string label = $"scRGB {value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}";

        return new ColorPatchDefinition(
            $"scrgb-{FormatIdValue(value)}",
            label,
            previewColor,
            "hdr-ramp",
            1.0,
            new Dictionary<string, string>
            {
                ["linearScRgb"] = value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            ColorEncoding.LinearScRgb,
            new HdrColor(value));
    }

    private static string FormatIdValue(float value)
    {
        return value
            .ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)
            .Replace(".", "_");
    }
}
