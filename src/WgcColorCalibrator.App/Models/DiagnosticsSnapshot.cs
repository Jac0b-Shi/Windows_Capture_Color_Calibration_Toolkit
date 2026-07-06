namespace WgcColorCalibrator.App.Models;

public sealed record DiagnosticsSnapshot(
    string ApplicationVersion,
    string DotNetVersion,
    string WindowsVersion,
    string WindowsAppSdkRelease,
    string WindowsAppRuntimeVersion,
    ProbeStatus WgcSupportStatus,
    ProbeStatus HdrStatus,
    ProbeStatus CapturePixelFormatStatus);

