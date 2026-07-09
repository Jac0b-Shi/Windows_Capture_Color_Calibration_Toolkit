using System.Globalization;
using System.Text;
using WgcColorCalibrator.Core.Measurements;

namespace WgcColorCalibrator.Core.Serialization;

/// <summary>
/// Writes the MVP measurement CSV export format.
/// </summary>
public static class MeasurementCsvSerializer
{
    private const string Header = "patchId,label,expectedR,expectedG,expectedB,capturedR,capturedG,capturedB,meanR,meanG,meanB,medianR,medianG,medianB,stdDevR,stdDevG,stdDevB,deltaR,deltaG,deltaB,capturePixelFormat,sampleMethod";

    public static string Serialize(MeasurementSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var patchById = session.Chart.Patches.ToDictionary(patch => patch.Id, StringComparer.Ordinal);
        var builder = new StringBuilder();
        builder.AppendLine(Header);

        foreach (MeasurementRecord measurement in session.Measurements)
        {
            patchById.TryGetValue(measurement.PatchId, out var patch);
            var expected = measurement.Expected.Rgb8;
            var captured = measurement.Captured.Rgb8;
            ChannelStatistics stats = measurement.ChannelStatistics;

            builder.Append(Escape(measurement.PatchId)).Append(',')
                .Append(Escape(patch?.Label ?? measurement.PatchId)).Append(',')
                .Append(ByteOrEmpty(expected?.R)).Append(',')
                .Append(ByteOrEmpty(expected?.G)).Append(',')
                .Append(ByteOrEmpty(expected?.B)).Append(',')
                .Append(ByteOrEmpty(captured?.R)).Append(',')
                .Append(ByteOrEmpty(captured?.G)).Append(',')
                .Append(ByteOrEmpty(captured?.B)).Append(',')
                .Append(DoubleOrEmpty(stats?.R.Mean)).Append(',')
                .Append(DoubleOrEmpty(stats?.G.Mean)).Append(',')
                .Append(DoubleOrEmpty(stats?.B.Mean)).Append(',')
                .Append(DoubleOrEmpty(stats?.R.Median)).Append(',')
                .Append(DoubleOrEmpty(stats?.G.Median)).Append(',')
                .Append(DoubleOrEmpty(stats?.B.Median)).Append(',')
                .Append(DoubleOrEmpty(stats?.R.StandardDeviation)).Append(',')
                .Append(DoubleOrEmpty(stats?.G.StandardDeviation)).Append(',')
                .Append(DoubleOrEmpty(stats?.B.StandardDeviation)).Append(',')
                .Append(DeltaOrEmpty(expected?.R, captured?.R)).Append(',')
                .Append(DeltaOrEmpty(expected?.G, captured?.G)).Append(',')
                .Append(DeltaOrEmpty(expected?.B, captured?.B)).Append(',')
                .Append(session.Capture.ActualPixelFormat).Append(',')
                .Append(measurement.Sampling.Method)
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string ByteOrEmpty(byte? value) =>
        value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private static string DoubleOrEmpty(double? value) =>
        value.HasValue ? value.Value.ToString("F4", CultureInfo.InvariantCulture) : string.Empty;

    private static string DeltaOrEmpty(byte? expected, byte? captured)
    {
        if (!expected.HasValue || !captured.HasValue)
        {
            return string.Empty;
        }

        return (captured.Value - expected.Value).ToString(CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        if (value.Contains('"', StringComparison.Ordinal) ||
            value.Contains(',', StringComparison.Ordinal) ||
            value.Contains('\n', StringComparison.Ordinal) ||
            value.Contains('\r', StringComparison.Ordinal))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }
}

