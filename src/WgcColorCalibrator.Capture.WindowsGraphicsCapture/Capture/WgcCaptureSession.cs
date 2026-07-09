using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture;

/// <summary>
/// Encapsulates a single-frame WGC capture session.
/// </summary>
internal sealed class WgcCaptureSession : IDisposable
{
    private readonly D3D11CaptureDevice _device;
    private readonly GraphicsCaptureItem _captureItem;
    private readonly Direct3D11CaptureFramePool _framePool;
    private readonly GraphicsCaptureSession _captureSession;
    private readonly CpuTextureReader _textureReader;
    private readonly TaskCompletionSource<Direct3D11CaptureFrame> _frameTcs;
    private readonly CancellationTokenRegistration _cancellationRegistration;
    private readonly CancellationTokenSource _timeoutCts;
    private readonly int _sizeTolerance;
    private readonly CapturePixelFormat _pixelFormat;

    private int _completed;
    private bool _disposed;

    public WgcCaptureSession(
        D3D11CaptureDevice device,
        GraphicsCaptureItem captureItem,
        TimeSpan timeout,
        CapturePixelFormat pixelFormat,
        CancellationToken cancellationToken,
        int sizeTolerance = 1)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(captureItem);

        _device = device;
        _captureItem = captureItem;
        _sizeTolerance = sizeTolerance;
        _pixelFormat = pixelFormat;
        _textureReader = new CpuTextureReader(device.Context);
        _frameTcs = new TaskCompletionSource<Direct3D11CaptureFrame>(TaskCreationOptions.RunContinuationsAsynchronously);
        _timeoutCts = new CancellationTokenSource(timeout);
        _cancellationRegistration = cancellationToken.Register(() => TryComplete(null, new OperationCanceledException(cancellationToken)));

        _timeoutCts.Token.Register(() => TryComplete(null, new TimeoutException("Capture timed out.")));

        _captureItem.Closed += OnCaptureItemClosed;

        _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            device.WinrtDevice,
            pixelFormat.ToDirectXPixelFormat(),
            1,
            captureItem.Size);
        _framePool.FrameArrived += OnFrameArrived;

        _captureSession = _framePool.CreateCaptureSession(_captureItem);
        if (ApiInformation.IsPropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", nameof(GraphicsCaptureSession.IsCursorCaptureEnabled)))
        {
            _captureSession.IsCursorCaptureEnabled = false;
        }

        if (ApiInformation.IsWriteablePropertyPresent("Windows.Graphics.Capture.GraphicsCaptureSession", nameof(GraphicsCaptureSession.IsBorderRequired)))
        {
            _captureSession.IsBorderRequired = false;
        }
    }

    public void StartCapture()
    {
        _captureSession.StartCapture();
    }

    public async Task<CapturedFrame> GetCapturedFrameAsync()
    {
        Direct3D11CaptureFrame frame = await _frameTcs.Task.ConfigureAwait(false);
        using (frame)
        {
            SizeInt contentSize = new(frame.ContentSize.Width, frame.ContentSize.Height);
            SizeInt surfaceSize = new(frame.Surface.Description.Width, frame.Surface.Description.Height);
            SizeInt captureItemSize = new(_captureItem.Size.Width, _captureItem.Size.Height);

            byte[] pixels = _textureReader.ReadContentRegion(frame.Surface, contentSize);
            List<string> warnings = new();

            if (surfaceSize.Width != contentSize.Width || surfaceSize.Height != contentSize.Height)
            {
                warnings.Add($"surface-size-differs-from-content-size: surface={surfaceSize.Width}x{surfaceSize.Height}, content={contentSize.Width}x{contentSize.Height}");
            }

            if (Math.Abs(captureItemSize.Width - surfaceSize.Width) > _sizeTolerance ||
                Math.Abs(captureItemSize.Height - surfaceSize.Height) > _sizeTolerance)
            {
                warnings.Add($"capture-item-size-differs-from-surface: item={captureItemSize.Width}x{captureItemSize.Height}, surface={surfaceSize.Width}x{surfaceSize.Height}");
            }

            int bytesPerPixel = _pixelFormat.GetBytesPerPixel();
            int packedRowStride = contentSize.Width * bytesPerPixel;

            return new CapturedFrame(
                captureItemSize,
                surfaceSize,
                contentSize,
                _pixelFormat,
                packedRowStride,
                pixels,
                CaptureSourceKind.Window,
                frame.SystemRelativeTime,
                DateTimeOffset.UtcNow,
                warnings);
        }
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        Direct3D11CaptureFrame? frame = null;
        try
        {
            frame = sender.TryGetNextFrame();
            if (frame is not null)
            {
                TryComplete(frame, null);
            }
        }
        catch (Exception ex)
        {
            frame?.Dispose();
            TryComplete(null, ex);
        }
    }

    private void OnCaptureItemClosed(GraphicsCaptureItem sender, object args)
    {
        TryComplete(null, new InvalidOperationException("Capture item was closed."));
    }

    private void TryComplete(Direct3D11CaptureFrame? frame, Exception? exception)
    {
        if (Interlocked.CompareExchange(ref _completed, 1, 0) != 0)
        {
            frame?.Dispose();
            return;
        }

        if (exception is not null)
        {
            _frameTcs.TrySetException(exception);
        }
        else if (frame is not null)
        {
            _frameTcs.TrySetResult(frame);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cancellationRegistration.Dispose();
        _timeoutCts.Dispose();
        _captureItem.Closed -= OnCaptureItemClosed;
        _framePool.FrameArrived -= OnFrameArrived;
        _captureSession.Dispose();
        _framePool.Dispose();
        _textureReader.Dispose();
        _disposed = true;
    }
}
