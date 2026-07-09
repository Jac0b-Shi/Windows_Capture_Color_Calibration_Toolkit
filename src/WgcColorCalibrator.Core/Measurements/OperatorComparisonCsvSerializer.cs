using System.Globalization;
using System.Text;
using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Serializes operator comparison results to CSV using invariant culture.
/// </summary>
public static class OperatorComparisonCsvSerializer
{
    public static string Serialize(IReadOnlyList<OperatorComparisonResult> results, MeasurementSession session)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(session);

        StringBuilder builder = new();
        AppendHeader(builder, results);

        for (int recordIndex = 0; recordIndex < session.Measurements.Count; recordIndex++)
        {
            MeasurementRecord measurement = session.Measurements[recordIndex];
            AppendValue(builder, measurement.PatchId);
            AppendFloat(builder, ToRgbaFloat(measurement.Expected));
            AppendFloat(builder, ToRgbaFloat(measurement.Captured));
            AppendDelta(builder, measurement, recordIndex, results);
            AppendMappedValues(builder, recordIndex, results);
            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static void AppendHeader(StringBuilder builder, IReadOnlyList<OperatorComparisonResult> results)
    {
        builder.Append("PatchId");
        builder.Append(",ExpectedR,ExpectedG,ExpectedB,ExpectedA");
        builder.Append(",CapturedR,CapturedG,CapturedB,CapturedA");
        builder.Append(",DeltaR,DeltaG,DeltaB");

        foreach (OperatorComparisonResult result in results)
        {
            string id = result.OperatorId;
            builder.Append(CultureInfo.InvariantCulture, $",{id}-MappedR,{id}-MappedG,{id}-MappedB,{id}-MappedA");
        }

        builder.AppendLine();
    }

    private static void AppendFloat(StringBuilder builder, RgbaFloat value)
    {
        builder.Append(CultureInfo.InvariantCulture, $",{value.R:F6},{value.G:F6},{value.B:F6},{value.A:F6}");
    }

    private static void AppendDelta(StringBuilder builder, MeasurementRecord measurement, int recordIndex, IReadOnlyList<OperatorComparisonResult> results)
    {
        RgbaFloat captured = ToRgbaFloat(measurement.Captured);
        RgbaFloat expected = ToRgbaFloat(measurement.Expected);
        RgbaFloat delta = new(
            captured.R - expected.R,
            captured.G - expected.G,
            captured.B - expected.B,
            captured.A - expected.A);
        builder.Append(CultureInfo.InvariantCulture, $",{delta.R:F6},{delta.G:F6},{delta.B:F6}");
    }

    private static void AppendMappedValues(StringBuilder builder, int recordIndex, IReadOnlyList<OperatorComparisonResult> results)
    {
        foreach (OperatorComparisonResult result in results)
        {
            RgbaFloat mapped = result.Records[recordIndex].Mapped;
            builder.Append(CultureInfo.InvariantCulture, $",{mapped.R:F6},{mapped.G:F6},{mapped.B:F6},{mapped.A:F6}");
        }
    }

    private static void AppendValue(StringBuilder builder, string value)
    {
        builder.Append(value);
    }

    private static RgbaFloat ToRgbaFloat(ColorValue value)
    {
        if (value.Rgba.HasValue)
        {
            return value.Rgba.Value;
        }

        if (value.Rgb8.HasValue)
        {
            Rgb8 rgb = value.Rgb8.Value;
            return new RgbaFloat(
                ColorSpaceConverter.SrgbByteToLinear(rgb.R),
                ColorSpaceConverter.SrgbByteToLinear(rgb.G),
                ColorSpaceConverter.SrgbByteToLinear(rgb.B),
                1.0f);
        }

        throw new InvalidOperationException("ColorValue does not contain a convertible color.");
    }
}
