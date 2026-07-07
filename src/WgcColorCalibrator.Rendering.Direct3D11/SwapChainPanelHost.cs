using System.Runtime.InteropServices;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Creates and owns the DXGI swap chain bound to a WinUI 3 SwapChainPanel.
/// </summary>
public sealed class SwapChainPanelHost : IDisposable
{
    private readonly D3D11DeviceResources _resources;
    private readonly object _panel;
    private IDXGISwapChain1? _swapChain;
    private bool _disposed;

    public SwapChainPanelHost(D3D11DeviceResources resources, object panel)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
        _panel = panel ?? throw new ArgumentNullException(nameof(panel));
    }

    public IDXGISwapChain1? SwapChain => _swapChain;

    public Format Format { get; private set; }

    public int Width { get; private set; }

    public int Height { get; private set; }

    public void EnsureSize(int width, int height, Format format, ColorSpaceType colorSpace)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentException("Swap chain dimensions must be positive.", nameof(width));
        }

        if (_swapChain is not null && Width == width && Height == height && Format == format)
        {
            return;
        }

        _swapChain?.Dispose();
        _swapChain = null;

        Format = format;
        Width = width;
        Height = height;

        var description = new SwapChainDescription1
        {
            Width = (uint)width,
            Height = (uint)height,
            Format = format,
            Stereo = false,
            SampleDescription = new SampleDescription(1, 0),
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = 2,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipSequential,
            AlphaMode = AlphaMode.Ignore,
            Flags = SwapChainFlags.None
        };

        _swapChain = _resources.Factory.CreateSwapChainForComposition(_resources.Device, description, null);
        SetColorSpace(_swapChain, colorSpace);
        SetSwapChainOnPanel(_swapChain);
    }

    public ID3D11Texture2D GetBackBuffer()
    {
        if (_swapChain is null)
        {
            throw new InvalidOperationException("Swap chain has not been created.");
        }

        return _swapChain.GetBuffer<ID3D11Texture2D>(0);
    }

    public void Present()
    {
        _swapChain?.Present(1, PresentFlags.None);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _swapChain?.Dispose();
        _swapChain = null;
        _disposed = true;
    }

    private static void SetColorSpace(IDXGISwapChain1 swapChain, ColorSpaceType colorSpace)
    {
        IDXGISwapChain3? swapChain3 = swapChain.QueryInterface<IDXGISwapChain3>();
        if (swapChain3 is not null)
        {
            using (swapChain3)
            {
                swapChain3.SetColorSpace1(colorSpace);
            }
        }
    }

    private void SetSwapChainOnPanel(IDXGISwapChain1 swapChain)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        var native = SwapChainPanelNative.FromObject(_panel);
        native.SetSwapChain(swapChain);
    }
}
