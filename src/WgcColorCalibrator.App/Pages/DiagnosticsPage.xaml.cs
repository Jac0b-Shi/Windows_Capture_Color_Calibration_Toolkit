using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using WgcColorCalibrator.App.Models;
using WgcColorCalibrator.App.Services;

namespace WgcColorCalibrator.App.Pages;

public sealed partial class DiagnosticsPage : Page
{
    public DiagnosticsPage()
    {
        InitializeComponent();
        var snapshot = App.Services.GetRequiredService<DiagnosticsSnapshotService>().CreateSnapshot();

        AppVersionTextBlock.Text = snapshot.ApplicationVersion;
        DotNetTextBlock.Text = snapshot.DotNetVersion;
        WindowsTextBlock.Text = snapshot.WindowsVersion;
        WindowsAppSdkTextBlock.Text = snapshot.WindowsAppSdkRelease;
        WindowsAppRuntimeTextBlock.Text = snapshot.WindowsAppRuntimeVersion;
        WgcTextBlock.Text = GetProbeStatusText(snapshot.WgcSupportStatus);
        HdrTextBlock.Text = GetProbeStatusText(snapshot.HdrStatus);
        PixelFormatTextBlock.Text = GetProbeStatusText(snapshot.CapturePixelFormatStatus);
    }

    private static string GetProbeStatusText(ProbeStatus status)
    {
        var loader = new ResourceLoader();
        string key = status switch
        {
            ProbeStatus.NotProbed => "ProbeStatusNotProbed",
            ProbeStatus.NotSelected => "ProbeStatusNotSelected",
            ProbeStatus.Supported => "ProbeStatusSupported",
            ProbeStatus.Unsupported => "ProbeStatusUnsupported",
            ProbeStatus.Error => "ProbeStatusError",
            _ => "ProbeStatusNotProbed"
        };

        return loader.GetString(key);
    }
}
