using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Creates and owns the DXGI swap chain bound to a WinUI 3 SwapChainPanel.
/// </summary>
public sealed class SwapChainPanelHost : IDisposable
{
    // IDXGISwapChain3 vtable indices for SetColorSpace1 and GetColorSpace1.
    // Vortice 3.8.3 exposes CheckColorSpaceSupport and SetColorSpace1, but not GetColorSpace1,
    // so the readback is performed through the COM vtable directly.
    private const int SetColorSpace1VtableIndex = 33;
    private const int GetColorSpace1VtableIndex = 34;

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

    public void DetachFromPanel()
    {
        if (_disposed)
        {
            return;
        }

        SetSwapChainOnPanel(null);
        Dispose();
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

    public bool TrySetColorSpace(ColorSpaceType requested, out ColorSpaceVerification verification)
    {
        verification = default;

        IDXGISwapChain3? swapChain3 = _swapChain?.QueryInterface<IDXGISwapChain3>();
        if (swapChain3 is null)
        {
            return false;
        }

        using (swapChain3)
        {
            SwapChainColorSpaceSupportFlags supportFlags = swapChain3.CheckColorSpaceSupport(requested);
            if (!supportFlags.HasFlag(SwapChainColorSpaceSupportFlags.Present))
            {
                verification = new ColorSpaceVerification(requested, (uint)supportFlags, new Result(unchecked((int)0x80004005)), null);
                return false;
            }

            Result setResult = SetColorSpace1WithHResult(swapChain3, requested);
            ColorSpaceType? actual = null;
            if (setResult.Success)
            {
                int getHr = GetColorSpace1WithHResult(swapChain3, out ColorSpaceType readBack);
                if (getHr >= 0)
                {
                    actual = readBack;
                }
            }

            verification = new ColorSpaceVerification(requested, (uint)supportFlags, setResult, actual);
            return setResult.Success && actual == requested;
        }
    }

    private static Result SetColorSpace1WithHResult(IDXGISwapChain3 swapChain3, ColorSpaceType colorSpace)
    {
        try
        {
            int hr = InvokeColorSpaceMethod(swapChain3, SetColorSpace1VtableIndex, colorSpace);
            return new Result(hr);
        }
        catch (Exception ex)
        {
            return new Result(ex.HResult);
        }
    }

    private static int GetColorSpace1WithHResult(IDXGISwapChain3 swapChain3, out ColorSpaceType colorSpace)
    {
        colorSpace = default;
        try
        {
            int hr = InvokeColorSpaceMethod(swapChain3, GetColorSpace1VtableIndex, out int rawColorSpace);
            colorSpace = (ColorSpaceType)rawColorSpace;
            return hr;
        }
        catch
        {
            return unchecked((int)0x80004005);
        }
    }

    private static int InvokeColorSpaceMethod(IDXGISwapChain3 swapChain3, int vtableIndex, ColorSpaceType colorSpace)
    {
        var cppObject = Unsafe.As<CppObject>(swapChain3);
        IntPtr nativePointer = cppObject.NativePointer;
        if (nativePointer == IntPtr.Zero)
        {
            return unchecked((int)0x80004003);
        }

        IntPtr vtable = Marshal.ReadIntPtr(nativePointer);
        IntPtr methodPointer = Marshal.ReadIntPtr(vtable, vtableIndex * IntPtr.Size);
        var method = Marshal.GetDelegateForFunctionPointer<SetColorSpace1Delegate>(methodPointer);
        return method(nativePointer, colorSpace);
    }

    private static int InvokeColorSpaceMethod(IDXGISwapChain3 swapChain3, int vtableIndex, out int colorSpace)
    {
        var cppObject = Unsafe.As<CppObject>(swapChain3);
        IntPtr nativePointer = cppObject.NativePointer;
        if (nativePointer == IntPtr.Zero)
        {
            colorSpace = 0;
            return unchecked((int)0x80004003);
        }

        IntPtr vtable = Marshal.ReadIntPtr(nativePointer);
        IntPtr methodPointer = Marshal.ReadIntPtr(vtable, vtableIndex * IntPtr.Size);
        var method = Marshal.GetDelegateForFunctionPointer<GetColorSpace1Delegate>(methodPointer);
        return method(nativePointer, out colorSpace);
    }

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int SetColorSpace1Delegate(IntPtr thisPtr, ColorSpaceType colorSpace);

    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate int GetColorSpace1Delegate(IntPtr thisPtr, out int colorSpace);

    private void SetSwapChainOnPanel(IDXGISwapChain1? swapChain)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return;
        }

        ISwapChainPanelNative native = SwapChainPanelNative.FromObject(_panel);

        nint swapChainPointer = swapChain?.NativePointer ?? nint.Zero;

        int hr = native.SetSwapChain(swapChainPointer);
        Marshal.ThrowExceptionForHR(hr);
    }
}
