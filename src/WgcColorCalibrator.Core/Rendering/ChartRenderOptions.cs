using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Options supplied to a chart renderer.
/// </summary>
public sealed record ChartRenderOptions(
    ChartDefinition Chart,
    IReadOnlyList<PatchPlacement> Placements,
    double RasterizationScale,
    bool DebugOverlayEnabled,
    RenderOutputMode RequestedOutputMode,
    RenderOutputMode ActualOutputMode,
    ToneMappingParameters ToneMappingParameters,
    DisplayOutputMetadata? DisplayOutput = null,
    bool AllowHdrClippingExperiment = false,
    IReadOnlyList<string>? Warnings = null,
    SizeInt? ClientPhysicalSize = null,
    Point? ContentOrigin = null);
