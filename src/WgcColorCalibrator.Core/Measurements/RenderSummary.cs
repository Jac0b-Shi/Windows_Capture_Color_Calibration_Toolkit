using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// A render summary without chart definition or placements to avoid duplicating measurement session data.
/// </summary>
public sealed record RenderSummary(
    string RendererId,
    string? ToneMapperId,
    RenderOutputMode RequestedOutputMode,
    RenderOutputMode ActualOutputMode,
    string SwapChainFormat,
    string DxgiColorSpace,
    bool HdrOutputActive,
    ToneMappingParameters ToneMappingParameters,
    SizeInt IntendedPhysicalSize,
    SizeInt ActualPhysicalSize,
    SizeInt ClientPhysicalSize,
    PixelPoint ContentOrigin,
    double CompositionScaleX,
    double CompositionScaleY,
    string MatrixTransform,
    DisplayOutputMetadata? DisplayOutput,
    IReadOnlyList<string> Warnings);
