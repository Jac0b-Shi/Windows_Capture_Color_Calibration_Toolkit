namespace WgcColorCalibrator.App.Models;

public sealed record DiagnosticsSnapshot(
    string ApplicationVersion,
    string DotNetVersion,
    string WindowsVersion,
    string WindowsAppSdkPackageVersion,
    string WgcSupportStatus,
    string HdrStatus,
    string CapturePixelFormatStatus);

