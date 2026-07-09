using WgcColorCalibrator.Core.Measurements;

namespace WgcColorCalibrator.Core.Capture;

/// <summary>
/// A platform-agnostic capture request containing only the capture target and format parameters.
/// </summary>
public sealed record WindowCaptureRequest(
    nint WindowHandle,
    CapturePixelFormat PixelFormat,
    TimeSpan Timeout);
