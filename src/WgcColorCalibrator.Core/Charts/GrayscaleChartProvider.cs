using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Creates a configurable grayscale chart that includes both endpoints.
/// </summary>
public sealed class GrayscaleChartProvider : IChartProvider
{
    public string Id => "grayscale";

    public string NameResourceKey => "Chart.Grayscale.Name";

    public string DescriptionResourceKey => "Chart.Grayscale.Description";

    public ChartDefinition Create(ChartGenerationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.GrayscaleSteps < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Grayscale steps must be at least 2.");
        }

        var patches = Enumerable.Range(0, options.GrayscaleSteps)
            .Select(index =>
            {
                byte value = (byte)Math.Round(index * 255.0 / (options.GrayscaleSteps - 1), MidpointRounding.AwayFromZero);
                var color = new Rgb8(value, value, value);
                string hex = color.ToHexString();
                return new ColorPatchDefinition(
                    $"gray-{value:000}",
                    hex,
                    color,
                    "grayscale",
                    1.0,
                    null);
            })
            .ToArray();

        var renderingParameters = new ChartRenderingParameters(
            options.OutputMode,
            options.ToneMappingParameters ?? ToneMappingParameters.Default,
            ColorSpaceConverter.ScrgbReferenceWhiteNits,
            ColorEncoding.SrgbEncoded);

        return new ChartDefinition(
            Id,
            "Grayscale",
            patches,
            options.Layout,
            new Dictionary<string, string>
            {
                ["providerId"] = Id,
                ["steps"] = options.GrayscaleSteps.ToString(System.Globalization.CultureInfo.InvariantCulture)
            },
            renderingParameters);
    }
}
