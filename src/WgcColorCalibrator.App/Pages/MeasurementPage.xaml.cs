using System.IO.Compression;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using Windows.Storage;
using WgcColorCalibrator.App.Services;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Measurements;

namespace WgcColorCalibrator.App.Pages;

public sealed record MeasurementRecordViewModel(
    string PatchId,
    string ExpectedText,
    string CapturedText,
    string MeanText,
    string MedianText,
    string StdDevText,
    string DeltaText,
    string RelativeErrorText,
    string ValidityText,
    string WarningsText);

public sealed partial class MeasurementPage : Page
{
    private readonly MeasurementService _measurementService;
    private readonly ProfileJsonSerializerService _serializer;
    private readonly MeasurementDebugOverlayService _overlayService;
    private readonly MeasurementOperatorComparisonExportService _operatorComparisonExportService;
    private readonly ResourceLoader _resourceLoader;
    private readonly EventHandler _onMeasurementServiceStateChanged;
    private bool _measurementServiceEventsAttached;
    private bool _isFilePickerOpen;
    private bool _isExporting;

    public MeasurementPage()
    {
        this.InitializeComponent();
        _measurementService = App.Services.GetRequiredService<MeasurementService>();
        _serializer = App.Services.GetRequiredService<ProfileJsonSerializerService>();
        _overlayService = App.Services.GetRequiredService<MeasurementDebugOverlayService>();
        _operatorComparisonExportService = App.Services.GetRequiredService<MeasurementOperatorComparisonExportService>();
        _resourceLoader = new ResourceLoader();
        _onMeasurementServiceStateChanged = OnMeasurementServiceStateChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        InitializeCaptureFormatSelection();
        Refresh();
    }

    private void InitializeCaptureFormatSelection()
    {
        CaptureFormatComboBox.SelectedItem = _measurementService.SelectedCapturePixelFormat switch
        {
            CapturePixelFormat.R16G16B16A16Float => CaptureFormatComboBox.Items.OfType<ComboBoxItem>().First(i => i.Tag as string == "r16g16b16a16-float"),
            _ => CaptureFormatComboBox.Items.OfType<ComboBoxItem>().First(i => i.Tag as string == "b8g8r8a8-uint-normalized")
        };
    }

    private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CaptureFormatComboBox.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _measurementService.SelectedCapturePixelFormat = tag switch
            {
                "r16g16b16a16-float" => CapturePixelFormat.R16G16B16A16Float,
                _ => CapturePixelFormat.B8G8R8A8UIntNormalized
            };
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (!_measurementServiceEventsAttached)
        {
            _measurementService.StateChanged += _onMeasurementServiceStateChanged;
            _measurementServiceEventsAttached = true;
        }

        Refresh();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);

        if (_measurementServiceEventsAttached)
        {
            _measurementService.StateChanged -= _onMeasurementServiceStateChanged;
            _measurementServiceEventsAttached = false;
        }
    }

    private void OnMeasurementServiceStateChanged(object? sender, EventArgs e)
    {
        _ = DispatcherQueue.TryEnqueue(() => Refresh());
    }

    private void Refresh()
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        CapturedFrame? frame = _measurementService.CurrentFrame;
        if (session is null || frame is null)
        {
            StatusInfoBar.IsOpen = false;
            SummaryTextBlock.Text = string.Empty;
            RecordsGridView.ItemsSource = null;
            ExportJsonMenuItem.IsEnabled = false;
            ExportCsvMenuItem.IsEnabled = false;
            ExportRawMenuItem.IsEnabled = false;
            ExportDebugOverlayMenuItem.IsEnabled = false;
            ExportLuminanceOverlayMenuItem.IsEnabled = false;
            ExportBuiltInOperatorComparisonMenuItem.IsEnabled = false;
            CustomOperatorMenuItem.IsEnabled = false;
            return;
        }

        CaptureGeometry? geometry = session.CaptureGeometry;
        string geometryStatus = geometry?.MappingStatus == CaptureMappingStatus.Verified ? "Verified" : "Unverified";
        string overlayNoteKey = frame.PixelFormat == CapturePixelFormat.R16G16B16A16Float
            ? "MeasurementFp16OverlayNote"
            : "MeasurementBgraOverlayNote";
        SummaryTextBlock.Text =
            $"Validity: {session.Validity}\n" +
            $"Geometry: {geometryStatus}\n" +
            $"Format: {session.Capture.ActualPixelFormat}\n" +
            $"Patches: {session.Measurements.Count}\n" +
            $"Warnings: {string.Join(", ", session.Warnings)}\n" +
            _resourceLoader.GetString(overlayNoteKey);

        StatusInfoBar.Message = session.Validity == MeasurementSessionValidity.Valid
            ? "Measurement valid"
            : "Diagnostic only";
        StatusInfoBar.Severity = session.Validity == MeasurementSessionValidity.Valid
            ? InfoBarSeverity.Success
            : InfoBarSeverity.Warning;
        StatusInfoBar.IsOpen = true;

        RecordsGridView.ItemsSource = session.Measurements.Select(ToViewModel).ToList();
        ExportJsonMenuItem.IsEnabled = true;
        ExportCsvMenuItem.IsEnabled = true;
        ExportRawMenuItem.IsEnabled = true;
        string exportRawLabelKey = frame.PixelFormat == CapturePixelFormat.R16G16B16A16Float
            ? "ExportRawRgba16fLabel"
            : "ExportRawBgra8Label";
        ExportRawMenuItem.Text = _resourceLoader.GetString(exportRawLabelKey);

        bool isBgra8 = frame.PixelFormat == CapturePixelFormat.B8G8R8A8UIntNormalized;
        ExportDebugOverlayMenuItem.IsEnabled = isBgra8;
        ExportDebugOverlayMenuItem.Visibility = isBgra8 ? Visibility.Visible : Visibility.Collapsed;
        ExportLuminanceOverlayMenuItem.IsEnabled = !isBgra8;
        ExportLuminanceOverlayMenuItem.Visibility = isBgra8 ? Visibility.Collapsed : Visibility.Visible;

        bool isFp16 = frame.PixelFormat == CapturePixelFormat.R16G16B16A16Float;
        ExportBuiltInOperatorComparisonMenuItem.IsEnabled = isFp16;
        ToolTipService.SetToolTip(
            ExportBuiltInOperatorComparisonMenuItem,
            isFp16 ? null : _resourceLoader.GetString("OperatorComparisonMenuItemToolTip"));

        CustomOperatorMenuItem.IsEnabled = false;
        ToolTipService.SetToolTip(
            CustomOperatorMenuItem,
            _resourceLoader.GetString("CustomOperatorMenuItemToolTip"));
    }

    private static MeasurementRecordViewModel ToViewModel(MeasurementRecord record)
    {
        string expected = FormatColorValue(record.Expected);
        string captured = FormatColorValue(record.Captured);
        string mean = FormatChannelStatistics(record.ChannelStatistics, s => s.Mean);
        string median = FormatChannelStatistics(record.ChannelStatistics, s => s.Median);
        string stdDev = FormatChannelStatistics(record.ChannelStatistics, s => s.StandardDeviation);
        string validity = record.Validity.ToString();
        (string delta, string relativeError) = ComputeDeltaAndRelativeError(record);
        return new MeasurementRecordViewModel(
            record.PatchId,
            expected,
            captured,
            mean,
            median,
            stdDev,
            delta,
            relativeError,
            validity,
            string.Join(", ", record.Warnings));
    }

    private static string FormatChannelStatistics(ChannelStatistics? statistics, Func<ChannelStatistic, double> selector)
    {
        if (statistics is null)
        {
            return "-";
        }

        return $"R{selector(statistics.R):F2} G{selector(statistics.G):F2} B{selector(statistics.B):F2}";
    }

    private static string FormatColorValue(ColorValue color)
    {
        if (color.Rgb8.HasValue)
        {
            Rgb8 rgb = color.Rgb8.Value;
            return $"R{rgb.R} G{rgb.G} B{rgb.B}";
        }

        if (color.Rgba.HasValue)
        {
            RgbaFloat rgba = color.Rgba.Value;
            return $"R{rgba.R:F4} G{rgba.G:F4} B{rgba.B:F4}";
        }

        return color.Encoding.ToString();
    }

    private static (string Delta, string RelativeError) ComputeDeltaAndRelativeError(MeasurementRecord record)
    {
        (float? expectedR, float? expectedG, float? expectedB) = ToFloatChannels(record.Expected);
        (float? capturedR, float? capturedG, float? capturedB) = ToFloatChannels(record.Captured);

        if (!expectedR.HasValue || !capturedR.HasValue)
        {
            return ("-", "-");
        }

        float dr = capturedR.Value - expectedR.Value;
        float dg = capturedG!.Value - expectedG!.Value;
        float db = capturedB!.Value - expectedB!.Value;
        string delta = $"ΔR{dr:F4} G{dg:F4} B{db:F4}";

        string relativeError = FormatRelativeError(expectedR.Value, expectedG.Value, expectedB.Value, dr, dg, db);

        return (delta, relativeError);
    }

    private static string FormatRelativeError(float eR, float eG, float eB, float dR, float dG, float dB)
    {
        bool allExpectedZero = eR == 0.0f && eG == 0.0f && eB == 0.0f;
        if (allExpectedZero)
        {
            bool allDeltaZero = dR == 0.0f && dG == 0.0f && dB == 0.0f;
            return allDeltaZero ? "0.00%" : "-";
        }

        double sum = 0.0;
        int count = 0;

        if (eR != 0.0f)
        {
            sum += Math.Abs(dR / eR);
            count++;
        }

        if (eG != 0.0f)
        {
            sum += Math.Abs(dG / eG);
            count++;
        }

        if (eB != 0.0f)
        {
            sum += Math.Abs(dB / eB);
            count++;
        }

        return count > 0 ? $"{sum / count:P2}" : "-";
    }

    private static (float? R, float? G, float? B) ToFloatChannels(ColorValue color)
    {
        if (color.Rgba.HasValue)
        {
            RgbaFloat rgba = color.Rgba.Value;
            return (rgba.R, rgba.G, rgba.B);
        }

        if (color.Rgb8.HasValue)
        {
            Rgb8 rgb = color.Rgb8.Value;
            return (rgb.R / 255.0f, rgb.G / 255.0f, rgb.B / 255.0f);
        }

        return (null, null, null);
    }

    private void RefreshExportMenuButtonEnabled()
    {
        ExportMenuButton.IsEnabled = !(_isFilePickerOpen || _isExporting);
    }

    private async Task<global::Windows.Storage.StorageFile?> PickSaveFileAsync(
        string suggestedFileName,
        string fileTypeName,
        string extension)
    {
        if (_isFilePickerOpen)
        {
            return null;
        }

        _isFilePickerOpen = true;
        RefreshExportMenuButtonEnabled();

        try
        {
            if (ExportMenuButton.Flyout is MenuFlyout flyout)
            {
                flyout.Hide();
            }

            await Task.Yield();

            var picker = new global::Windows.Storage.Pickers.FileSavePicker();
            picker.SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
            picker.SuggestedFileName = suggestedFileName;
            picker.FileTypeChoices.Add(fileTypeName, new[] { extension });

            WinRT.Interop.InitializeWithWindow.Initialize(picker, ((App)App.Current).WindowHandle);
            return await picker.PickSaveFileAsync();
        }
        finally
        {
            _isFilePickerOpen = false;
            RefreshExportMenuButtonEnabled();
            await Task.Yield();
            ((App)App.Current).MainWindow?.Activate();
            WindowRecoveryService.RecoverIfDisabled(((App)App.Current).WindowHandle);
        }
    }

    private async Task RunExportAsync(Func<Task<bool>> export, string successResourceKey)
    {
        _isExporting = true;
        RefreshExportMenuButtonEnabled();
        ShowExportInfo("Exporting");

        try
        {
            bool completed = await export();
            if (completed)
            {
                ShowExportSuccess(successResourceKey);
            }
            else
            {
                StatusInfoBar.IsOpen = false;
            }
        }
        catch (Exception ex)
        {
            ShowExportError(ex);
        }
        finally
        {
            _isExporting = false;
            RefreshExportMenuButtonEnabled();
        }
    }

    private async Task RunExportAsync(Func<Task<bool>> export)
    {
        await RunExportAsync(export, "ExportCompleted");
    }

    private void ShowExportInfo(string resourceKey)
    {
        StatusInfoBar.Title = _resourceLoader.GetString(resourceKey);
        StatusInfoBar.Message = string.Empty;
        StatusInfoBar.Severity = InfoBarSeverity.Informational;
        StatusInfoBar.IsOpen = true;
    }

    private void ShowExportSuccess(string resourceKey)
    {
        StatusInfoBar.Title = _resourceLoader.GetString(resourceKey);
        StatusInfoBar.Message = string.Empty;
        StatusInfoBar.Severity = InfoBarSeverity.Success;
        StatusInfoBar.IsOpen = true;
    }

    private void ShowExportError(Exception ex)
    {
        StatusInfoBar.Title = _resourceLoader.GetString("ExportErrorTitle");
        StatusInfoBar.Message = string.Format(
            System.Globalization.CultureInfo.CurrentCulture,
            _resourceLoader.GetString("ExportFailed"),
            ex.Message);
        StatusInfoBar.Severity = InfoBarSeverity.Error;
        StatusInfoBar.IsOpen = true;
    }

    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        CaptureButton.IsEnabled = false;
        StatusInfoBar.IsOpen = false;

        try
        {
            await _measurementService.CaptureAsync();
        }
        catch (Exception ex)
        {
            StatusInfoBar.Title = "Capture failed";
            StatusInfoBar.Message = ex.Message;
            StatusInfoBar.Severity = InfoBarSeverity.Error;
            StatusInfoBar.IsOpen = true;
        }
        finally
        {
            CaptureButton.IsEnabled = true;
        }
    }

    private async void ExportJsonButton_Click(object sender, RoutedEventArgs e)
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        if (session is null)
        {
            return;
        }

        await RunExportAsync(async () =>
        {
            global::Windows.Storage.StorageFile? file = await PickSaveFileAsync(
                $"measurement-{session.CreatedAt:yyyyMMdd-HHmmss}",
                "JSON",
                ".json");
            if (file is null)
            {
                return false;
            }

            string json = _serializer.SerializeMeasurement(session);
            await global::Windows.Storage.FileIO.WriteTextAsync(file, json);
            return true;
        });
    }

    private async void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        if (session is null)
        {
            return;
        }

        await RunExportAsync(async () =>
        {
            global::Windows.Storage.StorageFile? file = await PickSaveFileAsync(
                $"measurement-{session.CreatedAt:yyyyMMdd-HHmmss}",
                "CSV",
                ".csv");
            if (file is null)
            {
                return false;
            }

            string csv = _measurementService.ExportCurrentSessionAsCsv();
            await global::Windows.Storage.FileIO.WriteTextAsync(file, csv);
            return true;
        });
    }

    private async void ExportRawButton_Click(object sender, RoutedEventArgs e)
    {
        byte[]? pixels = _measurementService.ExportCurrentFrameRawBytes();
        CapturedFrame? frame = _measurementService.CurrentFrame;
        if (pixels is null || frame is null)
        {
            return;
        }

        (string fileTypeName, string extension) = frame.PixelFormat switch
        {
            CapturePixelFormat.R16G16B16A16Float => ("FP16 RGBA", ".rgba16f"),
            _ => ("BGRA", ".bgra")
        };

        MeasurementSession? session = _measurementService.CurrentSession;
        string suggestedFileName = session is not null
            ? $"capture-{session.CreatedAt:yyyyMMdd-HHmmss}"
            : "capture-frame";

        await RunExportAsync(async () =>
        {
            global::Windows.Storage.StorageFile? file = await PickSaveFileAsync(
                suggestedFileName,
                fileTypeName,
                extension);
            if (file is null)
            {
                return false;
            }

            await global::Windows.Storage.FileIO.WriteBytesAsync(file, pixels);
            return true;
        });
    }

    private async void ExportDebugOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        CapturedFrame? frame = _measurementService.CurrentFrame;
        if (session is null || frame is null || session.CaptureGeometry is null)
        {
            return;
        }

        if (frame.PixelFormat != CapturePixelFormat.B8G8R8A8UIntNormalized)
        {
            ShowExportError(new InvalidOperationException(_resourceLoader.GetString("Bgra8OverlayFormatError")));
            return;
        }

        await RunExportAsync(async () =>
        {
            global::Windows.Storage.StorageFile? file = await PickSaveFileAsync(
                $"overlay-{session.CreatedAt:yyyyMMdd-HHmmss}",
                "PNG",
                ".png");
            if (file is null)
            {
                return false;
            }

            await _overlayService.SaveDebugOverlayAsync(
                frame,
                session.CaptureGeometry,
                session.Layout,
                file);
            return true;
        });
    }

    private async void ExportLuminanceOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        CapturedFrame? frame = _measurementService.CurrentFrame;
        if (session is null || frame is null || session.CaptureGeometry is null)
        {
            return;
        }

        if (frame.PixelFormat != CapturePixelFormat.R16G16B16A16Float)
        {
            ShowExportError(new InvalidOperationException(_resourceLoader.GetString("Fp16LuminanceFormatError")));
            return;
        }

        await RunExportAsync(async () =>
        {
            global::Windows.Storage.StorageFile? file = await PickSaveFileAsync(
                $"overlay-luminance-{session.CreatedAt:yyyyMMdd-HHmmss}",
                "PNG",
                ".png");
            if (file is null)
            {
                return false;
            }

            await _overlayService.SaveDebugOverlayAsync(
                frame,
                session.CaptureGeometry,
                session.Layout,
                file);
            return true;
        });
    }

    private async void ExportOperatorComparisonButton_Click(object sender, RoutedEventArgs e)
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        if (session is null)
        {
            return;
        }

        _isExporting = true;
        RefreshExportMenuButtonEnabled();
        ShowExportInfo("Exporting");

        try
        {
            StorageFile? file = await PickSaveFileAsync(
                $"operator-comparison-{session.CreatedAt:yyyyMMdd-HHmmss}",
                "ZIP",
                ".zip");
            if (file is null)
            {
                StatusInfoBar.IsOpen = false;
                return;
            }

            string tempFolderName = $"operator-comparison-{session.CreatedAt:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}";
            StorageFolder tempFolder = await ApplicationData.Current.TemporaryFolder.CreateFolderAsync(
                tempFolderName,
                CreationCollisionOption.GenerateUniqueName);

            try
            {
                OperatorComparisonExportResult result = await _operatorComparisonExportService.ExportAsync(session, tempFolder, session.CreatedAt);
                ZipFile.CreateFromDirectory(tempFolder.Path, file.Path, CompressionLevel.Optimal, includeBaseDirectory: true);

                int csvCount = result.FileNames.Count - result.OperatorCount;
                StatusInfoBar.Title = _resourceLoader.GetString("OperatorComparisonPackageExported");
                StatusInfoBar.Message = string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    _resourceLoader.GetString("OperatorComparisonPackageExportedMessage"),
                    result.FileNames.Count,
                    csvCount,
                    result.OperatorCount);
                StatusInfoBar.Severity = InfoBarSeverity.Success;
                StatusInfoBar.IsOpen = true;
            }
            finally
            {
                await tempFolder.DeleteAsync(StorageDeleteOption.PermanentDelete);
            }
        }
        catch (Exception ex)
        {
            ShowExportError(ex);
        }
        finally
        {
            _isExporting = false;
            RefreshExportMenuButtonEnabled();
        }
    }

    private void BackToChartButton_Click(object sender, RoutedEventArgs e)
    {
        ((App)App.Current).MainWindow?.NavigateTo("chart");
    }
}
