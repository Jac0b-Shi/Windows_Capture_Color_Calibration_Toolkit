using System.Numerics;

namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Scales linear sRGB by the requested paper white relative to the scRGB reference white of 80 nits.
/// </summary>
public sealed class ToneMapperReferenceWhiteScaled : IToneMapper
{
    public string Id => "reference-white-scaled";

    // scRGB defines 1.0 as 80 nits.
    private const double ScrgbReferenceWhiteNits = 80.0;

    public Vector4 Map(Vector4 linearColor, ToneMappingParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        float scale = (float)(parameters.PaperWhiteNits / ScrgbReferenceWhiteNits);
        float exposureScale = MathF.Pow(2.0f, (float)parameters.ExposureEv);
        float finalScale = scale * exposureScale;

        return new Vector4(
            linearColor.X * finalScale,
            linearColor.Y * finalScale,
            linearColor.Z * finalScale,
            linearColor.W);
    }
}
