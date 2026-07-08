using SharpGen.Runtime;
using Vortice.DXGI;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Records the result of a color-space verification attempt on a swap chain.
/// </summary>
public readonly record struct ColorSpaceVerification(
    ColorSpaceType RequestedColorSpace,
    uint SupportFlags,
    Result SetColorSpaceResult,
    ColorSpaceType? ActualColorSpace);
