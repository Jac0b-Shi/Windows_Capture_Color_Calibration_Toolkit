using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.Core.Rendering.HdrToSdr;

namespace WgcColorCalibrator.Core.Tests.Measurements;

public sealed class OperatorComparisonServiceTests
{
    [Fact]
    public void Compare_Fp16Session_AppliesOperatorsAndBuildsPreview()
    {
        MeasurementSession session = CreateFp16Session();
        OperatorComparisonService service = new();
        IReadOnlyList<IHdrToSdrOperator> operators = [new ClampToSdrOperator(), new ReinhardOperator()];

        IReadOnlyList<OperatorComparisonResult> results = service.Compare(session, operators);

        Assert.Equal(2, results.Count);
        OperatorComparisonResult clampResult = results[0];
        Assert.Equal("clamp-to-sdr", clampResult.OperatorId);
        Assert.Single(clampResult.Records);
        Assert.Equal(2.0f, clampResult.Records[0].Captured.Rgba!.Value.R, 4);
        Assert.Equal(1.0f, clampResult.Records[0].Expected.Rgba!.Value.R, 4);
        Assert.Equal(1.0f, clampResult.Records[0].Mapped.R, 4);
        Assert.Equal(1.0f, clampResult.Records[0].Delta.R, 4);
        Assert.NotNull(clampResult.PreviewBgra8);
        Assert.Equal(10, clampResult.PreviewSize.Width);
        Assert.Equal(10, clampResult.PreviewSize.Height);

        OperatorComparisonResult reinhardResult = results[1];
        Assert.Equal("reinhard", reinhardResult.OperatorId);
        Assert.Equal(2.0f / 3.0f, reinhardResult.Records[0].Mapped.R, 4);
    }

    [Fact]
    public void Compare_Bgra8Session_ThrowsInvalidOperationException()
    {
        MeasurementSession session = CreateBgra8Session();
        OperatorComparisonService service = new();
        IReadOnlyList<IHdrToSdrOperator> operators = [new ClampToSdrOperator()];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => service.Compare(session, operators));
        Assert.Equal("Operator comparison is only supported for FP16 captures.", exception.Message);
    }

    [Fact]
    public void Serialize_ContainsOperatorColumnsAndPatchData()
    {
        MeasurementSession session = CreateFp16Session();
        OperatorComparisonService service = new();
        IReadOnlyList<IHdrToSdrOperator> operators = [new ClampToSdrOperator(), new ReinhardOperator()];
        IReadOnlyList<OperatorComparisonResult> results = service.Compare(session, operators);

        string csv = OperatorComparisonCsvSerializer.Serialize(results, session);

        Assert.Contains("PatchId,ExpectedR,ExpectedG,ExpectedB,ExpectedA,CapturedR", csv);
        Assert.Contains("DeltaR,DeltaG,DeltaB", csv);
        Assert.Contains("clamp-to-sdr-MappedR", csv);
        Assert.Contains("reinhard-MappedR", csv);
        Assert.Contains(session.Measurements[0].PatchId, csv);
    }

    private static MeasurementSession CreateFp16Session()
    {
        ColorValue expected = new(ColorEncoding.LinearScRgb, null, new RgbaFloat(1.0f, 0.0f, 0.0f, 1.0f));
        ColorValue captured = new(ColorEncoding.CaptureNative, null, new RgbaFloat(2.0f, 0.0f, 0.0f, 1.0f));
        MeasurementRecord record = new(
            "p1",
            expected,
            null,
            captured,
            new SamplingSummary(SampleMethod.CenterMean, 1),
            new ChannelStatistics(
                new ChannelStatistic(2.0, 2.0, 2.0, 2.0, 0.0, 1),
                new ChannelStatistic(0.0, 0.0, 0.0, 0.0, 0.0, 1),
                new ChannelStatistic(0.0, 0.0, 0.0, 0.0, 0.0, 1)),
            MeasurementValidity.Valid,
            []);

        ChartDefinition chart = new(
            "test-chart",
            "Test Chart",
            [new ColorPatchDefinition("p1", "Patch", new Rgb8(255, 0, 0), null, 1.0, null)],
            ChartLayoutDefinition.Default,
            null);

        PatchPlacement placement = new("p1", new PixelRect(0, 0, 10, 10), new PixelRect(2, 2, 6, 6));

        return CreateSession(chart, [placement], [record], CapturePixelFormat.R16G16B16A16Float);
    }

    private static MeasurementSession CreateBgra8Session()
    {
        ChartDefinition chart = new(
            "test-chart",
            "Test Chart",
            [new ColorPatchDefinition("p1", "Patch", new Rgb8(128, 128, 128), null, 1.0, null)],
            ChartLayoutDefinition.Default,
            null);
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 10, 10), new PixelRect(2, 2, 6, 6));
        MeasurementRecord record = new(
            "p1",
            new ColorValue(ColorEncoding.SrgbEncoded, new Rgb8(128, 128, 128), null),
            null,
            new ColorValue(ColorEncoding.CaptureNative, new Rgb8(128, 128, 128), null),
            new SamplingSummary(SampleMethod.CenterMean, 1),
            new ChannelStatistics(
                new ChannelStatistic(0.5, 0.5, 0.5, 0.5, 0.0, 1),
                new ChannelStatistic(0.5, 0.5, 0.5, 0.5, 0.0, 1),
                new ChannelStatistic(0.5, 0.5, 0.5, 0.5, 0.0, 1)),
            MeasurementValidity.Valid,
            []);

        return CreateSession(chart, [placement], [record], CapturePixelFormat.B8G8R8A8UIntNormalized);
    }

    private static MeasurementSession CreateSession(
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> layout,
        IReadOnlyList<MeasurementRecord> measurements,
        CapturePixelFormat pixelFormat)
    {
        CaptureSummary capture = new(
            "wgc",
            CaptureSourceKind.Window,
            pixelFormat,
            pixelFormat,
            ColorEncoding.CaptureNative,
            false);

        CaptureGeometry geometry = new(
            new WindowGeometrySnapshot(new ScreenRectInt(0, 0, 100, 100), null, new ScreenRectInt(0, 0, 100, 100)),
            new WindowGeometrySnapshot(new ScreenRectInt(0, 0, 100, 100), null, new ScreenRectInt(0, 0, 100, 100)),
            CaptureFrameOriginBasis.ClientRect,
            CaptureMappingStatus.Verified,
            new PixelPoint(0, 0),
            []);

        RenderSummary renderSummary = new(
            "test-renderer",
            "DirectScRgb",
            RenderOutputMode.SdrSrgb,
            RenderOutputMode.SdrSrgb,
            "R16G16B16A16_FLOAT",
            "RGB_FULL_G10_NONE_P709",
            false,
            ToneMappingParameters.Default,
            new SizeInt(0, 0),
            new SizeInt(0, 0),
            new SizeInt(0, 0),
            new PixelPoint(0, 0),
            1.0,
            1.0,
            "identity",
            DisplayOutputMetadata.Unknown,
            []);

        return new MeasurementSession(
            "1",
            new ApplicationInfo("WgcColorCalibrator", "0.1.0"),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            new Dictionary<string, string>(),
            capture,
            chart,
            layout,
            renderSummary,
            geometry,
            measurements,
            [],
            [],
            DateTimeOffset.UtcNow);
    }
}
