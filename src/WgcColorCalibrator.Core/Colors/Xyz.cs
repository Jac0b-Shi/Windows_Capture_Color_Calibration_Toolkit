namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Represents CIE XYZ coordinates. Interpretation depends on the source encoding and white point.
/// </summary>
public readonly record struct Xyz(double X, double Y, double Z);

