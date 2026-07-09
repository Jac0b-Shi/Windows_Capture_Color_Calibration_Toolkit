namespace WgcColorCalibrator.Core.Geometry;

/// <summary>
/// Represents a rectangle in screen coordinates, which may be negative on multi-monitor layouts.
/// </summary>
public readonly record struct ScreenRectInt(int X, int Y, int Width, int Height)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public int Left => X;

    [System.Text.Json.Serialization.JsonIgnore]
    public int Top => Y;

    [System.Text.Json.Serialization.JsonIgnore]
    public int Right => X + Width;

    [System.Text.Json.Serialization.JsonIgnore]
    public int Bottom => Y + Height;

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEmpty => Width == 0 || Height == 0;
}
