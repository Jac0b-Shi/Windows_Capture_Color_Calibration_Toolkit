using System.Numerics;

namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Represents a standard-dynamic-range color encoded as sRGB.
/// </summary>
public readonly record struct SdrColor(Rgb8 Rgb)
{
    public byte R => Rgb.R;

    public byte G => Rgb.G;

    public byte B => Rgb.B;

    public string ToHexString() => Rgb.ToHexString();
}
