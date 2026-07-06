namespace WgcColorCalibrator.App.Models;

public sealed record DiagnosticsSnapshot(
    string ApplicationVersion,
    string DotNetVersion,
    string WindowsVersion,
    string WindowsAppSdkPackageVersion,
    ProbeStatus WgcSupportStatus,
    ProbeStatus HdrStatus,
    ProbeStatus CapturePixelFormatStatus);

