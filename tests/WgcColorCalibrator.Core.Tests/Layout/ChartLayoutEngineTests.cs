using System.Linq;
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

    [Theory]
    [InlineData(128, 8, 16, 4, 5, 568, 296)]
    [InlineData(64, 8, 16, 4, 5, 312, 168)]
    [InlineData(256, 8, 16, 4, 5, 1080, 552)]
    public void CreatePlacements_Standard128Patch_FitsWithinIntendedPhysicalSize(
        int patchSize,
        int gap,
        int border,
        int columnCount,
        int grayscaleSteps,
        int expectedWidth,
        int expectedHeight)
    {
        var layout = new ChartLayoutDefinition(
            patchSize,
            patchSize,
            gap,
            border,
            SafeSampleInset: 4,
            columnCount,
            new Rgb8(0, 0, 0));

        var chart = new GrayscaleChartProvider().Create(ChartGenerationOptions.Default with
        {
            GrayscaleSteps = grayscaleSteps,
            Layout = layout
        });

        IReadOnlyList<PatchPlacement> placements = ChartLayoutEngine.CreatePlacements(chart);

        int maxRight = placements.Max(p => p.Bounds.X + p.Bounds.Width) + border;
        int maxBottom = placements.Max(p => p.Bounds.Y + p.Bounds.Height) + border;

        Assert.Equal(expectedWidth, maxRight);
        Assert.Equal(expectedHeight, maxBottom);

        foreach (PatchPlacement placement in placements)
        {
            Assert.True(placement.Bounds.X >= 0);
            Assert.True(placement.Bounds.Y >= 0);
            Assert.True(placement.Bounds.X + placement.Bounds.Width <= maxRight - border);
            Assert.True(placement.Bounds.Y + placement.Bounds.Height <= maxBottom - border);
        }
    }

    [Theory]
    [InlineData(1.00, 568.0, 296.0)]
    [InlineData(1.25, 454.4, 236.8)]
    [InlineData(1.50, 378.66666666666669, 197.33333333333334)]
    [InlineData(2.00, 284.0, 148.0)]
    public void PhysicalToLogicalSize_AtVariousDpiScales_IsCorrect(
        double scale,
        double expectedLogicalWidth,
        double expectedLogicalHeight)
    {
        const double PhysicalWidth = 568.0;
        const double PhysicalHeight = 296.0;

        double logicalWidth = PhysicalWidth / scale;
        double logicalHeight = PhysicalHeight / scale;

        Assert.Equal(expectedLogicalWidth, logicalWidth, precision: 6);
        Assert.Equal(expectedLogicalHeight, logicalHeight, precision: 6);
    }
}

