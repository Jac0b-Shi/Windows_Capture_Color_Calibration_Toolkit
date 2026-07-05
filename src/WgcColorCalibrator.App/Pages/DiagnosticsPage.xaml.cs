using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
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
        WindowsAppSdkTextBlock.Text = snapshot.WindowsAppSdkPackageVersion;
        WgcTextBlock.Text = snapshot.WgcSupportStatus;
        HdrTextBlock.Text = snapshot.HdrStatus;
        PixelFormatTextBlock.Text = snapshot.CapturePixelFormatStatus;
    }
}
