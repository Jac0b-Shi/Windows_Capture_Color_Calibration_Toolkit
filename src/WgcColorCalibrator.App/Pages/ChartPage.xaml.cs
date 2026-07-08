using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using WinRT.Interop;
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
    private readonly EventHandler _onWorkspaceStateChanged;
    private bool _uiEventsAttached;
    private bool _isDirty;
    private bool _isInitializing;

    public ChartPage()
    {
        _workspaceService = App.Services.GetRequiredService<ChartWorkspaceService>();
        _resourceLoader = new ResourceLoader();
        _previewRenderer = new XamlChartPreviewRenderer();
        _onWorkspaceStateChanged = OnWorkspaceStateChanged;

        InitializeComponent();

        // Attach events after InitializeComponent so all x:Name fields are assigned.
        // XAML event bindings would fire during initialization while sibling controls are still null.
        AttachUiEvents();
        InitializeUiState();

        _workspaceService.StateChanged += _onWorkspaceStateChanged;
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        _workspaceService.StateChanged -= _onWorkspaceStateChanged;
    }

    private void AttachUiEvents()
    {
        if (_uiEventsAttached)
        {
            return;
        }

        ChartTypeComboBox.SelectionChanged += ChartTypeComboBox_SelectionChanged;
        OutputModeComboBox.SelectionChanged += OutputModeComboBox_SelectionChanged;
        ToneMappingModeComboBox.SelectionChanged += (s, e) => SetDirty();
        HdrBehaviorComboBox.SelectionChanged += (s, e) => SetDirty();
        PatchSizePresetComboBox.SelectionChanged += PatchSizePresetComboBox_SelectionChanged;

        ManualRedNumberBox.ValueChanged += ManualColorNumberBox_ValueChanged;
        ManualGreenNumberBox.ValueChanged += ManualColorNumberBox_ValueChanged;
        ManualBlueNumberBox.ValueChanged += ManualColorNumberBox_ValueChanged;
        ManualHdrRedNumberBox.ValueChanged += ManualHdrColorNumberBox_ValueChanged;
        ManualHdrGreenNumberBox.ValueChanged += ManualHdrColorNumberBox_ValueChanged;
        ManualHdrBlueNumberBox.ValueChanged += ManualHdrColorNumberBox_ValueChanged;

        PatchWidthNumberBox.ValueChanged += (s, e) => SetDirty();
        PatchHeightNumberBox.ValueChanged += (s, e) => SetDirty();
        GapNumberBox.ValueChanged += (s, e) => SetDirty();
        BorderNumberBox.ValueChanged += (s, e) => SetDirty();
        SafeSampleInsetNumberBox.ValueChanged += (s, e) => SetDirty();
        ColumnCountNumberBox.ValueChanged += (s, e) => SetDirty();
        BackgroundColorTextBox.LostFocus += (s, e) => SetDirty();
        PaperWhiteNumberBox.ValueChanged += (s, e) => SetDirty();
        PeakBrightnessNumberBox.ValueChanged += (s, e) => SetDirty();
        ExposureNumberBox.ValueChanged += (s, e) => SetDirty();
        GrayscaleStepsNumberBox.ValueChanged += (s, e) => SetDirty();

        _uiEventsAttached = true;
    }

    private void InitializeUiState()
    {
        _isInitializing = true;
        ChartTypeComboBox.SelectedIndex = 0;
        OutputModeComboBox.SelectedIndex = 0;
        ToneMappingModeComboBox.SelectedIndex = 1;
        HdrBehaviorComboBox.SelectedIndex = 0;
        PatchSizePresetComboBox.SelectedIndex = 1;

        UpdateManualPanelVisibility();
        UpdateManualColorText();
        UpdateManualHdrColorText();
        _isInitializing = false;
        _isDirty = false;
        UpdateStatus();
    }

    private void SetDirty()
    {
        if (_isInitializing)
        {
            return;
        }

        _isDirty = true;
        UpdateStatus();
    }

    private void OnWorkspaceStateChanged(object? sender, EventArgs e)
    {
        UpdateStatus();
        RenderPreview();
    }

    private void ChartTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateManualPanelVisibility();
        string tag = GetSelectedTag(ChartTypeComboBox) ?? "manual-single-color";
        GrayscalePanel.Visibility = tag == "grayscale" ? Visibility.Visible : Visibility.Collapsed;
        SetDirty();
    }

    private void OutputModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateManualPanelVisibility();
        _workspaceService.CurrentOutputMode = ReadOutputMode();
        SetDirty();
    }

    private void PatchSizePresetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PatchSizePresetComboBox.SelectedItem is ComboBoxItem item &&
            item.Tag is string tag &&
            int.TryParse(tag, out int size))
        {
            PatchWidthNumberBox.Value = size;
            PatchHeightNumberBox.Value = size;
        }

        SetDirty();
    }

    private void UpdateManualPanelVisibility()
    {
        string chartType = GetSelectedTag(ChartTypeComboBox) ?? "manual-single-color";
        RenderOutputMode outputMode = ReadOutputMode();
        bool isManualSingleColor = chartType == "manual-single-color";
        bool isHdr = outputMode == RenderOutputMode.HdrScRgb || outputMode == RenderOutputMode.Hdr10;

        SdrManualPanel.Visibility = isManualSingleColor && !isHdr ? Visibility.Visible : Visibility.Collapsed;
        HdrManualPanel.Visibility = isManualSingleColor && isHdr ? Visibility.Visible : Visibility.Collapsed;
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

        SetDirty();
    }

    private void ManualColorNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        UpdateManualColorText();
        SetDirty();
    }

    private void UpdateManualColorText()
    {
        byte r = (byte)Math.Clamp((int)GetNumberBoxValue(ManualRedNumberBox, 255.0), 0, 255);
        byte g = (byte)Math.Clamp((int)GetNumberBoxValue(ManualGreenNumberBox, 255.0), 0, 255);
        byte b = (byte)Math.Clamp((int)GetNumberBoxValue(ManualBlueNumberBox, 255.0), 0, 255);
        var color = new Rgb8(r, g, b);
        ManualColorHexTextBox.Text = color.ToHexString();
        ManualColorNormalizedTextBlock.Text = $"{color.ToHexString()} RGB({color.R}, {color.G}, {color.B})";
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
            (string providerId, ChartGenerationOptions options)? result = GenerateChartFromUi();
            if (result is null)
            {
                return;
            }

            _workspaceService.GenerateChart(result.Value.providerId, result.Value.options);
            _isDirty = false;
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
            if (_isDirty)
            {
                (string providerId, ChartGenerationOptions options)? result = GenerateChartFromUi();
                if (result is null)
                {
                    return;
                }

                _workspaceService.GenerateChart(result.Value.providerId, result.Value.options);
                _isDirty = false;
            }

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

    private (string providerId, ChartGenerationOptions options)? GenerateChartFromUi()
    {
        string providerId = GetSelectedTag(ChartTypeComboBox) ?? "manual-single-color";
        ChartLayoutDefinition layout = ReadLayoutDefinition();
        RenderOutputMode outputMode = ReadOutputMode();
        ToneMappingParameters toneMapping = ReadToneMappingParameters();
        ToneMappingMode toneMappingMode = ReadToneMappingMode();

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
                    return null;
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
            manualHdrColor,
            toneMappingMode);

        return (providerId, options);
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
            _isDirty = false;
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
            _isDirty = false;
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

    private ToneMappingMode ReadToneMappingMode()
    {
        string tag = GetSelectedTag(ToneMappingModeComboBox) ?? "DirectScRgb";
        return Enum.Parse<ToneMappingMode>(tag);
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
        double r = GetNumberBoxValue(ManualHdrRedNumberBox, 1.0);
        double g = GetNumberBoxValue(ManualHdrGreenNumberBox, 1.0);
        double b = GetNumberBoxValue(ManualHdrBlueNumberBox, 1.0);
        var color = new HdrColor((float)r, (float)g, (float)b);
        if (!color.IsFinite || !color.IsNonNegative)
        {
            throw new InvalidOperationException(_resourceLoader.GetString("ValidationErrorInvalidHdrColor"));
        }

        return color;
    }

    private void ManualHdrColorNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        UpdateManualHdrColorText();
        SetDirty();
    }

    private void UpdateManualHdrColorText()
    {
        double r = GetNumberBoxValue(ManualHdrRedNumberBox, 1.0);
        double g = GetNumberBoxValue(ManualHdrGreenNumberBox, 1.0);
        double b = GetNumberBoxValue(ManualHdrBlueNumberBox, 1.0);
        ManualHdrColorNormalizedTextBlock.Text = FormattableString.Invariant($"R={r:G6} G={g:G6} B={b:G6}");
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
        nint hwnd = App.Services.GetRequiredService<MainWindow>().WindowHandle;
        InitializeWithWindow.Initialize(picker, hwnd);
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
            StatusTextBlock.Text = _isDirty
                ? _resourceLoader.GetString("ChartStatusParametersDirty")
                : _resourceLoader.GetString("ChartStatusNoChart");
            return;
        }

        ChartRenderSession? session = _workspaceService.CurrentSession;
        if (session is null || session.DisplayOutput is null || session.DisplayOutput == DisplayOutputMetadata.Unknown)
        {
            if (_isDirty)
            {
                StatusTextBlock.Text = _resourceLoader.GetString("ChartStatusParametersDirty");
            }
            else
            {
                StatusTextBlock.Text = $"{_resourceLoader.GetString("ChartStatusChart")}: {chart.Name}, {chart.Patches.Count} patches, {_workspaceService.CurrentOutputMode}";
            }

            HdrStatusTextBlock.Text = _resourceLoader.GetString("HdrStatusNotProbed");
            return;
        }

        DisplayOutputMetadata metadata = session.DisplayOutput;
        string statusText = $"{_resourceLoader.GetString("ChartStatusChart")}: {chart.Name}, {chart.Patches.Count} patches, {session.ActualOutputMode}";
        string luminanceText = $"max={metadata.MaxLuminance:F1}, full={metadata.MaxFullFrameLuminance:F1}, min={metadata.MinLuminance:F2}";

        string hdrStatusKey = metadata.HdrCapabilityKnown
            ? (metadata.HdrActive ? "HdrStatusActive" : "HdrStatusInactive")
            : "HdrStatusUnknown";
        HdrStatusTextBlock.Text = $"{_resourceLoader.GetString(hdrStatusKey)} ({metadata.DisplayName}, {luminanceText})";

        if (session.Warnings.Count > 0)
        {
            statusText += $" | {string.Join("; ", session.Warnings)}";
        }

        if (_isDirty)
        {
            statusText += $" | {_resourceLoader.GetString("ChartStatusParametersDirty")}";
        }

        StatusTextBlock.Text = statusText;
    }
}
