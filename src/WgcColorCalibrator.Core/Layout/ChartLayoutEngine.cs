using WgcColorCalibrator.Core.Charts;

namespace WgcColorCalibrator.Core.Layout;

/// <summary>
/// Computes deterministic patch placements from chart layout settings.
/// </summary>
public static class ChartLayoutEngine
{
    public static IReadOnlyList<PatchPlacement> CreatePlacements(ChartDefinition chart)
    {
        ArgumentNullException.ThrowIfNull(chart);

        var placements = new List<PatchPlacement>(chart.Patches.Count);
        ChartLayoutDefinition layout = chart.Layout;

        for (int index = 0; index < chart.Patches.Count; index++)
        {
            int row = index / layout.ColumnCount;
            int column = index % layout.ColumnCount;
            int x = layout.Border + (column * (layout.PatchWidth + layout.Gap));
            int y = layout.Border + (row * (layout.PatchHeight + layout.Gap));
            var bounds = new PixelRect(x, y, layout.PatchWidth, layout.PatchHeight);
            placements.Add(new PatchPlacement(
                chart.Patches[index].Id,
                bounds,
                bounds.Inset(layout.SafeSampleInset)));
        }

        return placements;
    }
}

