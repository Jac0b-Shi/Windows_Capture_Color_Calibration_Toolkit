using System.Text.Json;
using System.Text.Json.Nodes;
using Windows.Graphics.Imaging;
using Windows.Security.Cryptography;
using Windows.Storage;
using Windows.Storage.Streams;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering.HdrToSdr;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Exports an HDR-to-SDR operator comparison CSV, per-operator preview PNGs, and a manifest file
/// into a single dedicated folder. All file names are deterministic and collisions are treated as bugs.
/// </summary>
public sealed class MeasurementOperatorComparisonExportService
{
    private const string ManifestSchemaVersion = "1.0.0";

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

        List<string> fileNames = new(results.Count + 2)
        {
            "manifest.json",
            "operator-comparison.csv"
        };

        foreach (OperatorComparisonResult result in results)
        {
            fileNames.Add($"preview-{result.OperatorId}.png");
        }

        string csv = OperatorComparisonCsvSerializer.Serialize(results, session);
        await WriteTextFileAsync(outputFolder, "operator-comparison.csv", csv);

        await WriteManifestFileAsync(outputFolder, timestamp, session, operators, results, fileNames);

        foreach (OperatorComparisonResult result in results)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string pngFileName = $"preview-{result.OperatorId}.png";
            StorageFile pngFile = await outputFolder.CreateFileAsync(pngFileName, CreationCollisionOption.FailIfExists);

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

    private static async Task WriteTextFileAsync(StorageFolder folder, string fileName, string content)
    {
        StorageFile file = await folder.CreateFileAsync(fileName, CreationCollisionOption.FailIfExists);
        await FileIO.WriteTextAsync(file, content);
    }

    private static async Task WriteManifestFileAsync(
        StorageFolder folder,
        DateTimeOffset timestamp,
        MeasurementSession session,
        IReadOnlyList<IHdrToSdrOperator> operators,
        IReadOnlyList<OperatorComparisonResult> results,
        IReadOnlyList<string> fileNames)
    {
        var manifest = new JsonObject
        {
            ["schemaVersion"] = ManifestSchemaVersion,
            ["appVersion"] = session.Application.Version,
            ["createdAt"] = timestamp.ToString("O", System.Globalization.CultureInfo.InvariantCulture),
            ["sourceCapturePixelFormat"] = session.Capture.ActualPixelFormat.ToString(),
            ["chartId"] = session.Chart.Id,
            ["chartName"] = session.Chart.Name,
            ["operatorCount"] = operators.Count,
            ["patchCount"] = session.Measurements.Count,
            ["operators"] = new JsonArray(results.Select(r => JsonValue.Create(r.OperatorId)).ToArray()),
            ["exportedFiles"] = new JsonArray(fileNames.Select(f => JsonValue.Create(f)).ToArray())
        };

        StorageFile manifestFile = await folder.CreateFileAsync("manifest.json", CreationCollisionOption.FailIfExists);
        await FileIO.WriteTextAsync(manifestFile, manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}
