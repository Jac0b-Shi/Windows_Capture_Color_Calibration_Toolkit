using System.Numerics;

namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Maps a linear color value to the target HDR output space.
/// </summary>
public interface IToneMapper
{
    string Id { get; }

    /// <summary>
    /// Maps a linear RGB color (input) to the output color that will be written to the swap chain.
    /// </summary>
    Vector4 Map(Vector4 linearColor, ToneMappingParameters parameters);
}
