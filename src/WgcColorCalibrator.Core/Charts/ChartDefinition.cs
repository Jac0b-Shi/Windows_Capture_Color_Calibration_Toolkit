using System.Text.Json.Serialization;
using WgcColorCalibrator.Core.Common;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Defines a chart, its stable patches, and the intended layout configuration.
/// </summary>
public sealed record ChartDefinition
{
    public ChartDefinition(
        string id,
        string name,
        IReadOnlyList<ColorPatchDefinition> patches,
        ChartLayoutDefinition layout,
        IReadOnlyDictionary<string, string>? metadata)
        : this(SchemaVersions.Current, id, name, patches, layout, metadata)
    {
    }

    [JsonConstructor]
    public ChartDefinition(
        string schemaVersion,
        string id,
        string name,
        IReadOnlyList<ColorPatchDefinition> patches,
        ChartLayoutDefinition layout,
        IReadOnlyDictionary<string, string>? metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(patches);
        ArgumentNullException.ThrowIfNull(layout);

        if (patches.Count == 0)
        {
            throw new ArgumentException("A chart must contain at least one patch.", nameof(patches));
        }

        string? duplicateId = patches
            .GroupBy(patch => patch.Id, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1)
            ?.Key;

        if (duplicateId is not null)
        {
            throw new ArgumentException($"Duplicate patch id: {duplicateId}", nameof(patches));
        }

        SchemaVersion = schemaVersion;
        Id = id;
        Name = name;
        Patches = patches;
        Layout = layout;
        Metadata = metadata;
    }

    public string SchemaVersion { get; }

    public string Id { get; }

    public string Name { get; }

    public IReadOnlyList<ColorPatchDefinition> Patches { get; }

    public ChartLayoutDefinition Layout { get; }

    public IReadOnlyDictionary<string, string>? Metadata { get; }
}
