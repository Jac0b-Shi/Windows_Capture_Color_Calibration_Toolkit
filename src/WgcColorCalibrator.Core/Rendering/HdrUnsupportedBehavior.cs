namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Decides what the chart window should do when HDR is requested but the display or system does not support it.
/// </summary>
public enum HdrUnsupportedBehavior
{
    Cancel,
    SwitchToSdr,
    AllowClippingExperiment
}
