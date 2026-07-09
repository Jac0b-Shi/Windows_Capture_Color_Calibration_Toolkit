using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Contains the per-operator comparison records and a generated SDR preview bitmap.
/// </summary>
public sealed record OperatorComparisonResult(
    string OperatorId,
    string OperatorDisplayName,
    IReadOnlyList<OperatorComparisonRecord> Records,
    SizeInt PreviewSize,
    byte[] PreviewBgra8);
