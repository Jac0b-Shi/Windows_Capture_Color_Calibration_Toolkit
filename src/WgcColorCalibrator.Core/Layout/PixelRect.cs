namespace WgcColorCalibrator.Core.Layout;

/// <summary>
/// Represents an integer pixel rectangle.
/// </summary>
public readonly record struct PixelRect
{
    public PixelRect(int x, int y, int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(x);
        ArgumentOutOfRangeException.ThrowIfNegative(y);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public int X { get; }

    public int Y { get; }

    public int Width { get; }

    public int Height { get; }

    [System.Text.Json.Serialization.JsonIgnore]
    public int Right => checked(X + Width);

    [System.Text.Json.Serialization.JsonIgnore]
    public int Bottom => checked(Y + Height);

    public PixelRect Inset(int inset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(inset);

        int width = Width - (inset * 2);
        int height = Height - (inset * 2);
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(inset), "Inset must leave a positive rectangle.");
        }

        return new PixelRect(X + inset, Y + inset, width, height);
    }
}
