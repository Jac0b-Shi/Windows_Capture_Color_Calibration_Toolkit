using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Capture;

/// <summary>
/// Records a captured frame in packed BGRA format.
/// </summary>
public sealed record CapturedFrame(
    SizeInt CaptureItemSize,
    SizeInt SurfaceSize,
    SizeInt ContentSize,
    CapturePixelFormat PixelFormat,
    int PackedRowStride,
    byte[] ContentPixels,
    CaptureSourceKind SourceKind,
    TimeSpan? SystemRelativeTime,
    DateTimeOffset CapturedAt,
    IReadOnlyList<string> Warnings)
{
    public int ContentPixelLength => ContentSize.Width * ContentSize.Height * 4;
}
