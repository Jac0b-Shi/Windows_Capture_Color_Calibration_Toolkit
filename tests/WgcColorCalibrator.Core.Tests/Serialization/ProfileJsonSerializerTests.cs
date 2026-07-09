using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Common;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.Core.Tests.Serialization;

public sealed class ProfileJsonSerializerTests
{
    [Fact]
    public void MeasurementSession_RoundTripsWithSchemaVersion()
    {
        MeasurementSession session = CreateSession();

        string json = ProfileJsonSerializer.Serialize(session);
        MeasurementSession roundTripped = ProfileJsonSerializer.Deserialize<MeasurementSession>(json);

        Assert.Equal(SchemaVersions.MeasurementProfileCurrent, roundTripped.SchemaVersion);
        Assert.Equal("wgc", roundTripped.Capture.BackendId);
        Assert.Equal(session.Measurements[0].Captured.Rgb8, roundTripped.Measurements[0].Captured.Rgb8);
    }

    [Fact]
    public void MeasurementCsvSerializer_WritesExpectedCapturedAndDelta()
    {
        MeasurementSession session = CreateSession();

        string csv = MeasurementCsvSerializer.Serialize(session);

        Assert.Contains("patchId,label,expectedR,expectedG,expectedB,capturedR,capturedG,capturedB,deltaR,deltaG,deltaB,capturePixelFormat,sampleMethod", csv, StringComparison.Ordinal);
        Assert.Contains("manual-ffffff,#FFFFFF,255,255,255,242,242,242,-13,-13,-13,B8G8R8A8UIntNormalized,CenterMedian", csv, StringComparison.Ordinal);
    }

    private static MeasurementSession CreateSession()
    {
        ChartDefinition chart = new ManualSingleColorChartProvider().Create(ChartGenerationOptions.Default);
        IReadOnlyList<PatchPlacement> layout = ChartLayoutEngine.CreatePlacements(chart);

        int rows = (chart.Patches.Count + chart.Layout.ColumnCount - 1) / chart.Layout.ColumnCount;
        int intendedWidth = (chart.Layout.ColumnCount * chart.Layout.PatchWidth) + ((chart.Layout.ColumnCount - 1) * chart.Layout.Gap) + (2 * chart.Layout.Border);
        int intendedHeight = (rows * chart.Layout.PatchHeight) + ((rows - 1) * chart.Layout.Gap) + (2 * chart.Layout.Border);
        SizeInt intendedSize = new(intendedWidth, intendedHeight);

        var channel = new ChannelStatistic(242, 242, 242, 242, 0, 1);
        var channels = new ChannelStatistics(channel, channel, channel);
        var measurement = new MeasurementRecord(
            chart.Patches[0].Id,
            new ColorValue(ColorEncoding.SrgbEncoded, new Rgb8(255, 255, 255), null),
            null,
            new ColorValue(ColorEncoding.Unknown, new Rgb8(242, 242, 242), null),
            new SamplingSummary(SampleMethod.CenterMedian, 16),
            channels,
            MeasurementValidity.Valid,
            []);

        return new MeasurementSession(
            new ApplicationInfo("WgcColorCalibrator", "0.1.0"),
            new Dictionary<string, string> { ["windowsBuild"] = "unknown" },
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new CaptureSummary(
                "wgc",
                CaptureSourceKind.Window,
                CapturePixelFormat.B8G8R8A8UIntNormalized,
                CapturePixelFormat.B8G8R8A8UIntNormalized,
                ColorEncoding.Unknown,
                FormatDowngraded: false),
            chart,
            layout,
            new RenderSummary(
                "xaml",
                "DirectScRgb",
                RenderOutputMode.SdrSrgb,
                RenderOutputMode.SdrSrgb,
                "B8G8R8A8_UNORM",
                "RGB_FULL_G22_NONE_P709",
                false,
                new ToneMappingParameters(80.0, 1000.0, 0.0),
                intendedSize,
                intendedSize,
                intendedSize,
                new PixelPoint(0, 0),
                1.0,
                1.0,
                null,
                null,
                []),
            new CaptureGeometry(
                new WindowGeometrySnapshot(
                    new ScreenRectInt(0, 0, intendedSize.Width, intendedSize.Height),
                    null,
                    new ScreenRectInt(0, 0, intendedSize.Width, intendedSize.Height)),
                new WindowGeometrySnapshot(
                    new ScreenRectInt(0, 0, intendedSize.Width, intendedSize.Height),
                    null,
                    new ScreenRectInt(0, 0, intendedSize.Width, intendedSize.Height)),
                CaptureFrameOriginBasis.ClientRect,
                CaptureMappingStatus.Verified,
                new PixelPoint(0, 0),
                []),
            [measurement],
            [],
            [],
            new DateTimeOffset(2026, 7, 5, 0, 0, 0, TimeSpan.Zero));
    }
}

