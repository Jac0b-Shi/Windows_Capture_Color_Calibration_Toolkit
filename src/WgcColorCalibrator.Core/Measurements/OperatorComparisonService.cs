using System.Globalization;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.Core.Rendering.HdrToSdr;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Compares a set of HDR-to-SDR operators across every measurement in a session
/// and produces per-operator preview images.
/// </summary>
public sealed class OperatorComparisonService
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Instance method matches service design.")]
    public IReadOnlyList<OperatorComparisonResult> Compare(
        MeasurementSession session,
        IReadOnlyList<IHdrToSdrOperator> operators,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(operators);

        if (session.Capture.ActualPixelFormat != CapturePixelFormat.R16G16B16A16Float)
        {
            throw new InvalidOperationException("Operator comparison is only supported for FP16 captures.");
        }

        if (session.CaptureGeometry is null)
        {
            throw new InvalidOperationException("Capture geometry is required for operator comparison.");
        }

        SizeInt previewSize = ComputePreviewSize(session.Layout, session.CaptureGeometry);
        List<OperatorComparisonResult> results = new(operators.Count);

        foreach (IHdrToSdrOperator op in operators)
        {
            cancellationToken.ThrowIfCancellationRequested();

            List<OperatorComparisonRecord> records = new(session.Measurements.Count);
            byte[] preview = CreateBlackPreview(previewSize);

            foreach (MeasurementRecord measurement in session.Measurements)
            {
                RgbaFloat expected = ToRgbaFloat(measurement.Expected);
                RgbaFloat captured = ToRgbaFloat(measurement.Captured);
                RgbaFloat mapped = op.Map(captured);
                RgbaFloat delta = new(
                    captured.R - expected.R,
                    captured.G - expected.G,
                    captured.B - expected.B,
                    captured.A - expected.A);

                records.Add(new OperatorComparisonRecord(
                    measurement.PatchId,
                    measurement.Expected,
                    measurement.Captured,
                    mapped,
                    delta));

                FillPatchPreview(preview, previewSize, session.CaptureGeometry, measurement.PatchId, mapped, session.Layout);
            }

            results.Add(new OperatorComparisonResult(
                op.Id,
                op.Id,
                records,
                previewSize,
                preview));
        }

        return results;
    }

    private static SizeInt ComputePreviewSize(IReadOnlyList<PatchPlacement> layout, CaptureGeometry geometry)
    {
        int offsetX = geometry.ContentOffset.X;
        int offsetY = geometry.ContentOffset.Y;
        int width = 0;
        int height = 0;

        foreach (PatchPlacement placement in layout)
        {
            width = Math.Max(width, placement.Bounds.X + placement.Bounds.Width + offsetX);
            height = Math.Max(height, placement.Bounds.Y + placement.Bounds.Height + offsetY);
        }

        return new SizeInt(width, height);
    }

    private static byte[] CreateBlackPreview(SizeInt size)
    {
        int length = size.Width * size.Height * 4;
        byte[] buffer = new byte[length];

        for (int i = 3; i < length; i += 4)
        {
            buffer[i] = 0xFF;
        }

        return buffer;
    }

    private static void FillPatchPreview(
        byte[] preview,
        SizeInt previewSize,
        CaptureGeometry geometry,
        string patchId,
        RgbaFloat mapped,
        IReadOnlyList<PatchPlacement> layout)
    {
        PatchPlacement? placement = null;
        foreach (PatchPlacement candidate in layout)
        {
            if (candidate.PatchId == patchId)
            {
                placement = candidate;
                break;
            }
        }

        if (placement is null)
        {
            return;
        }

        byte b = ColorSpaceConverter.LinearToSrgbByte(mapped.B);
        byte g = ColorSpaceConverter.LinearToSrgbByte(mapped.G);
        byte r = ColorSpaceConverter.LinearToSrgbByte(mapped.R);
        byte a = 0xFF;

        int offsetX = geometry.ContentOffset.X;
        int offsetY = geometry.ContentOffset.Y;
        int left = Math.Max(0, placement.Bounds.X + offsetX);
        int top = Math.Max(0, placement.Bounds.Y + offsetY);
        int right = Math.Min(previewSize.Width, placement.Bounds.X + offsetX + placement.Bounds.Width);
        int bottom = Math.Min(previewSize.Height, placement.Bounds.Y + offsetY + placement.Bounds.Height);

        for (int y = top; y < bottom; y++)
        {
            int rowStart = y * previewSize.Width * 4;
            for (int x = left; x < right; x++)
            {
                int offset = rowStart + x * 4;
                preview[offset] = b;
                preview[offset + 1] = g;
                preview[offset + 2] = r;
                preview[offset + 3] = a;
            }
        }
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
