using System.Reflection;
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

    public DiagnosticsSnapshot CreateSnapshot()
    {
        string applicationVersion = GetApplicationVersion();
        string windowsAppSdkVersion = GetWindowsAppSdkVersion();

        return new(
            ApplicationVersion: applicationVersion,
            DotNetVersion: RuntimeInformation.FrameworkDescription,
            WindowsVersion: Environment.OSVersion.VersionString,
            WindowsAppSdkPackageVersion: windowsAppSdkVersion,
            WgcSupportStatus: "not-probed",
            HdrStatus: "not-probed",
            CapturePixelFormatStatus: "not-selected");
    }

    private static string GetApplicationVersion()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly is null)
        {
            return "unknown";
        }

        var version = assembly.GetName().Version;
        if (version is null)
        {
            return "unknown";
        }

        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string GetWindowsAppSdkVersion()
    {
        try
        {
            // Attempt to read the Windows App SDK runtime version at runtime
            var wapAssembly = Assembly.Load("Microsoft.WindowsAppRuntime");
            var version = wapAssembly.GetName().Version;
            return version is not null
                ? $"{version.Major}.{version.Minor}.{version.Build}"
                : "unknown";
        }
        catch
        {
            return "unknown";
        }
    }
}
