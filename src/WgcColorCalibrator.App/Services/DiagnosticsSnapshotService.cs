using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Windows.ApplicationModel.WindowsAppRuntime;
using WgcColorCalibrator.App.Models;

namespace WgcColorCalibrator.App.Services;

public sealed class DiagnosticsSnapshotService
{
    public DiagnosticsSnapshot CreateSnapshot()
    {
        return new(
            ApplicationVersion: GetApplicationVersion(),
            DotNetVersion: RuntimeInformation.FrameworkDescription,
            WindowsVersion: Environment.OSVersion.VersionString,
            WindowsAppSdkPackageVersion: GetWindowsAppSdkVersion(),
            WgcSupportStatus: ProbeStatus.NotProbed,
            HdrStatus: ProbeStatus.NotProbed,
            CapturePixelFormatStatus: ProbeStatus.NotSelected);
    }

    private static string GetApplicationVersion()
    {
        // Packaged: read MSIX identity version
        try
        {
            if (global::Windows.ApplicationModel.Package.Current is { } package)
            {
                var v = package.Id.Version;
                return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
            }
        }
        catch
        {
            // Not running packaged — fall back to assembly version
        }

        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetName().Version;
        if (version is not null)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        return "unknown";
    }

    private static string GetWindowsAppSdkVersion()
    {
        try
        {
            // Official Windows App SDK VersionInfo API (WinRT)
            var release = ReleaseInfo.AsString;
            return release;
        }
        catch
        {
            return "unknown";
        }
    }
}
