using System.Numerics;

namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Converts between color spaces used by the chart renderer.
/// </summary>
public static class ColorSpaceConverter
{
    // sRGB reference white is 80 nits in the Windows scRGB convention.
    public const double ScrgbReferenceWhiteNits = 80.0;

    /// <summary>
    /// Converts an sRGB-encoded byte value to a linear RGB value in [0, 1].
    /// </summary>
    public static float SrgbByteToLinear(byte value)
    {
        float normalized = value / 255.0f;
        return normalized <= 0.04045f
            ? normalized / 12.92f
            : MathF.Pow((normalized + 0.055f) / 1.055f, 2.4f);
    }

    /// <summary>
    /// Converts a linear RGB value in [0, 1] to an sRGB-encoded byte value.
    /// </summary>
    public static byte LinearToSrgbByte(float linear)
    {
        float srgb = linear <= 0.0031308f
            ? linear * 12.92f
            : (MathF.Pow(linear, 1.0f / 2.4f) * 1.055f) - 0.055f;
        int scaled = (int)Math.Round(srgb * 255.0f, MidpointRounding.AwayFromZero);
        return (byte)Math.Clamp(scaled, 0, 255);
    }

    /// <summary>
    /// Converts an sRGB Rgb8 color to a linear RGB vector.
    /// </summary>
    public static Vector3 SrgbToLinear(Rgb8 color) => new(
        SrgbByteToLinear(color.R),
        SrgbByteToLinear(color.G),
        SrgbByteToLinear(color.B));

    /// <summary>
    /// Converts a linear RGB vector to an sRGB Rgb8 color.
    /// </summary>
    public static Rgb8 LinearToSrgb(Vector3 linear) => new(
        LinearToSrgbByte(linear.X),
        LinearToSrgbByte(linear.Y),
        LinearToSrgbByte(linear.Z));

    /// <summary>
    /// Converts a target luminance in nits to the equivalent scRGB value.
    /// </summary>
    public static double NitsToScRgb(double nits) => nits / ScrgbReferenceWhiteNits;

    /// <summary>
    /// Converts an scRGB value to the equivalent luminance in nits.
    /// </summary>
    public static double ScRgbToNits(double scRgb) => scRgb * ScrgbReferenceWhiteNits;

    /// <summary>
    /// Applies exposure in EV to a linear value.
    /// </summary>
    public static float ApplyExposureEv(float linear, double exposureEv) => linear * MathF.Pow(2.0f, (float)exposureEv);

    /// <summary>
    /// Applies exposure in EV to a linear vector.
    /// </summary>
    public static Vector3 ApplyExposureEv(Vector3 linear, double exposureEv) => new(
        ApplyExposureEv(linear.X, exposureEv),
        ApplyExposureEv(linear.Y, exposureEv),
        ApplyExposureEv(linear.Z, exposureEv));
}
