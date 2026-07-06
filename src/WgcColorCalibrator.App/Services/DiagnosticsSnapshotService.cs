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
            WindowsAppSdkRelease: GetWindowsAppSdkRelease(),
            WindowsAppRuntimeVersion: GetWindowsAppRuntimeVersion(),
            WgcSupportStatus: ProbeStatus.NotProbed,
            HdrStatus: ProbeStatus.NotProbed,
            CapturePixelFormatStatus: ProbeStatus.NotSelected);
    }

    private static string GetApplicationVersion()
    {
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
        }

        var assembly = Assembly.GetEntryAssembly();
        var version = assembly?.GetName().Version;
        if (version is not null)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        return "unknown";
    }

    private static string GetWindowsAppSdkRelease()
    {
        try
        {
            return ReleaseInfo.AsString;
        }
        catch
        {
            return "unknown";
        }
    }

    private static string GetWindowsAppRuntimeVersion()
    {
        try
        {
            return RuntimeInfo.AsString;
        }
        catch
        {
            return "unknown";
        }
    }
}
