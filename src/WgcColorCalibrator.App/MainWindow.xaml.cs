using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using WinRT.Interop;
using WgcColorCalibrator.App.Pages;
using WgcColorCalibrator.App.Services;

namespace WgcColorCalibrator.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        WindowIconService.ApplyIcon(this);

        var resourceLoader = new ResourceLoader();
        Title = resourceLoader.GetString("MainWindowTitle");

        WindowHandle = WindowNative.GetWindowHandle(this);
        ContentFrame.Navigate(typeof(HomePage));
    }

    public nint WindowHandle { get; }

    public void NavigateTo(string tag)
    {
        NavigationViewItem? item = RootNavigation.MenuItems
            .OfType<NavigationViewItem>()
            .FirstOrDefault(i => string.Equals(i.Tag as string, tag, StringComparison.Ordinal));

        if (item is not null)
        {
            RootNavigation.SelectedItem = item;
        }

        NavigateByTag(tag);
    }

    private void NavigateByTag(string tag)
    {
        Type pageType = tag switch
        {
            "chart" => typeof(ChartPage),
            "measurement" => typeof(MeasurementPage),
            "settings" => typeof(SettingsPage),
            "diagnostics" => typeof(DiagnosticsPage),
            "about" => typeof(AboutPage),
            _ => typeof(HomePage)
        };

        if (ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        NavigateByTag(tag);
    }
}

