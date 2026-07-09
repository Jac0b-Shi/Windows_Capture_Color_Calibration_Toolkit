namespace WgcColorCalibrator.Core.Capture;

/// <summary>
/// A platform-agnostic single-frame capture backend.
/// </summary>
public interface ISingleFrameCaptureBackend
{
    string BackendId { get; }

    Task<CapturedFrame> CaptureAsync(WindowCaptureRequest request, CancellationToken cancellationToken = default);
}
