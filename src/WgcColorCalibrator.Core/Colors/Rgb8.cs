using System.Globalization;

namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Represents an 8-bit RGB color in R, G, B channel order.
/// </summary>
public readonly record struct Rgb8(byte R, byte G, byte B)
{
    /// <summary>
    /// Formats the color as an uppercase hexadecimal RGB string.
    /// </summary>
    public string ToHexString() => string.Create(
        CultureInfo.InvariantCulture,
        $"#{R:X2}{G:X2}{B:X2}");
}

