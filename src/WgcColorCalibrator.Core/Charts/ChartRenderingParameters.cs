using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Records the HDR rendering parameters associated with a chart definition.
/// </summary>
public sealed record ChartRenderingParameters
{
    public ChartRenderingParameters(
        RenderOutputMode requestedOutputMode,
        ToneMappingParameters toneMappingParameters,
        double targetLuminanceNits,
        ColorEncoding sourceEncoding,
        ToneMappingMode toneMappingMode = ToneMappingMode.DirectScRgb)
    {
        RequestedOutputMode = requestedOutputMode;
        ToneMappingParameters = toneMappingParameters ?? throw new ArgumentNullException(nameof(toneMappingParameters));
        TargetLuminanceNits = targetLuminanceNits;
        SourceEncoding = sourceEncoding;
        ToneMappingMode = toneMappingMode;
    }

    public RenderOutputMode RequestedOutputMode { get; }

    public ToneMappingParameters ToneMappingParameters { get; }

    public double TargetLuminanceNits { get; }

    public ColorEncoding SourceEncoding { get; }

    public ToneMappingMode ToneMappingMode { get; }

    public static ChartRenderingParameters Default { get; } = new(
        RenderOutputMode.SdrSrgb,
        ToneMappingParameters.Default,
        80.0,
        ColorEncoding.SrgbEncoded,
        ToneMappingMode.DirectScRgb);
}
