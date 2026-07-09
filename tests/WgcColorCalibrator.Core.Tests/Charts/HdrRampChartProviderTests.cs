using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Tests.Charts;

public sealed class HdrRampChartProviderTests
{
    [Fact]
    public void Create_ProducesLinearScRgbHdrPatches()
    {
        var provider = new HdrRampChartProvider();

        ChartDefinition chart = provider.Create(ChartGenerationOptions.Default with
        {
            OutputMode = RenderOutputMode.HdrScRgb,
            ToneMappingMode = ToneMappingMode.DirectScRgb
        });

        Assert.Equal("hdr-ramp", chart.Id);
        Assert.NotNull(chart.RenderingParameters);
        Assert.Equal(ColorEncoding.LinearScRgb, chart.RenderingParameters.SourceEncoding);
        Assert.All(chart.Patches, patch =>
        {
            Assert.Equal(ColorEncoding.LinearScRgb, patch.SourceEncoding);
            Assert.True(patch.HdrColor.HasValue);
            Assert.True(patch.IsHdrOnly);
        });

        Assert.Contains(chart.Patches, p => p.HdrColor!.Value.R > 1.0f);
    }

    [Fact]
    public void Create_PatchesContainExpectedRampValues()
    {
        var provider = new HdrRampChartProvider();

        ChartDefinition chart = provider.Create(ChartGenerationOptions.Default);

        Assert.Equal(10, chart.Patches.Count);

        ColorPatchDefinition first = chart.Patches[0];
        Assert.Equal(0.0f, first.HdrColor!.Value.R);

        ColorPatchDefinition last = chart.Patches[^1];
        Assert.Equal(4.0f, last.HdrColor!.Value.R);
    }
}
