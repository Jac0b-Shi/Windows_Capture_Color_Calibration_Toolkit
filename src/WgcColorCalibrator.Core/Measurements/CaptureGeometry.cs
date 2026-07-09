using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Records Win32/DWM geometry for a native window at a point in time.
/// </summary>
public sealed record WindowGeometrySnapshot(
    ScreenRectInt WindowRect,
    ScreenRectInt? ExtendedFrameBounds,
    ScreenRectInt ClientRectInScreen);

/// <summary>
/// Describes which rectangle the WGC frame origin is believed to correspond to.
/// </summary>
public enum CaptureFrameOriginBasis
{
    WindowRect,
    ExtendedFrameBounds,
    ClientRect,
    Unverified
}

/// <summary>
/// Indicates whether the capture frame origin was verified.
/// </summary>
public enum CaptureMappingStatus
{
    Verified,
    Unverified
}

/// <summary>
/// Records the geometry mapping between the captured frame and the chart content.
/// </summary>
public sealed record CaptureGeometry(
    WindowGeometrySnapshot BeforeCaptureGeometry,
    WindowGeometrySnapshot AfterCaptureGeometry,
    CaptureFrameOriginBasis FrameOriginBasis,
    CaptureMappingStatus MappingStatus,
    PixelPoint ContentOffset,
    IReadOnlyList<string> Warnings);
