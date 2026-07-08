namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Probes the display attached to a window for HDR capability and luminance metadata.
/// </summary>
public interface IDisplayOutputProbe
{
    DisplayOutputMetadata Probe(nint windowHandle);
}
