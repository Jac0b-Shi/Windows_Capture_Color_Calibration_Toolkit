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
        double minLuminance)
    {
        DisplayName = displayName;
        HdrSupported = hdrSupported;
        HdrActive = hdrActive;
        MaxLuminance = maxLuminance;
        MaxFullFrameLuminance = maxFullFrameLuminance;
        MinLuminance = minLuminance;
    }

    public string DisplayName { get; }

    public bool HdrSupported { get; }

    public bool HdrActive { get; }

    public double MaxLuminance { get; }

    public double MaxFullFrameLuminance { get; }

    public double MinLuminance { get; }

    public static DisplayOutputMetadata Unknown { get; } = new(
        "Unknown",
        false,
        false,
        0.0,
        0.0,
        0.0);
}
