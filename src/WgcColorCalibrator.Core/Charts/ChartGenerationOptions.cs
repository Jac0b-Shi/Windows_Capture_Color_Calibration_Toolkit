using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Defines chart generation parameters for built-in chart providers.
/// </summary>
public sealed record ChartGenerationOptions(
    Rgb8? ManualColor,
    int GrayscaleSteps,
    ChartLayoutDefinition Layout)
{
    public static ChartGenerationOptions Default { get; } = new(
        ManualColor: new Rgb8(255, 255, 255),
        GrayscaleSteps: 5,
        Layout: ChartLayoutDefinition.Default);
}

