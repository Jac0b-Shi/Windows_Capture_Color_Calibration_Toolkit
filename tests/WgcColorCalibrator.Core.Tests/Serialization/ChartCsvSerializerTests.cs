using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.Core.Tests.Serialization;

public sealed class ChartCsvSerializerTests
{
    [Fact]
    public void DeserializeChart_ReadsMvpCsvFormat()
    {
        const string csv = """
            id,label,r,g,b,category,weight
            white,#FFFFFF,255,255,255,manual,1
            black,#000000,0,0,0,manual,0.5
            """;

        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(
            csv,
            "custom",
            "Custom",
            ChartGenerationOptions.Default.Layout);

        Assert.Equal(2, chart.Patches.Count);
        Assert.Equal(new Rgb8(255, 255, 255), chart.Patches[0].ExpectedColor);
        Assert.Equal(0.5, chart.Patches[1].Weight);
    }

    [Fact]
    public void SerializePatches_WritesMvpCsvFormat()
    {
        ChartDefinition chart = new ManualSingleColorChartProvider().Create(ChartGenerationOptions.Default);

        string csv = ChartCsvSerializer.SerializePatches(chart.Patches);

        Assert.Contains("id,label,r,g,b,category,weight", csv, StringComparison.Ordinal);
        Assert.Contains("manual-ffffff,#FFFFFF,255,255,255,manual,1", csv, StringComparison.Ordinal);
    }
}

