using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using WgcColorCalibrator.App.Rendering.Xaml;
using WgcColorCalibrator.App.Services;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace WgcColorCalibrator.App.Pages;

public sealed partial class ChartPage : Page
{
    private readonly ChartWorkspaceService _workspaceService;
    private readonly ResourceLoader _resourceLoader;
    private readonly XamlChartPreviewRenderer _previewRenderer;

    public ChartPage()
    {
        InitializeComponent();
        _workspaceService = App.Services.GetRequiredService<ChartWorkspaceService>();
        _resourceLoader = new ResourceLoader();
        _previewRenderer = new XamlChartPreviewRenderer();

        _workspaceService.StateChanged += OnWorkspaceStateChanged;
        ChartTypeComboBox.SelectedIndex = 0;
        OutputModeComboBox.SelectedIndex = 0;
        ToneMappingModeComboBox.SelectedIndex = 1;
        HdrBehaviorComboBox.SelectedIndex = 0;
        UpdateStatus();
    }

    private void OnWorkspaceStateChanged(object? sender, EventArgs e)
    {
        UpdateStatus();
        RenderPreview();
    }

    private void ChartTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        string tag = GetSelectedTag(ChartTypeComboBox) ?? "manual-single-color";
        ManualPanel.Visibility = tag == "manual-single-color" ? Visibility.Visible : Visibility.Collapsed;
        GrayscalePanel.Visibility = tag == "grayscale" ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ManualColorHexTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (HexColorParser.TryParseRgb8(ManualColorHexTextBox.Text, out Rgb8 color))
        {
            ManualRedNumberBox.Value = color.R;
            ManualGreenNumberBox.Value = color.G;
            ManualBlueNumberBox.Value = color.B;
            UpdateColorPreview(color);
        }
    }

    private void ManualColorNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        byte r = (byte)Math.Clamp((int)GetNumberBoxValue(ManualRedNumberBox, 255.0), 0, 255);
        byte g = (byte)Math.Clamp((int)GetNumberBoxValue(ManualGreenNumberBox, 255.0), 0, 255);
        byte b = (byte)Math.Clamp((int)GetNumberBoxValue(ManualBlueNumberBox, 255.0), 0, 255);
        var color = new Rgb8(r, g, b);
        ManualColorHexTextBox.Text = color.ToHexString();
        UpdateColorPreview(color);
    }

    private void UpdateColorPreview(Rgb8 color)
    {
        ManualColorPreview.Fill = new Microsoft.UI.Xaml.Media.SolidColorBrush(
            global::Windows.UI.Color.FromArgb(255, color.R, color.G, color.B));
        ManualColorNormalizedTextBlock.Text = $"{color.ToHexString()} RGB({color.R}, {color.G}, {color.B})";
    }

    private void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string providerId = GetSelectedTag(ChartTypeComboBox) ?? "manual-single-color";
            ChartLayoutDefinition layout = ReadLayoutDefinition();
            RenderOutputMode outputMode = ReadOutputMode();
            ToneMappingParameters toneMapping = ReadToneMappingParameters();

            Rgb8? manualColor = null;
            HdrColor? manualHdrColor = null;

            if (providerId == "manual-single-color")
            {
                if (outputMode == RenderOutputMode.HdrScRgb || outputMode == RenderOutputMode.Hdr10)
                {
                    // For HDR mode, interpret the manual input as scRGB linear values.
                    manualHdrColor = ReadHdrColorFromManualInputs();
                    manualColor = new Rgb8(255, 255, 255);
                }
                else
                {
                    if (!HexColorParser.TryParseRgb8(ManualColorHexTextBox.Text, out Rgb8 color))
                    {
                        ShowError(_resourceLoader.GetString("ValidationErrorInvalidHex"));
                        return;
                    }

                    manualColor = color;
                }
            }

            int grayscaleSteps = (int)GetNumberBoxValue(GrayscaleStepsNumberBox, 5.0);
            var options = new ChartGenerationOptions(
                manualColor,
                grayscaleSteps,
                layout,
                outputMode,
                toneMapping,
                manualHdrColor);

            _workspaceService.GenerateChart(providerId, options);
            RenderPreview();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenChartWindowButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _workspaceService.HdrUnsupportedBehavior = ReadHdrUnsupportedBehavior();
            _workspaceService.OpenChartWindow();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void CloseChartWindowButton_Click(object sender, RoutedEventArgs e)
    {
        _workspaceService.CloseChartWindow();
    }

    private void ToggleDebugOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        _workspaceService.ToggleDebugOverlay();
    }

    private async void ImportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            FileOpenPicker picker = new();
            InitializePicker(picker);
            picker.FileTypeFilter.Add(".json");
            StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            string json = await FileIO.ReadTextAsync(file);
            _workspaceService.ImportJson(json);
        }
        catch (Exception ex)
        {
            ShowError($"{_resourceLoader.GetString("ImportErrorTitle")}: {ex.Message}");
        }
    }

    private async void ImportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            FileOpenPicker picker = new();
            InitializePicker(picker);
            picker.FileTypeFilter.Add(".csv");
            StorageFile? file = await picker.PickSingleFileAsync();
            if (file is null)
            {
                return;
            }

            string csv = await FileIO.ReadTextAsync(file);
            string chartId = Path.GetFileNameWithoutExtension(file.Name);
            _workspaceService.ImportCsv(csv, chartId, chartId);
        }
        catch (Exception ex)
        {
            ShowError($"{_resourceLoader.GetString("ImportErrorTitle")}: {ex.Message}");
        }
    }

    private async void ExportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            FileSavePicker picker = new();
            InitializePicker(picker);
            picker.DefaultFileExtension = ".json";
            picker.FileTypeChoices.Add("JSON", [".json"]);
            StorageFile? file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                return;
            }

            string json = _workspaceService.ExportJson();
            await FileIO.WriteTextAsync(file, json);
        }
        catch (Exception ex)
        {
            ShowError($"{_resourceLoader.GetString("ExportErrorTitle")}: {ex.Message}");
        }
    }

    private async void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            FileSavePicker picker = new();
            InitializePicker(picker);
            picker.DefaultFileExtension = ".csv";
            picker.FileTypeChoices.Add("CSV", [".csv"]);
            StorageFile? file = await picker.PickSaveFileAsync();
            if (file is null)
            {
                return;
            }

            string csv = _workspaceService.ExportCsv();
            await FileIO.WriteTextAsync(file, csv);
        }
        catch (Exception ex)
        {
            ShowError($"{_resourceLoader.GetString("ExportErrorTitle")}: {ex.Message}");
        }
    }

    private void RenderPreview()
    {
        if (_workspaceService.CurrentChart is null || _workspaceService.CurrentPlacements is null)
        {
            return;
        }

        var options = new ChartRenderOptions(
            _workspaceService.CurrentChart,
            _workspaceService.CurrentPlacements,
            1.0,
            false,
            RenderOutputMode.SdrSrgb,
            ToneMappingParameters.Default);

        _previewRenderer.Render(
            _workspaceService.CurrentChart,
            _workspaceService.CurrentPlacements,
            options,
            PreviewCanvas);
    }

    private ChartLayoutDefinition ReadLayoutDefinition()
    {
        int patchWidth = (int)GetNumberBoxValue(PatchWidthNumberBox, 64.0);
        int patchHeight = (int)GetNumberBoxValue(PatchHeightNumberBox, 64.0);
        int gap = (int)GetNumberBoxValue(GapNumberBox, 8.0);
        int border = (int)GetNumberBoxValue(BorderNumberBox, 16.0);
        int safeSampleInset = (int)GetNumberBoxValue(SafeSampleInsetNumberBox, 8.0);
        int columnCount = (int)GetNumberBoxValue(ColumnCountNumberBox, 4.0);

        if (!HexColorParser.TryParseRgb8(BackgroundColorTextBox.Text, out Rgb8 background))
        {
            background = new Rgb8(0, 0, 0);
        }

        return new ChartLayoutDefinition(
            patchWidth,
            patchHeight,
            gap,
            border,
            safeSampleInset,
            columnCount,
            background);
    }

    private RenderOutputMode ReadOutputMode()
    {
        string tag = GetSelectedTag(OutputModeComboBox) ?? "SdrSrgb";
        return Enum.Parse<RenderOutputMode>(tag);
    }

    private HdrUnsupportedBehavior ReadHdrUnsupportedBehavior()
    {
        string tag = GetSelectedTag(HdrBehaviorComboBox) ?? "Cancel";
        return Enum.Parse<HdrUnsupportedBehavior>(tag);
    }

    private ToneMappingParameters ReadToneMappingParameters()
    {
        double paperWhite = GetNumberBoxValue(PaperWhiteNumberBox, 200.0);
        double peakBrightness = GetNumberBoxValue(PeakBrightnessNumberBox, 1000.0);
        double exposure = GetNumberBoxValue(ExposureNumberBox, 0.0);
        return new ToneMappingParameters(paperWhite, peakBrightness, exposure);
    }

    private HdrColor ReadHdrColorFromManualInputs()
    {
        double r = GetNumberBoxValue(ManualRedNumberBox, 255.0);
        double g = GetNumberBoxValue(ManualGreenNumberBox, 255.0);
        double b = GetNumberBoxValue(ManualBlueNumberBox, 255.0);
        var color = new HdrColor((float)r, (float)g, (float)b);
        if (!color.IsFinite || !color.IsNonNegative)
        {
            throw new InvalidOperationException(_resourceLoader.GetString("ValidationErrorInvalidHdrColor"));
        }

        return color;
    }

    private static double GetNumberBoxValue(NumberBox box, double defaultValue) =>
        double.IsNaN(box.Value) ? defaultValue : box.Value;

    private static string? GetSelectedTag(ComboBox comboBox)
    {
        if (comboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            return tag;
        }

        return null;
    }

    private static void InitializePicker(object picker)
    {
        // Packaged apps do not need an HWND for pickers.
        // This method is kept as a hook if unpackaged support is added later.
    }

    private void ShowError(string message)
    {
        StatusTextBlock.Text = $"{_resourceLoader.GetString("ValidationErrorTitle")}: {message}";
    }

    private void UpdateStatus()
    {
        ChartDefinition? chart = _workspaceService.CurrentChart;
        if (chart is null)
        {
            StatusTextBlock.Text = _resourceLoader.GetString("ChartStatusNoChart");
            return;
        }

        ChartRenderSession? session = _workspaceService.CurrentSession;
        if (session is null || session.DisplayOutput is null || session.DisplayOutput == DisplayOutputMetadata.Unknown)
        {
            StatusTextBlock.Text = $"{_resourceLoader.GetString("ChartStatusChart")}: {chart.Name}, {chart.Patches.Count} patches, {_workspaceService.CurrentOutputMode}";
            HdrStatusTextBlock.Text = _resourceLoader.GetString("HdrStatusNotProbed");
            return;
        }

        DisplayOutputMetadata metadata = session.DisplayOutput;
        string statusText = $"{_resourceLoader.GetString("ChartStatusChart")}: {chart.Name}, {chart.Patches.Count} patches, {session.ActualOutputMode}";
        string luminanceText = $"max={metadata.MaxLuminance:F1}, full={metadata.MaxFullFrameLuminance:F1}, min={metadata.MinLuminance:F2}";
        HdrStatusTextBlock.Text = metadata.HdrActive
            ? $"{_resourceLoader.GetString("HdrStatusActive")} ({metadata.DisplayName}, {luminanceText})"
            : $"{_resourceLoader.GetString("HdrStatusInactive")} ({metadata.DisplayName}, {luminanceText})";

        if (session.Warnings.Count > 0)
        {
            statusText += $" | {string.Join("; ", session.Warnings)}";
        }

        StatusTextBlock.Text = statusText;
    }
}
