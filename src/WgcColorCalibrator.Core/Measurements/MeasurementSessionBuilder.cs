using System.Linq;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Common;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Builds a <see cref="MeasurementSession"/> from captured data and geometry.
/// </summary>
public static class MeasurementSessionBuilder
{
    public static MeasurementSession Build(
        ApplicationInfo application,
        IReadOnlyDictionary<string, string> system,
        IReadOnlyDictionary<string, string> gpu,
        IReadOnlyDictionary<string, string> display,
        IReadOnlyDictionary<string, string> hdr,
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> layout,
        ChartRenderSession renderSession,
        CaptureSummary captureSummary,
        CaptureGeometry captureGeometry,
        IReadOnlyList<PatchSample> patchSamples)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(system);
        ArgumentNullException.ThrowIfNull(gpu);
        ArgumentNullException.ThrowIfNull(display);
        ArgumentNullException.ThrowIfNull(hdr);
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(renderSession);
        ArgumentNullException.ThrowIfNull(captureSummary);
        ArgumentNullException.ThrowIfNull(captureGeometry);
        ArgumentNullException.ThrowIfNull(patchSamples);

        RenderSummary renderSummary = ToRenderSummary(renderSession);
        List<string> warnings = new(captureGeometry.Warnings);
        List<MeasurementRecord> records = new(patchSamples.Count);
        Dictionary<string, ColorPatchDefinition> patchById = chart.Patches.ToDictionary(p => p.Id, StringComparer.Ordinal);

        foreach (PatchSample sample in patchSamples)
        {
            if (!patchById.TryGetValue(sample.PatchId, out ColorPatchDefinition? patch))
            {
                warnings.Add($"unknown-patch-id: {sample.PatchId}");
                continue;
            }

            MeasurementValidity validity = DetermineValidity(captureGeometry.MappingStatus, sample);
            ColorValue expected = new(
                patch.SourceEncoding,
                patch.ExpectedColor,
                patch.HdrColor is not null ? new RgbaFloat(patch.HdrColor.Value.R, patch.HdrColor.Value.G, patch.HdrColor.Value.B, 1.0f) : null);
            ColorValue captured = sample.Rgb8Value.HasValue
                ? new ColorValue(ColorEncoding.CaptureNative, sample.Rgb8Value.Value, null)
                : new ColorValue(ColorEncoding.Unknown, null, null);

            records.Add(new MeasurementRecord(
                sample.PatchId,
                expected,
                null,
                captured,
                new SamplingSummary(sample.Method, sample.SampleCount),
                sample.Statistics,
                validity,
                sample.Warnings.ToList()));
        }

        return new MeasurementSession(
            application,
            system,
            gpu,
            display,
            hdr,
            captureSummary,
            chart,
            layout,
            renderSummary,
            captureGeometry,
            records,
            new List<AnalyzerResult>(),
            warnings,
            DateTimeOffset.UtcNow);
    }

    private static RenderSummary ToRenderSummary(ChartRenderSession session)
    {
        return new RenderSummary(
            session.RendererId,
            session.ToneMapperId,
            session.RequestedOutputMode,
            session.ActualOutputMode,
            session.SwapChainFormat,
            session.DxgiColorSpace,
            session.HdrOutputActive,
            session.ToneMappingParameters,
            session.IntendedPhysicalSize,
            session.ActualPhysicalSize,
            session.ClientPhysicalSize,
            new PixelPoint((int)Math.Round(session.ContentOrigin.X), (int)Math.Round(session.ContentOrigin.Y)),
            session.CompositionScaleX,
            session.CompositionScaleY,
            session.MatrixTransform,
            session.DisplayOutput,
            session.Warnings);
    }

    private static MeasurementValidity DetermineValidity(CaptureMappingStatus mappingStatus, PatchSample sample)
    {
        if (mappingStatus == CaptureMappingStatus.Unverified)
        {
            return MeasurementValidity.GeometryUnverified;
        }

        if (sample.Warnings.Contains("sample-region-clipped"))
        {
            return MeasurementValidity.SampleRegionClipped;
        }

        if (sample.SampleCount == 0)
        {
            return MeasurementValidity.EmptySample;
        }

        return MeasurementValidity.Valid;
    }
}
