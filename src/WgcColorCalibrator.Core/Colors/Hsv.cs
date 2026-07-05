namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Represents HSV values. Hue is degrees in [0, 360); saturation and value are in [0, 1].
/// </summary>
public readonly record struct Hsv(double H, double S, double V);

