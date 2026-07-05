using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Represents a measured or declared color value and its encoding metadata.
/// </summary>
public sealed record ColorValue(
    ColorEncoding Encoding,
    Rgb8? Rgb8,
    RgbaFloat? Rgba);
