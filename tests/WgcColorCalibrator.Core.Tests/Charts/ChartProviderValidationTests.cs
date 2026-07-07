using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Tests.Charts;

public class ChartProviderValidationTests
{
    [Fact]
    public void ManualSingleColorProvider_FFFFFF_CreatesCorrectPatch()
    {
        var options = new ChartGenerationOptions(
            new Rgb8(255, 255, 255),
            5,
            ChartLayoutDefinition.Default,
            RenderOutputMode.SdrSrgb,
            ToneMappingParameters.Default,
            null);
        var provider = new ManualSingleColorChartProvider();

        ChartDefinition chart = provider.Create(options);

        Assert.Single(chart.Patches);
        Assert.Equal("manual-ffffff", chart.Patches[0].Id);
        Assert.Equal(new Rgb8(255, 255, 255), chart.Patches[0].ExpectedColor);
    }

    [Fact]
    public void NearWhiteProvider_GeneratesUniquePatchIds()
    {
        var options = new ChartGenerationOptions(
            new Rgb8(255, 255, 255),
            5,
            ChartLayoutDefinition.Default,
            RenderOutputMode.SdrSrgb,
            ToneMappingParameters.Default,
            null);
        var provider = new NearWhiteChartProvider();

        ChartDefinition chart = provider.Create(options);

        List<string> ids = chart.Patches.Select(p => p.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact]
    public void GrayscaleProvider_IncludesBlackAndWhiteEndpoints()
    {
        var options = new ChartGenerationOptions(
            new Rgb8(255, 255, 255),
            5,
            ChartLayoutDefinition.Default,
            RenderOutputMode.SdrSrgb,
            ToneMappingParameters.Default,
            null);
        var provider = new GrayscaleChartProvider();

        ChartDefinition chart = provider.Create(options);

        Assert.Contains(chart.Patches, p => p.ExpectedColor == new Rgb8(0, 0, 0));
        Assert.Contains(chart.Patches, p => p.ExpectedColor == new Rgb8(255, 255, 255));
    }

    [Fact]
    public void GrayscaleProvider_StepsLessThanTwo_Throws()
    {
        var options = new ChartGenerationOptions(
            new Rgb8(255, 255, 255),
            1,
            ChartLayoutDefinition.Default,
            RenderOutputMode.SdrSrgb,
            ToneMappingParameters.Default,
            null);
        var provider = new GrayscaleChartProvider();

        Assert.Throws<ArgumentOutOfRangeException>(() => provider.Create(options));
    }

    [Fact]
    public void ChartLayoutDefinition_InvalidPatchSize_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartLayoutDefinition(0, 64, 0, 0, 0, 1, new Rgb8(0, 0, 0)));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartLayoutDefinition(64, 0, 0, 0, 0, 1, new Rgb8(0, 0, 0)));
    }

    [Fact]
    public void ChartLayoutDefinition_SafeSampleInsetTooLarge_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartLayoutDefinition(64, 64, 0, 0, 32, 1, new Rgb8(0, 0, 0)));
    }

    [Fact]
    public void ChartLayoutDefinition_ZeroColumnCount_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartLayoutDefinition(64, 64, 0, 0, 0, 0, new Rgb8(0, 0, 0)));
    }
}
