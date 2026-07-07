namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Describes how a color patch value should be interpreted.
/// </summary>
public enum ColorEncoding
{
    Unknown,
    SrgbEncoded,
    LinearScRgb,
    DisplayObserved,
    CaptureNative,
    Hdr10St2084
}
