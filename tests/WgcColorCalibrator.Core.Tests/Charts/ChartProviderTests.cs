using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering;

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
    public void ManualSingleColorProvider_HdrColor_ProducesLinearScRgbPatch()
    {
        var provider = new ManualSingleColorChartProvider();
        var hdrColor = new HdrColor(1.0f, 2.0f, 3.0f);
        ChartDefinition chart = provider.Create(ChartGenerationOptions.Default with
        {
            ManualColor = new Rgb8(255, 255, 255),
            ManualHdrColor = hdrColor,
            OutputMode = RenderOutputMode.HdrScRgb
        });

        Assert.Single(chart.Patches);
        Assert.Equal(ColorEncoding.LinearScRgb, chart.Patches[0].SourceEncoding);
        Assert.True(chart.Patches[0].HdrColor.HasValue);
        Assert.Equal(hdrColor, chart.Patches[0].HdrColor.Value);
        Assert.Equal(ToneMappingMode.DirectScRgb, chart.RenderingParameters?.ToneMappingMode);
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
    public void GrayscaleProvider_PreservesToneMappingMode()
    {
        var provider = new GrayscaleChartProvider();
        ChartDefinition chart = provider.Create(ChartGenerationOptions.Default with
        {
            GrayscaleSteps = 5,
            ToneMappingMode = ToneMappingMode.ReferenceWhiteScaled
        });

        Assert.Equal(ToneMappingMode.ReferenceWhiteScaled, chart.RenderingParameters?.ToneMappingMode);
    }

    [Fact]
    public void NearWhiteProvider_PreservesToneMappingMode()
    {
        var provider = new NearWhiteChartProvider();
        ChartDefinition chart = provider.Create(ChartGenerationOptions.Default with
        {
            ToneMappingMode = ToneMappingMode.ReferenceWhiteScaled
        });

        Assert.Equal(ToneMappingMode.ReferenceWhiteScaled, chart.RenderingParameters?.ToneMappingMode);
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

