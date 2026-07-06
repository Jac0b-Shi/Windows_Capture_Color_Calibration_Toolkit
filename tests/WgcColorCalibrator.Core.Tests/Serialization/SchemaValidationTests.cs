using System.Text.Json;
using Json.Schema;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Measurements;
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

        var measurement = new MeasurementRecord(
            chart.Patches[0].Id,
            new ColorValue(ColorEncoding.SrgbEncoded, new Rgb8(255, 255, 255), null),
            null,
            new ColorValue(ColorEncoding.Unknown, new Rgb8(242, 242, 242), null),
            new SamplingSummary(SampleMethod.CenterMedian, 16),
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
