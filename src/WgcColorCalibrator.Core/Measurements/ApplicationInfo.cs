namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Identifies the application that created a profile.
/// </summary>
public sealed record ApplicationInfo(
    string Name,
    string Version);

