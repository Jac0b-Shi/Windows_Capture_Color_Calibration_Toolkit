namespace WgcColorCalibrator.Core.Layout;

/// <summary>
/// Converts between physical pixels and device-independent pixels (DIP) using a rasterization scale.
/// </summary>
public static class PhysicalPixelConverter
{
    /// <summary>
    /// Converts physical pixels to DIP.
    /// </summary>
    public static double ToDip(int physicalPixels, double scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scale);
        return physicalPixels / scale;
    }

    /// <summary>
    /// Converts DIP to physical pixels, rounding away from zero.
    /// </summary>
    public static int ToPhysicalPixels(double dip, double scale)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(scale);
        return (int)Math.Round(dip * scale, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Rounds a DIP value to a physical pixel boundary and returns the resulting DIP value.
    /// </summary>
    public static double RoundToPhysicalPixelBoundary(double dip, double scale)
    {
        int physical = ToPhysicalPixels(dip, scale);
        return ToDip(physical, scale);
    }
}
