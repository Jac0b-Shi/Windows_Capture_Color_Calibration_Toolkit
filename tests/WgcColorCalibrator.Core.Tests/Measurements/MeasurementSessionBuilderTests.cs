using System.Text.Json;
using System.Text.Json.Nodes;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Common;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.Core.Tests.Measurements;

public sealed class MeasurementSessionBuilderTests
{
    [Fact]
    public void Build_InvalidSample_DoesNotEncodeBlack()
    {
        // Arrange
        ChartDefinition chart = CreateChart();
        IReadOnlyList<PatchPlacement> layout = CreateLayout(chart);
        ChartRenderSession renderSession = CreateRenderSession(chart, layout);
        CaptureSummary captureSummary = CreateCaptureSummary();
        CaptureGeometry captureGeometry = CreateCaptureGeometry();
        PatchSample clippedSample = new(
            chart.Patches[0].Id,
            SampleMethod.CenterMean,
            0,
            null,
            null,
            new ChannelStatistics(
                new ChannelStatistic(0, 0, 0, 0, 0, 0),
                new ChannelStatistic(0, 0, 0, 0, 0, 0),
                new ChannelStatistic(0, 0, 0, 0, 0, 0)),
            new List<string> { "sample-region-clipped" });

        // Act
        MeasurementSession session = MeasurementSessionBuilder.Build(
            new ApplicationInfo("WgcColorCalibrator", "0.1.0"),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            chart,
            layout,
            renderSession,
            captureSummary,
            captureGeometry,
            [clippedSample]);

        MeasurementRecord record = session.Measurements[0];

        // Assert
        Assert.Equal(MeasurementValidity.SampleRegionClipped, record.Validity);
        Assert.Equal(ColorEncoding.Unknown, record.Captured.Encoding);
        Assert.Null(record.Captured.Rgb8);
        Assert.Null(record.Captured.Rgba);
    }

    [Fact]
    public void Build_InvalidSample_SerializesAsUnknown()
    {
        ChartDefinition chart = CreateChart();
        IReadOnlyList<PatchPlacement> layout = CreateLayout(chart);
        ChartRenderSession renderSession = CreateRenderSession(chart, layout);
        CaptureSummary captureSummary = CreateCaptureSummary();
        CaptureGeometry captureGeometry = CreateCaptureGeometry();
        PatchSample clippedSample = new(
            chart.Patches[0].Id,
            SampleMethod.CenterMean,
            0,
            null,
            null,
            new ChannelStatistics(
                new ChannelStatistic(0, 0, 0, 0, 0, 0),
                new ChannelStatistic(0, 0, 0, 0, 0, 0),
                new ChannelStatistic(0, 0, 0, 0, 0, 0)),
            new List<string> { "sample-region-clipped" });

        MeasurementSession session = MeasurementSessionBuilder.Build(
            new ApplicationInfo("WgcColorCalibrator", "0.1.0"),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            chart,
            layout,
            renderSession,
            captureSummary,
            captureGeometry,
            [clippedSample]);

        string json = ProfileJsonSerializer.Serialize(session);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement captured = document.RootElement
            .GetProperty("measurements")[0]
            .GetProperty("captured");

        Assert.Equal("unknown", captured.GetProperty("encoding").GetString());
        Assert.True(captured.GetProperty("rgb8").ValueKind == JsonValueKind.Null);
        Assert.True(captured.GetProperty("rgba").ValueKind == JsonValueKind.Null);
    }

    private static ChartDefinition CreateChart()
    {
        return new ChartDefinition(
            "test-chart",
            "Test Chart",
            [new ColorPatchDefinition("p1", "Patch", new Rgb8(128, 128, 128), null, 1.0, null)],
            ChartLayoutDefinition.Default,
            null);
    }

    private static IReadOnlyList<PatchPlacement> CreateLayout(ChartDefinition chart)
    {
        return ChartLayoutEngine.CreatePlacements(chart);
    }

    private static ChartRenderSession CreateRenderSession(ChartDefinition chart, IReadOnlyList<PatchPlacement> layout)
    {
        return new ChartRenderSession(
            "test-renderer",
            "DirectScRgb",
            chart,
            layout,
            RenderOutputMode.SdrSrgb,
            RenderOutputMode.SdrSrgb,
            "B8G8R8A8_UNORM",
            "RGB_FULL_G22_NONE_P709",
            false,
            1.0,
            new Size(0, 0),
            new SizeInt(0, 0),
            new SizeInt(0, 0),
            new SizeInt(0, 0),
            new Point(0, 0),
            ToneMappingParameters.Default,
            DisplayOutputMetadata.Unknown,
            [],
            DateTimeOffset.UtcNow,
            1.0,
            1.0,
            "identity");
    }

    private static CaptureSummary CreateCaptureSummary()
    {
        return new CaptureSummary(
            "wgc",
            CaptureSourceKind.Window,
            CapturePixelFormat.B8G8R8A8UIntNormalized,
            CapturePixelFormat.B8G8R8A8UIntNormalized,
            ColorEncoding.CaptureNative,
            false);
    }

    private static CaptureGeometry CreateCaptureGeometry()
    {
        return new CaptureGeometry(
            new WindowGeometrySnapshot(
                new ScreenRectInt(0, 0, 100, 100),
                null,
                new ScreenRectInt(0, 0, 100, 100)),
            new WindowGeometrySnapshot(
                new ScreenRectInt(0, 0, 100, 100),
                null,
                new ScreenRectInt(0, 0, 100, 100)),
            CaptureFrameOriginBasis.ClientRect,
            CaptureMappingStatus.Verified,
            new PixelPoint(0, 0),
            []);
    }
}
