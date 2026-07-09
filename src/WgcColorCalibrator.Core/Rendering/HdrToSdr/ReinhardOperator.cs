using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Rendering.HdrToSdr;

/// <summary>
/// Applies the Reinhard tone-mapping curve: out = x / (1 + x).
/// </summary>
public sealed class ReinhardOperator : IHdrToSdrOperator
{
    public string Id => "reinhard";

    public RgbaFloat Map(RgbaFloat hdr)
    {
        return new RgbaFloat(Reinhard(hdr.R), Reinhard(hdr.G), Reinhard(hdr.B), hdr.A);
    }

    private static float Reinhard(float value)
    {
        return value <= 0.0f ? 0.0f : value / (1.0f + value);
    }
}
