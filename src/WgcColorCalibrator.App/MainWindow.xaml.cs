using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using WgcColorCalibrator.App.Pages;

namespace WgcColorCalibrator.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var resourceLoader = new ResourceLoader();
        Title = resourceLoader.GetString("MainWindowTitle");

        ContentFrame.Navigate(typeof(HomePage));
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is not NavigationViewItem item || item.Tag is not string tag)
        {
            return;
        }

        Type pageType = tag switch
        {
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
}

