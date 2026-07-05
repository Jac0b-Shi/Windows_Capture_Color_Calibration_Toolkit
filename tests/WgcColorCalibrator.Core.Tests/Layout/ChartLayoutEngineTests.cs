using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Tests.Layout;

public sealed class ChartLayoutEngineTests
{
    [Fact]
    public void CreatePlacements_ComputesBoundsAndSafeSampleBounds()
    {
        var layout = new ChartLayoutDefinition(
            PatchWidth: 16,
            PatchHeight: 16,
            Gap: 4,
            Border: 2,
            SafeSampleInset: 4,
            ColumnCount: 2,
            WindowBackground: new Rgb8(0, 0, 0));

        var chart = new GrayscaleChartProvider().Create(ChartGenerationOptions.Default with
        {
            GrayscaleSteps = 3,
            Layout = layout
        });

        IReadOnlyList<PatchPlacement> placements = ChartLayoutEngine.CreatePlacements(chart);

        Assert.Equal(new PixelRect(2, 2, 16, 16), placements[0].Bounds);
        Assert.Equal(new PixelRect(6, 6, 8, 8), placements[0].SafeSampleBounds);
        Assert.Equal(new PixelRect(22, 2, 16, 16), placements[1].Bounds);
        Assert.Equal(new PixelRect(2, 22, 16, 16), placements[2].Bounds);
    }

    [Fact]
    public void ChartLayoutDefinition_RejectsInsetThatConsumesPatch()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartLayoutDefinition(
            PatchWidth: 8,
            PatchHeight: 8,
            Gap: 0,
            Border: 0,
            SafeSampleInset: 4,
            ColumnCount: 1,
            WindowBackground: new Rgb8(0, 0, 0)));
    }
}

