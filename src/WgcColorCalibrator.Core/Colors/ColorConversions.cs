namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Provides deterministic conversions for explicitly encoded RGB values.
/// </summary>
public static class ColorConversions
{
    /// <summary>
    /// Converts gamma-encoded 8-bit RGB channels to HSV using channel values normalized to [0, 1].
    /// </summary>
    public static Hsv ToHsv(Rgb8 color)
    {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;

        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;

        double hue = 0.0;
        if (delta > 0.0)
        {
            if (max.Equals(r))
            {
                hue = 60.0 * (((g - b) / delta) % 6.0);
            }
            else if (max.Equals(g))
            {
                hue = 60.0 * (((b - r) / delta) + 2.0);
            }
            else
            {
                hue = 60.0 * (((r - g) / delta) + 4.0);
            }

            if (hue < 0.0)
            {
                hue += 360.0;
            }
        }

        double saturation = max == 0.0 ? 0.0 : delta / max;
        return new Hsv(hue, saturation, max);
    }
}

