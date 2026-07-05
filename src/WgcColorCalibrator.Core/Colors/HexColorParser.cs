using System.Globalization;

namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Parses and formats RGB hexadecimal color values.
/// </summary>
public static class HexColorParser
{
    /// <summary>
    /// Parses #RGB, RGB, #RRGGBB, or RRGGBB into an <see cref="Rgb8"/>.
    /// </summary>
    public static Rgb8 ParseRgb8(string value)
    {
        if (TryParseRgb8(value, out Rgb8 color))
        {
            return color;
        }

        throw new FormatException("The value is not a valid RGB hex color.");
    }

    /// <summary>
    /// Attempts to parse #RGB, RGB, #RRGGBB, or RRGGBB into an <see cref="Rgb8"/>.
    /// </summary>
    public static bool TryParseRgb8(string? value, out Rgb8 color)
    {
        color = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        string normalized = value.Trim();
        if (normalized.StartsWith('#'))
        {
            normalized = normalized[1..];
        }

        if (normalized.Length == 3)
        {
            normalized = string.Concat(
                normalized[0], normalized[0],
                normalized[1], normalized[1],
                normalized[2], normalized[2]);
        }

        if (normalized.Length != 6)
        {
            return false;
        }

        if (!byte.TryParse(normalized[0..2], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte r) ||
            !byte.TryParse(normalized[2..4], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte g) ||
            !byte.TryParse(normalized[4..6], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
        {
            return false;
        }

        color = new Rgb8(r, g, b);
        return true;
    }
}

