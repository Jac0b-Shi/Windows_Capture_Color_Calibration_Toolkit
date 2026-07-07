using Vortice.Direct3D11;
using Vortice.DXGI;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Direct3D 11 renderer that draws the chart to a physical-pixel swap chain hosted in a SwapChainPanel.
/// </summary>
public sealed class D3D11ChartRenderer : IChartRenderer, IDisposable
{
    private readonly D3D11DeviceResources _deviceResources;
    private readonly TextureChartRenderer _textureRenderer;
    private readonly Dictionary<object, SwapChainPanelHost> _hosts = new();
    private bool _disposed;

    public D3D11ChartRenderer(D3D11DeviceResources deviceResources)
    {
        _deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
        _textureRenderer = new TextureChartRenderer(deviceResources);
    }

    public string RendererId => "d3d11";

    public ChartRenderSession Render(
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> placements,
        ChartRenderOptions options,
        object host)
    {
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(host);

        if (host is not Microsoft.UI.Xaml.Controls.SwapChainPanel panel)
        {
            throw new ArgumentException("Host must be a SwapChainPanel.", nameof(host));
        }

        var warnings = new List<string>();
        SizeInt intendedPhysicalSize = CalculateIntendedPhysicalSize(placements, chart.Layout);
        SizeInt actualPhysicalSize = GetActualPhysicalSize(panel, options.RasterizationScale);

        if (actualPhysicalSize.Width != intendedPhysicalSize.Width ||
            actualPhysicalSize.Height != intendedPhysicalSize.Height)
        {
            warnings.Add($"size-mismatch: intended={intendedPhysicalSize.Width}x{intendedPhysicalSize.Height}, actual={actualPhysicalSize.Width}x{actualPhysicalSize.Height}");
        }

        if (options.DebugOverlayEnabled)
        {
            warnings.Add("debug-overlay-enabled");
        }

        RenderOutputMode actualOutputMode = ResolveOutputMode(options.OutputMode, warnings);
        (Format format, ColorSpaceType colorSpace) = GetFormatAndColorSpace(actualOutputMode);

        SwapChainPanelHost hostWrapper = GetOrCreateHost(panel);
        hostWrapper.EnsureSize(actualPhysicalSize.Width, actualPhysicalSize.Height, format, colorSpace);

        using (ID3D11Texture2D backBuffer = hostWrapper.GetBackBuffer())
        {
            _textureRenderer.Render(backBuffer, chart, placements, options, options.DebugOverlayEnabled);
            hostWrapper.Present();
        }

        return new ChartRenderSession(
            RendererId,
            chart,
            placements,
            options.OutputMode,
            actualOutputMode,
            format.ToString(),
            colorSpace.ToString(),
            actualOutputMode != RenderOutputMode.SdrSrgb,
            options.RasterizationScale,
            new Size(panel.ActualWidth, panel.ActualHeight),
            intendedPhysicalSize,
            actualPhysicalSize,
            options.ToneMappingParameters,
            warnings,
            DateTimeOffset.UtcNow);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        foreach (SwapChainPanelHost host in _hosts.Values)
        {
            host.Dispose();
        }

        _hosts.Clear();
        _textureRenderer.Dispose();
        _deviceResources.Dispose();
        _disposed = true;
    }

    private SwapChainPanelHost GetOrCreateHost(Microsoft.UI.Xaml.Controls.SwapChainPanel panel)
    {
        if (!_hosts.TryGetValue(panel, out SwapChainPanelHost? host))
        {
            host = new SwapChainPanelHost(_deviceResources, panel);
            _hosts[panel] = host;
        }

        return host;
    }

    private static SizeInt CalculateIntendedPhysicalSize(IReadOnlyList<PatchPlacement> placements, ChartLayoutDefinition layout)
    {
        int maxRight = 0;
        int maxBottom = 0;

        foreach (PatchPlacement placement in placements)
        {
            maxRight = Math.Max(maxRight, placement.Bounds.X + placement.Bounds.Width);
            maxBottom = Math.Max(maxBottom, placement.Bounds.Y + placement.Bounds.Height);
        }

        return new SizeInt(maxRight + layout.Border, maxBottom + layout.Border);
    }

    private static SizeInt GetActualPhysicalSize(Microsoft.UI.Xaml.Controls.SwapChainPanel panel, double scale)
    {
        int width = (int)Math.Round(panel.ActualWidth * scale);
        int height = (int)Math.Round(panel.ActualHeight * scale);
        return new SizeInt(Math.Max(1, width), Math.Max(1, height));
    }

    private static RenderOutputMode ResolveOutputMode(RenderOutputMode requested, List<string> warnings)
    {
        if (requested == RenderOutputMode.SdrSrgb)
        {
            return requested;
        }

        warnings.Add($"hdr-not-implemented: requested {requested}, falling back to SdrSrgb");
        return RenderOutputMode.SdrSrgb;
    }

    private static (Format Format, ColorSpaceType ColorSpace) GetFormatAndColorSpace(RenderOutputMode mode)
    {
        return mode switch
        {
            RenderOutputMode.SdrSrgb => (Format.B8G8R8A8_UNorm, ColorSpaceType.RgbFullG22NoneP709),
            RenderOutputMode.HdrScRgb => (Format.R16G16B16A16_Float, ColorSpaceType.RgbFullG10NoneP709),
            RenderOutputMode.Hdr10 => (Format.R10G10B10A2_UNorm, ColorSpaceType.RgbFullG2084NoneP2020),
            _ => throw new NotSupportedException($"Output mode {mode} is not supported.")
        };
    }
}
