namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Represents CIE L*a*b* coordinates. Interpretation depends on the source encoding and white point.
/// </summary>
public readonly record struct Lab(double L, double A, double B);

