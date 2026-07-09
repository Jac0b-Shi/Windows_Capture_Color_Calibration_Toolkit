namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Indicates whether a single patch measurement can be trusted.
/// </summary>
public enum MeasurementValidity
{
    Valid,
    GeometryUnverified,
    SampleRegionClipped,
    EmptySample
}

/// <summary>
/// Indicates whether a measurement session can be trusted.
/// </summary>
public enum MeasurementSessionValidity
{
    Valid,
    DiagnosticOnly
}
