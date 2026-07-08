using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Defines chart generation parameters for built-in chart providers.
/// </summary>
public sealed record ChartGenerationOptions(
    Rgb8? ManualColor,
    int GrayscaleSteps,
    ChartLayoutDefinition Layout,
    RenderOutputMode OutputMode = RenderOutputMode.SdrSrgb,
    ToneMappingParameters? ToneMappingParameters = null,
    HdrColor? ManualHdrColor = null,
    ToneMappingMode ToneMappingMode = ToneMappingMode.DirectScRgb)
{
    public static ChartGenerationOptions Default { get; } = new(
        ManualColor: new Rgb8(255, 255, 255),
        GrayscaleSteps: 5,
        Layout: ChartLayoutDefinition.Default,
        OutputMode: RenderOutputMode.SdrSrgb,
        ToneMappingParameters: Rendering.ToneMappingParameters.Default,
        ManualHdrColor: null,
        ToneMappingMode: ToneMappingMode.DirectScRgb);
}

