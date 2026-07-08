using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Records everything known about a chart rendering that may be used for later capture and measurement.
/// </summary>
public sealed record ChartRenderSession
{
    public ChartRenderSession(
        string rendererId,
        string? toneMapperId,
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> placements,
        RenderOutputMode requestedOutputMode,
        RenderOutputMode actualOutputMode,
        string swapChainFormat,
        string dxgiColorSpace,
        bool hdrOutputActive,
        double rasterizationScale,
        Size logicalSize,
        SizeInt intendedPhysicalSize,
        SizeInt actualPhysicalSize,
        SizeInt clientPhysicalSize,
        Point contentOrigin,
        ToneMappingParameters toneMappingParameters,
        DisplayOutputMetadata? displayOutput,
        IReadOnlyList<string> warnings,
        DateTimeOffset createdAt,
        double compositionScaleX,
        double compositionScaleY,
        string matrixTransform,
        uint? colorSpaceSupportFlags = null,
        int? setColorSpaceResult = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rendererId);
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentException.ThrowIfNullOrWhiteSpace(swapChainFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(dxgiColorSpace);
        ArgumentException.ThrowIfNullOrWhiteSpace(matrixTransform);
        ArgumentNullException.ThrowIfNull(toneMappingParameters);
        ArgumentNullException.ThrowIfNull(warnings);

        RendererId = rendererId;
        ToneMapperId = toneMapperId;
        Chart = chart;
        Placements = placements;
        RequestedOutputMode = requestedOutputMode;
        ActualOutputMode = actualOutputMode;
        SwapChainFormat = swapChainFormat;
        DxgiColorSpace = dxgiColorSpace;
        HdrOutputActive = hdrOutputActive;
        RasterizationScale = rasterizationScale;
        LogicalSize = logicalSize;
        IntendedPhysicalSize = intendedPhysicalSize;
        ActualPhysicalSize = actualPhysicalSize;
        ClientPhysicalSize = clientPhysicalSize;
        ContentOrigin = contentOrigin;
        ToneMappingParameters = toneMappingParameters;
        DisplayOutput = displayOutput;
        Warnings = warnings;
        CreatedAt = createdAt;
        CompositionScaleX = compositionScaleX;
        CompositionScaleY = compositionScaleY;
        MatrixTransform = matrixTransform;
        ColorSpaceSupportFlags = colorSpaceSupportFlags;
        SetColorSpaceResult = setColorSpaceResult;
    }

    public string RendererId { get; }

    public string? ToneMapperId { get; }

    public ChartDefinition Chart { get; }

    public IReadOnlyList<PatchPlacement> Placements { get; }

    public RenderOutputMode RequestedOutputMode { get; }

    public RenderOutputMode ActualOutputMode { get; }

    public string SwapChainFormat { get; }

    public string DxgiColorSpace { get; }

    public bool HdrOutputActive { get; }

    public double RasterizationScale { get; }

    public Size LogicalSize { get; }

    public SizeInt IntendedPhysicalSize { get; }

    public SizeInt ActualPhysicalSize { get; }

    public SizeInt ClientPhysicalSize { get; }

    public Point ContentOrigin { get; }

    public ToneMappingParameters ToneMappingParameters { get; }

    public DisplayOutputMetadata? DisplayOutput { get; }

    public IReadOnlyList<string> Warnings { get; }

    public DateTimeOffset CreatedAt { get; }

    public double CompositionScaleX { get; }

    public double CompositionScaleY { get; }

    public string MatrixTransform { get; }

    public uint? ColorSpaceSupportFlags { get; }

    public int? SetColorSpaceResult { get; }

    public bool IsDebugOverlayEnabled => Warnings.Contains("debug-overlay-enabled");
}

/// <summary>
/// DIP size.
/// </summary>
public readonly record struct Size(double Width, double Height);

/// <summary>
/// Integer physical pixel size.
/// </summary>
public readonly record struct SizeInt(int Width, int Height);

/// <summary>
/// Content origin point in physical pixels.
/// </summary>
public readonly record struct Point(double X, double Y);
