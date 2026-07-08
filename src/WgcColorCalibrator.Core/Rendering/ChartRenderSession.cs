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
        ToneMappingParameters toneMappingParameters,
        DisplayOutputMetadata? displayOutput,
        IReadOnlyList<string> warnings,
        DateTimeOffset createdAt,
        uint? colorSpaceSupportFlags = null,
        int? setColorSpaceResult = null,
        string? actualColorSpace = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rendererId);
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentException.ThrowIfNullOrWhiteSpace(swapChainFormat);
        ArgumentException.ThrowIfNullOrWhiteSpace(dxgiColorSpace);
        ArgumentNullException.ThrowIfNull(toneMappingParameters);
        ArgumentNullException.ThrowIfNull(warnings);

        RendererId = rendererId;
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
        ToneMappingParameters = toneMappingParameters;
        DisplayOutput = displayOutput;
        Warnings = warnings;
        CreatedAt = createdAt;
        ColorSpaceSupportFlags = colorSpaceSupportFlags;
        SetColorSpaceResult = setColorSpaceResult;
        ActualColorSpace = actualColorSpace;
    }

    public string RendererId { get; }

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

    public ToneMappingParameters ToneMappingParameters { get; }

    public DisplayOutputMetadata? DisplayOutput { get; }

    public IReadOnlyList<string> Warnings { get; }

    public DateTimeOffset CreatedAt { get; }

    public uint? ColorSpaceSupportFlags { get; }

    public int? SetColorSpaceResult { get; }

    public string? ActualColorSpace { get; }

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
