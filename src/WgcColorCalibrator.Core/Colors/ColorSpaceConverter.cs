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
    /// Converts linear BT.709 (scRGB) primaries to BT.2020 primaries.
    /// </summary>
    public static Vector3 LinearScRgbToRec2020(Vector3 linear)
    {
        return new Vector3(
            0.708f * linear.X + 0.292f * linear.Y + 0.000f * linear.Z,
            0.170f * linear.X + 0.797f * linear.Y + 0.033f * linear.Z,
            0.131f * linear.X + 0.046f * linear.Y + 0.823f * linear.Z);
    }

    /// <summary>
    /// Encodes a normalized linear value (1.0 = 10,000 nits) with SMPTE ST.2084 PQ.
    /// </summary>
    public static float PqEncode(float normalized)
    {
        if (normalized <= 0.0f)
            return 0.0f;

        const float m1 = 2610.0f / 4096.0f / 4.0f;
        const float m2 = 2523.0f / 4096.0f * 128.0f;
        const float c1 = 3424.0f / 4096.0f;
        const float c2 = 2413.0f / 4096.0f * 32.0f;
        const float c3 = 2392.0f / 4096.0f * 32.0f;

        float ePow = MathF.Pow(normalized, m1);
        float numerator = c1 + c2 * ePow;
        float denominator = 1.0f + c3 * ePow;
        return MathF.Pow(numerator / denominator, m2);
    }

    /// <summary>
    /// Decodes a PQ-encoded value back to a normalized linear value (1.0 = 10,000 nits).
    /// </summary>
    public static float PqDecode(float pq)
    {
        if (pq <= 0.0f)
            return 0.0f;

        const float m1 = 2610.0f / 4096.0f / 4.0f;
        const float m2 = 2523.0f / 4096.0f * 128.0f;
        const float c1 = 3424.0f / 4096.0f;
        const float c2 = 2413.0f / 4096.0f * 32.0f;
        const float c3 = 2392.0f / 4096.0f * 32.0f;

        float nPow = MathF.Pow(pq, 1.0f / m2);
        float numerator = MathF.Max(nPow - c1, 0.0f);
        float denominator = c2 - c3 * nPow;
        return denominator <= 0.0f
            ? 0.0f
            : MathF.Pow(numerator / denominator, 1.0f / m1);
    }

    /// <summary>
    /// Converts a luminance in nits to a PQ-encoded value.
    /// </summary>
    public static float NitsToPqCodeValue(float nits) => PqEncode(nits / 10_000.0f);

    /// <summary>
    /// Converts a PQ-encoded value to luminance in nits.
    /// </summary>
    public static float PqCodeValueToNits(float pq) => PqDecode(pq) * 10_000.0f;
}
