using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.App.Rendering.Abstractions;

/// <summary>
/// Options supplied to a chart renderer.
/// </summary>
public sealed record ChartRenderOptions(
    ChartDefinition Chart,
    IReadOnlyList<PatchPlacement> Placements,
    double RasterizationScale,
    bool DebugOverlayEnabled,
    RenderOutputMode OutputMode,
    ToneMappingParameters ToneMappingParameters);
