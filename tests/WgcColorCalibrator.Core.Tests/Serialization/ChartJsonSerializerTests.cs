using System.Text.Json;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.Core.Tests.Serialization;

public class ChartJsonSerializerTests
{
    [Fact]
    public void RoundTrip_PreservesChartDefinition()
    {
        var layout = new ChartLayoutDefinition(64, 64, 8, 16, 8, 4, new Rgb8(0, 0, 0));
        var patch = new ColorPatchDefinition("gray-128", "#808080", new Rgb8(128, 128, 128), "grayscale", 1.0, null);
        var rendering = new ChartRenderingParameters(
            RenderOutputMode.SdrSrgb,
            ToneMappingParameters.Default,
            80.0,
            ColorEncoding.SrgbEncoded);
        var chart = new ChartDefinition("grayscale-test", "Grayscale Test", [patch], layout, null, rendering);

        string json = ChartJsonSerializer.Serialize(chart);
        ChartDefinition roundTripped = ChartJsonSerializer.Deserialize(json);

        Assert.Equal(chart.Id, roundTripped.Id);
        Assert.Equal(chart.Name, roundTripped.Name);
        Assert.Single(roundTripped.Patches);
        Assert.Equal("gray-128", roundTripped.Patches[0].Id);
        Assert.Equal(new Rgb8(128, 128, 128), roundTripped.Patches[0].ExpectedColor);
        Assert.NotNull(roundTripped.RenderingParameters);
        Assert.Equal(RenderOutputMode.SdrSrgb, roundTripped.RenderingParameters!.RequestedOutputMode);
    }

    [Fact]
    public void Deserialize_InvalidSchemaVersion_Throws()
    {
        const string json = """
            {
              "schemaVersion": "0.0.0",
              "id": "test",
              "name": "Test",
              "patches": [{"id":"p1","label":"#FFFFFF","expectedColor":{"r":255,"g":255,"b":255},"weight":1.0}],
              "layout": {"patchWidth":64,"patchHeight":64,"gap":8,"safeSampleInset":8,"columnCount":4}
            }
            """;

        JsonException ex = Assert.Throws<JsonException>(() => ChartJsonSerializer.Deserialize(json));
        Assert.Contains("Unsupported schema version", ex.Message);
    }

    [Fact]
    public void Deserialize_DuplicatePatchIds_Throws()
    {
        const string json = """
            {
              "schemaVersion": "0.1.0",
              "id": "test",
              "name": "Test",
              "patches": [
                {"id":"p1","label":"#FFFFFF","expectedColor":{"r":255,"g":255,"b":255},"weight":1.0},
                {"id":"p1","label":"#000000","expectedColor":{"r":0,"g":0,"b":0},"weight":1.0}
              ],
              "layout": {"patchWidth":64,"patchHeight":64,"gap":8,"safeSampleInset":8,"columnCount":4}
            }
            """;

        Assert.Throws<ArgumentException>(() => ChartJsonSerializer.Deserialize(json));
    }

    [Fact]
    public void Deserialize_InvalidRgb_Throws()
    {
        const string json = """
            {
              "schemaVersion": "0.1.0",
              "id": "test",
              "name": "Test",
              "patches": [
                {"id":"p1","label":"#FFFFFF","expectedColor":{"r":300,"g":255,"b":255},"weight":1.0}
              ],
              "layout": {"patchWidth":64,"patchHeight":64,"gap":8,"safeSampleInset":8,"columnCount":4}
            }
            """;

        Assert.Throws<JsonException>(() => ChartJsonSerializer.Deserialize(json));
    }

    [Fact]
    public void RoundTrip_HdrPatch_PreservesHdrColor()
    {
        var layout = new ChartLayoutDefinition(64, 64, 8, 16, 8, 4, new Rgb8(0, 0, 0));
        var patch = new ColorPatchDefinition(
            "hdr-white",
            "HDR White",
            new Rgb8(255, 255, 255),
            "hdr",
            1.0,
            null,
            ColorEncoding.LinearScRgb,
            new HdrColor(2.5f, 2.5f, 2.5f));
        var rendering = new ChartRenderingParameters(
            RenderOutputMode.HdrScRgb,
            new ToneMappingParameters(200.0, 1000.0, 0.0),
            200.0,
            ColorEncoding.LinearScRgb);
        var chart = new ChartDefinition("hdr-test", "HDR Test", [patch], layout, null, rendering);

        string json = ChartJsonSerializer.Serialize(chart);
        ChartDefinition roundTripped = ChartJsonSerializer.Deserialize(json);

        Assert.Equal(RenderOutputMode.HdrScRgb, roundTripped.RenderingParameters!.RequestedOutputMode);
        Assert.Equal(ColorEncoding.LinearScRgb, roundTripped.Patches[0].SourceEncoding);
        Assert.Equal(2.5f, roundTripped.Patches[0].HdrColor!.Value.R);
    }
}
