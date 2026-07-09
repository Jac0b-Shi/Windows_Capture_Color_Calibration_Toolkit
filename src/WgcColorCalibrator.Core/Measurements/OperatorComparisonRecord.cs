using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Records the expected, captured, mapped, and delta values for a single patch
/// when comparing HDR-to-SDR operators.
/// </summary>
public sealed record OperatorComparisonRecord(
    string PatchId,
    ColorValue Expected,
    ColorValue Captured,
    RgbaFloat Mapped,
    RgbaFloat Delta);
