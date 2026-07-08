using SharpGen.Runtime;
using Vortice.DXGI;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Records the result of applying a color space to a swap chain.
/// </summary>
public readonly record struct ColorSpaceApplicationResult(
    ColorSpaceType RequestedColorSpace,
    uint SupportFlags,
    bool SetSucceeded,
    Result SetHResult);
