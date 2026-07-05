using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Tests.Charts;

public sealed class ChartProviderTests
{
    [Fact]
    public void ManualSingleColorProvider_CreatesStablePatch()
    {
        var provider = new ManualSingleColorChartProvider();
        ChartDefinition chart = provider.Create(ChartGenerationOptions.Default with
        {
            ManualColor = new Rgb8(255, 255, 255)
        });

        Assert.Equal("manual-single-color", chart.Id);
        Assert.Single(chart.Patches);
        Assert.Equal("manual-ffffff", chart.Patches[0].Id);
        Assert.Equal("#FFFFFF", chart.Patches[0].Label);
    }

    [Fact]
    public void GrayscaleProvider_IncludesEndpoints()
    {
        var provider = new GrayscaleChartProvider();
        ChartDefinition chart = provider.Create(ChartGenerationOptions.Default with
        {
            GrayscaleSteps = 5
        });

        Assert.Equal(new Rgb8(0, 0, 0), chart.Patches[0].ExpectedColor);
        Assert.Equal(new Rgb8(255, 255, 255), chart.Patches[^1].ExpectedColor);
        Assert.Equal(5, chart.Patches.Count);
    }

    [Fact]
    public void ChartDefinition_RejectsDuplicatePatchIds()
    {
        var patch = new ColorPatchDefinition("same", "Same", new Rgb8(1, 2, 3), null, 1.0, null);

        Assert.Throws<ArgumentException>(() => new ChartDefinition(
            "duplicate",
            "Duplicate",
            [patch, patch],
            ChartGenerationOptions.Default.Layout,
            null));
    }
}

