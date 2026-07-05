using System.Text.Json.Serialization;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Common;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Represents the complete machine-readable input to analysis and export.
/// </summary>
public sealed record MeasurementSession
{
    public MeasurementSession(
        ApplicationInfo application,
        IReadOnlyDictionary<string, string> system,
        IReadOnlyDictionary<string, string> gpu,
        IReadOnlyDictionary<string, string> display,
        IReadOnlyDictionary<string, string> hdr,
        CaptureSummary capture,
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> layout,
        IReadOnlyList<MeasurementRecord> measurements,
        IReadOnlyList<AnalyzerResult> analysis,
        IReadOnlyList<string> warnings,
        DateTimeOffset createdAt)
        : this(SchemaVersions.Current, application, system, gpu, display, hdr, capture, chart, layout, measurements, analysis, warnings, createdAt)
    {
    }

    [JsonConstructor]
    public MeasurementSession(
        string schemaVersion,
        ApplicationInfo application,
        IReadOnlyDictionary<string, string> system,
        IReadOnlyDictionary<string, string> gpu,
        IReadOnlyDictionary<string, string> display,
        IReadOnlyDictionary<string, string> hdr,
        CaptureSummary capture,
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> layout,
        IReadOnlyList<MeasurementRecord> measurements,
        IReadOnlyList<AnalyzerResult> analysis,
        IReadOnlyList<string> warnings,
        DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(gpu);
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(hdr);
        ArgumentNullException.ThrowIfNull(capture);
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(measurements);
        ArgumentNullException.ThrowIfNull(analysis);
        ArgumentNullException.ThrowIfNull(warnings);

        SchemaVersion = schemaVersion;
        Application = application;
        System = system;
        Gpu = gpu;
        Display = display;
        Hdr = hdr;
        Capture = capture;
        Chart = chart;
        Layout = layout;
        Measurements = measurements;
        Analysis = analysis;
        Warnings = warnings;
        CreatedAt = createdAt;
    }

    public string SchemaVersion { get; }

    public ApplicationInfo Application { get; }

    public IReadOnlyDictionary<string, string> System { get; }

    public IReadOnlyDictionary<string, string> Gpu { get; }

    public IReadOnlyDictionary<string, string> Display { get; }

    public IReadOnlyDictionary<string, string> Hdr { get; }

    public CaptureSummary Capture { get; }

    public ChartDefinition Chart { get; }

    public IReadOnlyList<PatchPlacement> Layout { get; }

    public IReadOnlyList<MeasurementRecord> Measurements { get; }

    public IReadOnlyList<AnalyzerResult> Analysis { get; }

    public IReadOnlyList<string> Warnings { get; }

    public DateTimeOffset CreatedAt { get; }
}
