using System.Runtime.InteropServices;
using WgcColorCalibrator.App.Models;

namespace WgcColorCalibrator.App.Services;

public sealed class DiagnosticsSnapshotService
{
    private readonly AppSettings appSettings;

    public DiagnosticsSnapshotService(AppSettings appSettings)
    {
        this.appSettings = appSettings;
    }

    public DiagnosticsSnapshot CreateSnapshot() => new(
        ApplicationVersion: appSettings.Version,
        DotNetVersion: RuntimeInformation.FrameworkDescription,
        WindowsVersion: Environment.OSVersion.VersionString,
        WindowsAppSdkPackageVersion: appSettings.WindowsAppSdkPackageVersion,
        WgcSupportStatus: "not-probed",
        HdrStatus: "not-probed",
        CapturePixelFormatStatus: "not-selected");
}
