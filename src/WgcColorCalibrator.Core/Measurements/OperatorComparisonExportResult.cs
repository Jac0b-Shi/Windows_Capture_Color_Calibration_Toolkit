namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Describes the files produced by an HDR-to-SDR operator comparison export.
/// </summary>
public sealed record OperatorComparisonExportResult(
    int OperatorCount,
    int PatchCount,
    IReadOnlyList<string> FileNames);
