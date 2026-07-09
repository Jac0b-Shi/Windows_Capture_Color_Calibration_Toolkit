using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Rendering.HdrToSdr;

/// <summary>
/// Clamps the linear scRGB value to the SDR [0, 1] range.
/// </summary>
public sealed class ClampToSdrOperator : IHdrToSdrOperator
{
    public string Id => "clamp-to-sdr";

    public RgbaFloat Map(RgbaFloat hdr)
    {
        return new RgbaFloat(Saturate(hdr.R), Saturate(hdr.G), Saturate(hdr.B), hdr.A);
    }

    private static float Saturate(float value)
    {
        return value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value;
    }
}
