using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Records capture metadata relevant to measurement interpretation.
/// </summary>
public sealed record CaptureSummary
{
    public CaptureSummary(
        string BackendId,
        CaptureSourceKind SourceKind,
        CapturePixelFormat RequestedPixelFormat,
        CapturePixelFormat ActualPixelFormat,
        ColorEncoding Encoding,
        bool FormatDowngraded)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(BackendId);

        this.BackendId = BackendId;
        this.SourceKind = SourceKind;
        this.RequestedPixelFormat = RequestedPixelFormat;
        this.ActualPixelFormat = ActualPixelFormat;
        this.Encoding = Encoding;
        this.FormatDowngraded = FormatDowngraded;
    }

    public string BackendId { get; }

    public CaptureSourceKind SourceKind { get; }

    public CapturePixelFormat RequestedPixelFormat { get; }

    public CapturePixelFormat ActualPixelFormat { get; }

    public ColorEncoding Encoding { get; }

    public bool FormatDowngraded { get; }
}
