namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Describes how color channel values should be interpreted.
/// </summary>
public enum ColorEncoding
{
    /// <summary>
    /// The encoding is unknown or has not been verified.
    /// </summary>
    Unknown,

    /// <summary>
    /// Gamma-encoded sRGB values.
    /// </summary>
    SrgbEncoded,

    /// <summary>
    /// Linear scRGB values.
    /// </summary>
    LinearScRgb,

    /// <summary>
    /// Values manually observed from a display or external tool.
    /// </summary>
    DisplayObserved,

    /// <summary>
    /// Values in the capture backend native representation.
    /// </summary>
    CaptureNative
}

