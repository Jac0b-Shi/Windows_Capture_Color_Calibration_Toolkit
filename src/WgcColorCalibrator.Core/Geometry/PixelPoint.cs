namespace WgcColorCalibrator.Core.Geometry;

/// <summary>
/// Represents an integer physical-pixel offset.
/// </summary>
public readonly record struct PixelPoint(int X, int Y)
{
    public static PixelPoint Zero => new(0, 0);
}
