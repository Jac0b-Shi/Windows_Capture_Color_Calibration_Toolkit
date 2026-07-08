namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Records display luminance and HDR capability reported by DXGI output probing.
/// </summary>
public sealed record DisplayOutputMetadata
{
    public DisplayOutputMetadata(
        string displayName,
        bool hdrSupported,
        bool hdrActive,
        double maxLuminance,
        double maxFullFrameLuminance,
        double minLuminance,
        DisplayCapabilityProbeMethod capabilityProbeMethod = DisplayCapabilityProbeMethod.DxgiOutputDescription1)
    {
        DisplayName = displayName;
        HdrSupported = hdrSupported;
        HdrActive = hdrActive;
        MaxLuminance = maxLuminance;
        MaxFullFrameLuminance = maxFullFrameLuminance;
        MinLuminance = minLuminance;
        CapabilityProbeMethod = capabilityProbeMethod;
    }

    public string DisplayName { get; }

    public bool HdrSupported { get; }

    public bool HdrActive { get; }

    public double MaxLuminance { get; }

    public double MaxFullFrameLuminance { get; }

    public double MinLuminance { get; }

    /// <summary>
    /// Indicates how HDR capability was determined. <see cref="DisplayCapabilityProbeMethod.Unknown"/> means
    /// the display could not be matched to a DXGI output, so <see cref="HdrSupported"/> is a conservative fallback
    /// and should be reported as "capability unknown" rather than "HDR unsupported".
    /// </summary>
    public DisplayCapabilityProbeMethod CapabilityProbeMethod { get; }

    public bool HdrCapabilityKnown => CapabilityProbeMethod != DisplayCapabilityProbeMethod.Unknown;

    public static DisplayOutputMetadata Unknown { get; } = new(
        "Unknown",
        false,
        false,
        0.0,
        0.0,
        0.0,
        DisplayCapabilityProbeMethod.Unknown);
}
