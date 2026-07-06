using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.Core.Tests.Serialization;

public sealed class ChartCsvSerializerTests
{
    private static ChartLayoutDefinition Layout => ChartGenerationOptions.Default.Layout;

    [Fact]
    public void DeserializeChart_ReadsMvpCsvFormat()
    {
        const string csv = "id,label,r,g,b,category,weight\nwhite,#FFFFFF,255,255,255,manual,1\nblack,#000000,0,0,0,manual,0.5\n";

        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, "custom", "Custom", Layout);

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

    [Fact]
    public void Deserialize_CsvWithCommasInQuotedField()
    {
        const string csv = "id,label,r,g,b,category,weight\n\"a,b\",\"Label, with comma\",255,255,255,manual,1\n";

        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, "comma", "Comma", Layout);

        Assert.Equal("a,b", chart.Patches[0].Id);
        Assert.Equal("Label, with comma", chart.Patches[0].Label);
    }

    [Fact]
    public void Deserialize_CsvWithDoubleQuotesInField()
    {
        const string csv = "id,label,r,g,b,category,weight\ntest,\"She said \"\"hello\"\"\",255,255,255,manual,1\n";

        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, "quotes", "Quotes", Layout);

        Assert.Equal("She said \"hello\"", chart.Patches[0].Label);
    }

    [Fact]
    public void Deserialize_CsvWithCrLfLineEndings()
    {
        string csv = "id,label,r,g,b,category,weight\r\ncrlf,#CRLF,1,2,3,test,1\r\n";

        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, "crlf", "CRLF", Layout);

        Assert.Equal("crlf", chart.Patches[0].Id);
        Assert.Equal(new Rgb8(1, 2, 3), chart.Patches[0].ExpectedColor);
    }

    [Fact]
    public void Deserialize_CsvWithLfOnlyLineEndings()
    {
        string csv = "id,label,r,g,b,category,weight\nlf,#LF,1,2,3,test,1\n";

        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, "lf", "LF", Layout);

        Assert.Equal("lf", chart.Patches[0].Id);
    }

    [Fact]
    public void RoundTrip_PreservesMultilineField()
    {
        var patches = new[]
        {
            new ColorPatchDefinition(
                "multi",
                "Line 1\nLine 2",
                new Rgb8(128, 128, 128),
                "multi-line",
                1.0,
                null)
        };

        string csv = ChartCsvSerializer.SerializePatches(patches);
        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, "multi", "Multi", Layout);

        Assert.Equal("Line 1\nLine 2", chart.Patches[0].Label);
    }

    [Fact]
    public void Deserialize_EmptyFields()
    {
        const string csv = "id,label,r,g,b,category,weight\nempty,,128,128,128,,1\n";

        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, "empty", "Empty", Layout);

        Assert.Equal("", chart.Patches[0].Label);
        Assert.Null(chart.Patches[0].Category);
    }

    [Fact]
    public void Deserialize_UnclosedQuote_ThrowsFormatException()
    {
        const string csv = "id,label,r,g,b,category,weight\ntest,\"unclosed,255,255,255,manual,1\n";

        Assert.Throws<FormatException>(() =>
            ChartCsvSerializer.DeserializeChart(csv, "bad", "Bad", Layout));
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    public void Deserialize_InvalidWeight_ThrowsFormatException(string weight)
    {
        string csv = $"id,label,r,g,b,category,weight\ntest,#FFF,255,255,255,manual,{weight}\n";

        Assert.Throws<FormatException>(() =>
            ChartCsvSerializer.DeserializeChart(csv, "bad", "Bad", Layout));
    }

    [Fact]
    public void RoundTrip_ComplexFieldsPreserved()
    {
        var patches = new[]
        {
            new ColorPatchDefinition(
                "id,with,commas",
                """Label with "quotes" and , commas""",
                new Rgb8(100, 150, 200),
                "cat,egory",
                0.75,
                null)
        };

        string csv = ChartCsvSerializer.SerializePatches(patches);
        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, "complex", "Complex", Layout);

        Assert.Equal("id,with,commas", chart.Patches[0].Id);
        Assert.Equal("""Label with "quotes" and , commas""", chart.Patches[0].Label);
        Assert.Equal("cat,egory", chart.Patches[0].Category);
        Assert.Equal(0.75, chart.Patches[0].Weight);
    }
}
