using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Rendering.HdrToSdr;

/// <summary>
/// Maps a linear scRGB HDR value to a normalized [0, 1] SDR-linear value.
/// </summary>
public interface IHdrToSdrOperator
{
    string Id { get; }

    /// <summary>
    /// Maps the HDR input to a normalized SDR-linear value.
    /// The returned components are expected to be in the [0, 1] range;
    /// callers apply linear-to-sRGB encoding and quantization when needed.
    /// </summary>
    RgbaFloat Map(RgbaFloat hdr);
}
