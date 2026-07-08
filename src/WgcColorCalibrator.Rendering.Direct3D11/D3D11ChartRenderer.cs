using System.Linq;
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
    private readonly IEnumerable<IToneMapper> _toneMappers;
    private readonly Dictionary<object, SwapChainPanelHost> _hosts = new();
    private bool _disposed;

    public D3D11ChartRenderer(D3D11DeviceResources deviceResources, IEnumerable<IToneMapper> toneMappers)
    {
        _deviceResources = deviceResources ?? throw new ArgumentNullException(nameof(deviceResources));
        _toneMappers = toneMappers ?? throw new ArgumentNullException(nameof(toneMappers));
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

        var warnings = new List<string>(options.Warnings ?? Array.Empty<string>());
        SizeInt intendedPhysicalSize = CalculateIntendedPhysicalSize(placements, chart.Layout);
        (SizeInt actualPhysicalSize, double scaleX, double scaleY) = GetActualPhysicalSize(panel);

        if (actualPhysicalSize.Width != intendedPhysicalSize.Width ||
            actualPhysicalSize.Height != intendedPhysicalSize.Height)
        {
            warnings.Add($"size-mismatch: intended={intendedPhysicalSize.Width}x{intendedPhysicalSize.Height}, actual={actualPhysicalSize.Width}x{actualPhysicalSize.Height}");
        }

        if (options.DebugOverlayEnabled)
        {
            warnings.Add("debug-overlay-enabled");
        }

        RenderOutputMode requestedOutputMode = options.RequestedOutputMode;
        RenderOutputMode actualOutputMode = options.ActualOutputMode;
        (Format format, ColorSpaceType colorSpace) = GetFormatAndColorSpace(actualOutputMode);

        SwapChainPanelHost hostWrapper = GetOrCreateHost(panel);
        hostWrapper.EnsureSize(intendedPhysicalSize.Width, intendedPhysicalSize.Height, format);

        bool colorSpaceSet = hostWrapper.TrySetColorSpace(colorSpace, out ColorSpaceApplicationResult colorSpaceResult);
        if (!colorSpaceSet)
        {
            warnings.Add("color-space-set-failed");
        }

        bool hdrOutputActive = actualOutputMode != RenderOutputMode.SdrSrgb &&
                               colorSpaceResult.SetSucceeded &&
                               ((SwapChainColorSpaceSupportFlags)colorSpaceResult.SupportFlags).HasFlag(SwapChainColorSpaceSupportFlags.Present);

        DisplayOutputMetadata displayMetadata = options.DisplayOutput ?? DisplayOutputMetadata.Unknown;
        if (displayMetadata == DisplayOutputMetadata.Unknown)
        {
            displayMetadata = ProbeDisplayMetadata(hostWrapper.SwapChain);
        }

        ToneMappingMode toneMappingMode = chart.RenderingParameters?.ToneMappingMode ?? ToneMappingMode.DirectScRgb;
        IToneMapper toneMapper = ResolveToneMapper(toneMappingMode);

        using (ID3D11Texture2D backBuffer = hostWrapper.GetBackBuffer())
        {
            Texture2DDescription desc = backBuffer.Description;
            if (desc.Width != intendedPhysicalSize.Width || desc.Height != intendedPhysicalSize.Height)
            {
                throw new Direct3D11RenderingException(
                    $"Back buffer size mismatch: expected={intendedPhysicalSize.Width}x{intendedPhysicalSize.Height}, " +
                    $"actual={desc.Width}x{desc.Height}.");
            }

            _textureRenderer.Render(backBuffer, chart, placements, toneMapper, options, options.DebugOverlayEnabled);
            hostWrapper.Present();
        }

        string matrixTransform = $"ScaleX={1.0 / scaleX:F6}, ScaleY={1.0 / scaleY:F6}";

        return new ChartRenderSession(
            RendererId,
            chart,
            placements,
            requestedOutputMode,
            actualOutputMode,
            format.ToString(),
            colorSpace.ToString(),
            hdrOutputActive,
            scaleX,
            new Size(panel.ActualWidth, panel.ActualHeight),
            intendedPhysicalSize,
            actualPhysicalSize,
            options.ToneMappingParameters,
            displayMetadata,
            warnings,
            DateTimeOffset.UtcNow,
            scaleX,
            scaleY,
            matrixTransform,
            colorSpaceResult.SupportFlags,
            colorSpaceResult.SetHResult.Code);
    }

    public void DetachHost(object host)
    {
        if (host is null || !_hosts.TryGetValue(host, out SwapChainPanelHost? hostWrapper))
        {
            return;
        }

        hostWrapper.DetachFromPanel();
        _hosts.Remove(host);
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

    private static (SizeInt PhysicalSize, double ScaleX, double ScaleY) GetActualPhysicalSize(Microsoft.UI.Xaml.Controls.SwapChainPanel panel)
    {
        double fallbackScale = panel.XamlRoot?.RasterizationScale ?? 1.0;
        double scaleX = panel.CompositionScaleX > 0 ? panel.CompositionScaleX : fallbackScale;
        double scaleY = panel.CompositionScaleY > 0 ? panel.CompositionScaleY : fallbackScale;

        int width = (int)Math.Round(panel.ActualWidth * scaleX);
        int height = (int)Math.Round(panel.ActualHeight * scaleY);
        return (new SizeInt(Math.Max(1, width), Math.Max(1, height)), scaleX, scaleY);
    }

    private IToneMapper ResolveToneMapper(ToneMappingMode mode)
    {
        string id = mode switch
        {
            ToneMappingMode.DirectScRgb => "direct-scrgb",
            ToneMappingMode.ReferenceWhiteScaled => "reference-white-scaled",
            _ => throw new NotSupportedException($"Tone mapping mode {mode} is not supported.")
        };

        IToneMapper? mapper = _toneMappers.FirstOrDefault(m => m.Id == id);
        return mapper ?? _toneMappers.First(m => m.Id == "direct-scrgb");
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

    private static DisplayOutputMetadata ProbeDisplayMetadata(IDXGISwapChain1? swapChain)
    {
        if (swapChain is null)
        {
            return DisplayOutputMetadata.Unknown;
        }

        IDXGIOutput? output = null;
        try
        {
            output = swapChain.GetContainingOutput();
        }
        catch
        {
            return DisplayOutputMetadata.Unknown;
        }

        if (output is null)
        {
            return DisplayOutputMetadata.Unknown;
        }

        using (output)
        {
            IDXGIOutput6? output6 = output.QueryInterface<IDXGIOutput6>();
            if (output6 is null)
            {
                return DisplayOutputMetadata.Unknown;
            }

            using (output6)
            {
                OutputDescription1 desc = output6.Description1;
                bool hdrActive =
                    desc.ColorSpace == ColorSpaceType.RgbFullG10NoneP709 ||
                    desc.ColorSpace == ColorSpaceType.RgbFullG2084NoneP2020;
                bool hdrSupported = desc.MaxLuminance > 80.0f;

                return new DisplayOutputMetadata(
                    desc.DeviceName,
                    hdrSupported,
                    hdrActive,
                    desc.MaxLuminance,
                    desc.MaxFullFrameLuminance,
                    desc.MinLuminance);
            }
        }
    }
}
