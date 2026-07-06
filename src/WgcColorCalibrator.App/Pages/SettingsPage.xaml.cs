using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WgcColorCalibrator.App.Services;
using Windows.ApplicationModel.Resources;

namespace WgcColorCalibrator.App.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly LanguageService languageService;

    public SettingsPage()
    {
        InitializeComponent();
        languageService = App.Services.GetRequiredService<LanguageService>();
        SelectCurrentLanguage();
    }

    private async void ApplyLanguage_Click(object sender, RoutedEventArgs e)
    {
        if (LanguageComboBox.SelectedItem is ComboBoxItem item && item.Tag is string languageTag)
        {
            string previousTag = languageService.CurrentLanguageTag;

            languageService.SetLanguageOverride(languageTag);
            LanguageStatusTextBlock.Text = languageService.CurrentLanguageTag;

            if (!string.Equals(languageTag, previousTag, StringComparison.Ordinal))
            {
                var loader = ResourceLoader.GetForViewIndependentUse();

                ContentDialog dialog = new()
                {
                    Title = loader.GetString("SettingsLanguageRestartTitle"),
                    Content = loader.GetString("SettingsLanguageRestartContent"),
                    PrimaryButtonText = loader.GetString("SettingsLanguageRestartExitButton"),
                    CloseButtonText = loader.GetString("SettingsLanguageRestartLaterButton"),
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                ContentDialogResult result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    Application.Current.Exit();
                }
            }
        }
    }

    private void SelectCurrentLanguage()
    {
        string currentLanguage = languageService.CurrentLanguageTag;
        foreach (object item in LanguageComboBox.Items)
        {
            if (item is ComboBoxItem comboBoxItem && string.Equals(comboBoxItem.Tag as string, currentLanguage, StringComparison.Ordinal))
            {
                LanguageComboBox.SelectedItem = comboBoxItem;
                break;
            }
        }

        LanguageStatusTextBlock.Text = currentLanguage;
    }
}
