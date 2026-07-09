using System.Text.Json;
using Json.Schema;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.Core.Tests.Serialization;

public sealed class SchemaValidationTests
{
    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the repository root.");
    }

    [Fact]
    public void MeasurementSession_SerializedOutput_PassesSchema()
    {
        // Arrange — build a realistic session identical to the round-trip test
        ChartDefinition chart = new ManualSingleColorChartProvider().Create(ChartGenerationOptions.Default);
        IReadOnlyList<PatchPlacement> layout = ChartLayoutEngine.CreatePlacements(chart);

        int rows = (chart.Patches.Count + chart.Layout.ColumnCount - 1) / chart.Layout.ColumnCount;
        int intendedWidth = (chart.Layout.ColumnCount * chart.Layout.PatchWidth) + ((chart.Layout.ColumnCount - 1) * chart.Layout.Gap) + (2 * chart.Layout.Border);
        int intendedHeight = (rows * chart.Layout.PatchHeight) + ((rows - 1) * chart.Layout.Gap) + (2 * chart.Layout.Border);
        SizeInt intendedSize = new(intendedWidth, intendedHeight);

        var measurement = new MeasurementRecord(
            chart.Patches[0].Id,
            new ColorValue(ColorEncoding.SrgbEncoded, new Rgb8(255, 255, 255), null),
            null,
            new ColorValue(ColorEncoding.Unknown, new Rgb8(242, 242, 242), null),
            new SamplingSummary(SampleMethod.CenterMedian, 16),
            new ChannelStatistics(
                new ChannelStatistic(0, 0, 0, 0, 0, 0),
                new ChannelStatistic(0, 0, 0, 0, 0, 0),
                new ChannelStatistic(0, 0, 0, 0, 0, 0)),
            MeasurementValidity.Valid,
            []);

        var session = new MeasurementSession(
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

        string repositoryRoot = GetRepositoryRoot();
        string schemaPath = Path.Combine(repositoryRoot, "schemas", "measurement-profile.schema.json");
        JsonSchema schema = LoadSchema(schemaPath);

        // Act
        string json = ProfileJsonSerializer.Serialize(session);
        using JsonDocument document = JsonDocument.Parse(json);

        // Assert
        EvaluationResults result = schema.Evaluate(document.RootElement);
        Assert.True(result.IsValid, $"Schema validation failed: {FormatErrors(result)}");
    }

    [Theory]
    [InlineData("grayscale.sample.json")]
    [InlineData("near-white.sample.json")]
    public void SampleChartJson_DeserializesToChartDefinition_AndPassesSchema(string filename)
    {
        string repositoryRoot = GetRepositoryRoot();

        string jsonPath = Path.Combine(repositoryRoot, "samples", "charts", filename);
        Assert.True(File.Exists(jsonPath), $"Sample file not found: {jsonPath}");

        string json = File.ReadAllText(jsonPath);

        // Act — deserialize
        ChartDefinition chart = ProfileJsonSerializer.Deserialize<ChartDefinition>(json);

        // Assert basic structure
        Assert.NotNull(chart.Id);
        Assert.NotNull(chart.Name);
        Assert.NotEmpty(chart.Patches);
        Assert.NotNull(chart.Layout);

        // Act — validate against schema
        string schemaPath = Path.Combine(repositoryRoot, "schemas", "chart-definition.schema.json");
        JsonSchema schema = LoadSchema(schemaPath);
        using JsonDocument document = JsonDocument.Parse(json);
        EvaluationResults result = schema.Evaluate(document.RootElement);

        Assert.True(result.IsValid, $"Schema validation failed for {filename}: {FormatErrors(result)}");
    }

    [Fact]
    public void MeasurementProfile_MissingRequiredField_IsRejected()
    {
        const string json = """
        {
          "schemaVersion": "0.1.0",
          "application": { "name": "test", "version": "0.1.0" },
          "system": {},
          "chart": { "schemaVersion": "0.1.0", "id": "t", "name": "t", "patches": [{"id":"p","label":"l","expectedColor":{"r":0,"g":0,"b":0},"category":"c","weight":1}], "layout": {} },
          "layout": [],
          "measurements": [],
          "warnings": [],
          "createdAt": "2026-01-01T00:00:00Z"
        }
        """;

        JsonSchema schema = LoadMeasurementProfileSchema();
        using JsonDocument document = JsonDocument.Parse(json);
        EvaluationResults result = schema.Evaluate(document.RootElement);
        Assert.False(result.IsValid, "Expected rejection: missing 'capture' field.");
    }

    [Fact]
    public void MeasurementProfile_LayoutNotArray_IsRejected()
    {
        const string json = """
        {
          "schemaVersion": "0.1.0",
          "application": { "name": "test", "version": "0.1.0" },
          "system": {},
          "capture": { "backendId": "w", "sourceKind": "window", "requestedPixelFormat": "b8G8R8A8UIntNormalized", "actualPixelFormat": "b8G8R8A8UIntNormalized", "encoding": "unknown", "formatDowngraded": false },
          "chart": { "schemaVersion": "0.1.0", "id": "t", "name": "t", "patches": [{"id":"p","label":"l","expectedColor":{"r":0,"g":0,"b":0},"category":"c","weight":1}], "layout": {} },
          "layout": {},
          "measurements": [],
          "warnings": [],
          "createdAt": "2026-01-01T00:00:00Z"
        }
        """;

        JsonSchema schema = LoadMeasurementProfileSchema();
        using JsonDocument document = JsonDocument.Parse(json);
        EvaluationResults result = schema.Evaluate(document.RootElement);
        Assert.False(result.IsValid, "Expected rejection: 'layout' is not an array.");
    }

    private static JsonSchema LoadMeasurementProfileSchema()
    {
        string schemaPath = Path.Combine(GetRepositoryRoot(), "schemas", "measurement-profile.schema.json");
        return LoadSchema(schemaPath);
    }

    private static JsonSchema LoadSchema(string path)
    {
        string schemaText = File.ReadAllText(path);
        var options = new BuildOptions
        {
            SchemaRegistry = new SchemaRegistry()
        };
        return JsonSchema.FromText(schemaText, options);
    }

    private static string FormatErrors(EvaluationResults result)
    {
        if (result.IsValid)
        {
            return "(none)";
        }

        var lines = new List<string>();
        CollectErrors(result, lines, "");
        return string.Join("; ", lines);
    }

    private static void CollectErrors(EvaluationResults result, List<string> lines, string indent)
    {
        if (!result.IsValid && result.Errors is not null && result.Errors.Count > 0)
        {
            foreach (var error in result.Errors)
            {
                lines.Add($"{result.InstanceLocation}: {error.Key} — {error.Value}");
            }
        }

        if (result.Details is not null)
        {
            foreach (var detail in result.Details)
            {
                CollectErrors(detail, lines, indent + "  ");
            }
        }
    }
}
