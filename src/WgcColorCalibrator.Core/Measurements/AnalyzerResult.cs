namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Stores a named analyzer output payload.
/// </summary>
public sealed record AnalyzerResult(
    string AnalyzerId,
    string NameResourceKey,
    IReadOnlyDictionary<string, string> Values,
    IReadOnlyList<string> Warnings);

