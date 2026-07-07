using System.Numerics;

namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Passes the input linear color through unchanged as scRGB.
/// </summary>
public sealed class ToneMapperDirectScRgb : IToneMapper
{
    public string Id => "direct-scrgb";

    public Vector4 Map(Vector4 linearColor, ToneMappingParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        return linearColor;
    }
}
