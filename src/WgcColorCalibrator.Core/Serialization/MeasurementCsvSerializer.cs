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
            var expectedFloat = measurement.Expected.Rgba;
            var captured = measurement.Captured.Rgb8;
            var capturedFloat = measurement.Captured.Rgba;
            ChannelStatistics stats = measurement.ChannelStatistics;

            builder.Append(Escape(measurement.PatchId)).Append(',')
                .Append(Escape(patch?.Label ?? measurement.PatchId)).Append(',')
                .Append(ByteOrFloat(expected?.R, expectedFloat?.R)).Append(',')
                .Append(ByteOrFloat(expected?.G, expectedFloat?.G)).Append(',')
                .Append(ByteOrFloat(expected?.B, expectedFloat?.B)).Append(',')
                .Append(ByteOrFloat(captured?.R, capturedFloat?.R)).Append(',')
                .Append(ByteOrFloat(captured?.G, capturedFloat?.G)).Append(',')
                .Append(ByteOrFloat(captured?.B, capturedFloat?.B)).Append(',')
                .Append(DoubleOrEmpty(stats?.R.Mean)).Append(',')
                .Append(DoubleOrEmpty(stats?.G.Mean)).Append(',')
                .Append(DoubleOrEmpty(stats?.B.Mean)).Append(',')
                .Append(DoubleOrEmpty(stats?.R.Median)).Append(',')
                .Append(DoubleOrEmpty(stats?.G.Median)).Append(',')
                .Append(DoubleOrEmpty(stats?.B.Median)).Append(',')
                .Append(DoubleOrEmpty(stats?.R.StandardDeviation)).Append(',')
                .Append(DoubleOrEmpty(stats?.G.StandardDeviation)).Append(',')
                .Append(DoubleOrEmpty(stats?.B.StandardDeviation)).Append(',')
                .Append(DeltaOrEmpty(expected?.R, expectedFloat?.R, captured?.R, capturedFloat?.R)).Append(',')
                .Append(DeltaOrEmpty(expected?.G, expectedFloat?.G, captured?.G, capturedFloat?.G)).Append(',')
                .Append(DeltaOrEmpty(expected?.B, expectedFloat?.B, captured?.B, capturedFloat?.B)).Append(',')
                .Append(session.Capture.ActualPixelFormat).Append(',')
                .Append(measurement.Sampling.Method)
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string ByteOrFloat(byte? byteValue, float? floatValue) =>
        byteValue.HasValue
            ? byteValue.Value.ToString(CultureInfo.InvariantCulture)
            : floatValue.HasValue
                ? floatValue.Value.ToString("F6", CultureInfo.InvariantCulture)
                : string.Empty;

    private static string DeltaOrEmpty(byte? expectedByte, float? expectedFloat, byte? capturedByte, float? capturedFloat)
    {
        if (expectedByte.HasValue && capturedByte.HasValue)
        {
            return (capturedByte.Value - expectedByte.Value).ToString(CultureInfo.InvariantCulture);
        }

        if (expectedFloat.HasValue && capturedFloat.HasValue)
        {
            return (capturedFloat.Value - expectedFloat.Value).ToString("F6", CultureInfo.InvariantCulture);
        }

        return string.Empty;
    }

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

