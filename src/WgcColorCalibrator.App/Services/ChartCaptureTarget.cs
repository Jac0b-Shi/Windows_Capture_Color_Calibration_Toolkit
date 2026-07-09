using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Read-only snapshot of the current chart capture target.
/// </summary>
public sealed record ChartCaptureTarget(
    nint WindowHandle,
    ChartDefinition Chart,
    IReadOnlyList<PatchPlacement> Placements,
    ChartRenderSession RenderSession,
    bool IsSurfaceReady,
    bool IsDebugOverlayEnabled,
    bool AreParametersDirty);
