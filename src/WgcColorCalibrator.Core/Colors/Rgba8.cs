namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Represents an 8-bit RGBA color in R, G, B, A channel order.
/// </summary>
public readonly record struct Rgba8(byte R, byte G, byte B, byte A)
{
    /// <summary>
    /// Gets the RGB channels without alpha.
    /// </summary>
    public Rgb8 ToRgb8() => new(R, G, B);
}

