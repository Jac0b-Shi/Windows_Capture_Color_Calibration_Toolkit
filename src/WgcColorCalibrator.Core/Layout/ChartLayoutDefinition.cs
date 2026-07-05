using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Layout;

/// <summary>
/// Defines logical patch layout parameters.
/// </summary>
public sealed record ChartLayoutDefinition
{
    public static ChartLayoutDefinition Default { get; } = new(
        PatchWidth: 64,
        PatchHeight: 64,
        Gap: 8,
        Border: 16,
        SafeSampleInset: 8,
        ColumnCount: 4,
        WindowBackground: new Rgb8(0, 0, 0));

    public ChartLayoutDefinition(
        int PatchWidth,
        int PatchHeight,
        int Gap,
        int Border,
        int SafeSampleInset,
        int ColumnCount,
        Rgb8 WindowBackground)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PatchWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(PatchHeight);
        ArgumentOutOfRangeException.ThrowIfNegative(Gap);
        ArgumentOutOfRangeException.ThrowIfNegative(Border);
        ArgumentOutOfRangeException.ThrowIfNegative(SafeSampleInset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(ColumnCount);

        if ((SafeSampleInset * 2) >= PatchWidth || (SafeSampleInset * 2) >= PatchHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(SafeSampleInset), "Safe sample inset must leave a positive sample area.");
        }

        this.PatchWidth = PatchWidth;
        this.PatchHeight = PatchHeight;
        this.Gap = Gap;
        this.Border = Border;
        this.SafeSampleInset = SafeSampleInset;
        this.ColumnCount = ColumnCount;
        this.WindowBackground = WindowBackground;
    }

    public int PatchWidth { get; }

    public int PatchHeight { get; }

    public int Gap { get; }

    public int Border { get; }

    public int SafeSampleInset { get; }

    public int ColumnCount { get; }

    public Rgb8 WindowBackground { get; }
}
