using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering.HdrToSdr;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Exports an HDR-to-SDR operator comparison CSV and per-operator preview PNGs.
/// </summary>
public sealed class MeasurementOperatorComparisonExportService
{
    private readonly OperatorComparisonService _operatorComparisonService;

    public MeasurementOperatorComparisonExportService(OperatorComparisonService operatorComparisonService)
    {
        _operatorComparisonService = operatorComparisonService ?? throw new ArgumentNullException(nameof(operatorComparisonService));
    }

    public async Task<OperatorComparisonExportResult> ExportAsync(
        MeasurementSession session,
        StorageFolder outputFolder,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(outputFolder);

        List<IHdrToSdrOperator> operators =
        [
            new ClampToSdrOperator(),
            new LinearScaleOperator(4.0f),
            new ReinhardOperator(),
            new ExposureGammaOperator(0.25f, 2.2f)
        ];

        IReadOnlyList<OperatorComparisonResult> results = _operatorComparisonService.Compare(session, operators, cancellationToken);

        List<string> fileNames = new(results.Count + 1);
        string csvFileName = $"operator-comparison-{timestamp:yyyyMMdd-HHmmss}.csv";
        fileNames.Add(csvFileName);

        string csv = OperatorComparisonCsvSerializer.Serialize(results, session);
        StorageFile csvFile = await outputFolder.CreateFileAsync(csvFileName, CreationCollisionOption.GenerateUniqueName);
        await FileIO.WriteTextAsync(csvFile, csv);

        foreach (OperatorComparisonResult result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string pngFileName = $"operator-preview-{result.OperatorId}-{timestamp:yyyyMMdd-HHmmss}.png";
            fileNames.Add(pngFileName);

            StorageFile pngFile = await outputFolder.CreateFileAsync(pngFileName, CreationCollisionOption.GenerateUniqueName);

            using IRandomAccessStream stream = await pngFile.OpenAsync(FileAccessMode.ReadWrite);
            BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            IBuffer buffer = CryptographicBuffer.CreateFromByteArray(result.PreviewBgra8);
            encoder.SetSoftwareBitmap(SoftwareBitmap.CreateCopyFromBuffer(
                buffer,
                BitmapPixelFormat.Bgra8,
                result.PreviewSize.Width,
                result.PreviewSize.Height,
                BitmapAlphaMode.Premultiplied));
            await encoder.FlushAsync();
        }

        return new OperatorComparisonExportResult(results.Count, session.Measurements.Count, fileNames);
    }
}
