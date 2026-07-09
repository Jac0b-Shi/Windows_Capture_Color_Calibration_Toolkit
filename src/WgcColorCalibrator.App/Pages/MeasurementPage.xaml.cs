using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    string ValidityText,
    string WarningsText);

public sealed partial class MeasurementPage : Page
{
    private readonly MeasurementService _measurementService;
    private readonly ProfileJsonSerializerService _serializer;
    private readonly MeasurementDebugOverlayService _overlayService;

    public MeasurementPage()
    {
        this.InitializeComponent();
        _measurementService = App.Services.GetRequiredService<MeasurementService>();
        _serializer = App.Services.GetRequiredService<ProfileJsonSerializerService>();
        _overlayService = App.Services.GetRequiredService<MeasurementDebugOverlayService>();
        _measurementService.StateChanged += OnMeasurementServiceStateChanged;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Refresh();
    }

    private void OnMeasurementServiceStateChanged(object? sender, EventArgs e)
    {
        _ = DispatcherQueue.TryEnqueue(() => Refresh());
    }

    private void Refresh()
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        if (session is null)
        {
            StatusInfoBar.IsOpen = false;
            SummaryTextBlock.Text = string.Empty;
            RecordsGridView.ItemsSource = null;
            ExportJsonButton.IsEnabled = false;
            return;
        }

        CaptureGeometry? geometry = session.CaptureGeometry;
        string geometryStatus = geometry?.MappingStatus == CaptureMappingStatus.Verified ? "Verified" : "Unverified";
        SummaryTextBlock.Text =
            $"Validity: {session.Validity}\n" +
            $"Geometry: {geometryStatus}\n" +
            $"Patches: {session.Measurements.Count}\n" +
            $"Warnings: {string.Join(", ", session.Warnings)}";

        StatusInfoBar.Message = session.Validity == MeasurementSessionValidity.Valid
            ? "Measurement valid"
            : "Diagnostic only";
        StatusInfoBar.Severity = session.Validity == MeasurementSessionValidity.Valid
            ? InfoBarSeverity.Success
            : InfoBarSeverity.Warning;
        StatusInfoBar.IsOpen = true;

        RecordsGridView.ItemsSource = session.Measurements.Select(ToViewModel).ToList();
        ExportJsonButton.IsEnabled = true;
        ExportCsvButton.IsEnabled = true;
        ExportBgraButton.IsEnabled = _measurementService.CurrentFrame is not null;
        ExportDebugOverlayButton.IsEnabled = _measurementService.CurrentFrame is not null;
    }

    private static MeasurementRecordViewModel ToViewModel(MeasurementRecord record)
    {
        string expected = FormatColorValue(record.Expected);
        string captured = FormatColorValue(record.Captured);
        string mean = FormatChannelStatistics(record.ChannelStatistics, s => s.Mean);
        string median = FormatChannelStatistics(record.ChannelStatistics, s => s.Median);
        string stdDev = FormatChannelStatistics(record.ChannelStatistics, s => s.StandardDeviation);
        string validity = record.Validity.ToString();
        return new MeasurementRecordViewModel(
            record.PatchId,
            expected,
            captured,
            mean,
            median,
            stdDev,
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

        var picker = new global::Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = $"measurement-{session.CreatedAt:yyyyMMdd-HHmmss}";
        picker.FileTypeChoices.Add("JSON", new[] { ".json" });

        WinRT.Interop.InitializeWithWindow.Initialize(picker, ((App)App.Current).WindowHandle);
        global::Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        string json = _serializer.SerializeMeasurement(session);
        await global::Windows.Storage.FileIO.WriteTextAsync(file, json);
    }

    private async void ExportCsvButton_Click(object sender, RoutedEventArgs e)
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        if (session is null)
        {
            return;
        }

        var picker = new global::Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = $"measurement-{session.CreatedAt:yyyyMMdd-HHmmss}";
        picker.FileTypeChoices.Add("CSV", new[] { ".csv" });

        WinRT.Interop.InitializeWithWindow.Initialize(picker, ((App)App.Current).WindowHandle);
        global::Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        string csv = _measurementService.ExportCurrentSessionAsCsv();
        await global::Windows.Storage.FileIO.WriteTextAsync(file, csv);
    }

    private async void ExportBgraButton_Click(object sender, RoutedEventArgs e)
    {
        byte[]? pixels = _measurementService.ExportCurrentFrameAsBgra();
        if (pixels is null)
        {
            return;
        }

        MeasurementSession? session = _measurementService.CurrentSession;
        var picker = new global::Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = session is not null
            ? $"capture-{session.CreatedAt:yyyyMMdd-HHmmss}"
            : "capture-frame";
        picker.FileTypeChoices.Add("BGRA", new[] { ".bgra" });

        WinRT.Interop.InitializeWithWindow.Initialize(picker, ((App)App.Current).WindowHandle);
        global::Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        await global::Windows.Storage.FileIO.WriteBytesAsync(file, pixels);
    }

    private async void ExportDebugOverlayButton_Click(object sender, RoutedEventArgs e)
    {
        MeasurementSession? session = _measurementService.CurrentSession;
        CapturedFrame? frame = _measurementService.CurrentFrame;
        if (session is null || frame is null || session.CaptureGeometry is null)
        {
            return;
        }

        var picker = new global::Windows.Storage.Pickers.FileSavePicker();
        picker.SuggestedStartLocation = global::Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.SuggestedFileName = $"overlay-{session.CreatedAt:yyyyMMdd-HHmmss}";
        picker.FileTypeChoices.Add("PNG", new[] { ".png" });

        WinRT.Interop.InitializeWithWindow.Initialize(picker, ((App)App.Current).WindowHandle);
        global::Windows.Storage.StorageFile? file = await picker.PickSaveFileAsync();
        if (file is null)
        {
            return;
        }

        await _overlayService.SaveDebugOverlayAsync(
            frame,
            session.CaptureGeometry,
            session.Layout,
            file);
    }

    private void BackToChartButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ChartPage));
    }
}
