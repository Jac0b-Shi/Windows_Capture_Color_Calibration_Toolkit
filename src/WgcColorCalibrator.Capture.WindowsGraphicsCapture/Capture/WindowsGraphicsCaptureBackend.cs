using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Capture.WindowsGraphicsCapture.Native;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture;

/// <summary>
/// Windows Graphics Capture implementation of the single-frame capture backend.
/// </summary>
public sealed class WindowsGraphicsCaptureBackend : ISingleFrameCaptureBackend, IDisposable
{
    private readonly D3D11CaptureDevice _device;
    private readonly SemaphoreSlim _concurrencyLock = new(1, 1);
    private bool _disposed;

    public WindowsGraphicsCaptureBackend()
    {
        if (!GraphicsCaptureSession.IsSupported())
        {
            throw new NotSupportedException("Windows Graphics Capture is not supported on this device.");
        }

        _device = new D3D11CaptureDevice();
    }

    public string BackendId => "windows-graphics-capture";

    public async Task<CapturedFrame> CaptureAsync(WindowCaptureRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentOutOfRangeException.ThrowIfZero(request.WindowHandle);

        if (request.PixelFormat != CapturePixelFormat.B8G8R8A8UIntNormalized &&
            request.PixelFormat != CapturePixelFormat.R16G16B16A16Float)
        {
            throw new NotSupportedException($"Pixel format '{request.PixelFormat}' is not supported in this milestone.");
        }

        await _concurrencyLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            _ = WindowGeometryNative.DwmFlush();

            GraphicsCaptureItem item = GraphicsCaptureItemFactory.CreateForWindow(request.WindowHandle);
            using WgcCaptureSession session = new(
                _device,
                item,
                request.Timeout,
                request.PixelFormat,
                cancellationToken);

            session.StartCapture();
            return await session.GetCapturedFrameAsync().ConfigureAwait(false);
        }
        finally
        {
            _concurrencyLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _concurrencyLock.Dispose();
        _device.Dispose();
        _disposed = true;
    }
}
