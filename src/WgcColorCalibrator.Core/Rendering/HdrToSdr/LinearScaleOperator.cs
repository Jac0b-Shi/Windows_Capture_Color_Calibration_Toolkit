using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Rendering.HdrToSdr;

/// <summary>
/// Linearly scales the HDR value so that the configured input white scRGB value maps to SDR white.
/// </summary>
public sealed class LinearScaleOperator : IHdrToSdrOperator
{
    public LinearScaleOperator(float inputWhiteScRgb)
    {
        if (!float.IsFinite(inputWhiteScRgb) || inputWhiteScRgb <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(inputWhiteScRgb), "Input white scRGB must be a positive finite value.");
        }

        InputWhiteScRgb = inputWhiteScRgb;
    }

    public string Id => "linear-scale";

    public float InputWhiteScRgb { get; }

    public RgbaFloat Map(RgbaFloat hdr)
    {
        float scale = 1.0f / InputWhiteScRgb;
        return new RgbaFloat(Saturate(hdr.R * scale), Saturate(hdr.G * scale), Saturate(hdr.B * scale), hdr.A);
    }

    private static float Saturate(float value)
    {
        return value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value;
    }
}
