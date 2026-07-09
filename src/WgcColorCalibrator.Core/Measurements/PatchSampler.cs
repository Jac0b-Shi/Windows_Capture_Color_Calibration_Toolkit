using System.Linq;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Samples a chart patch from a captured BGRA frame.
/// </summary>
public static class PatchSampler
{
    public static PatchSample Sample(
        CapturedFrame frame,
        PixelPoint contentOffset,
        PatchPlacement placement,
        SampleMethod method)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(placement);

        PixelRect sampleBounds = placement.SafeSampleBounds;
        int sampleLeft = sampleBounds.X + contentOffset.X;
        int sampleTop = sampleBounds.Y + contentOffset.Y;
        int sampleRight = sampleLeft + sampleBounds.Width;
        int sampleBottom = sampleTop + sampleBounds.Height;

        if (sampleLeft < 0 || sampleTop < 0 ||
            sampleRight > frame.ContentSize.Width ||
            sampleBottom > frame.ContentSize.Height)
        {
            return CreateInvalidSample(placement, method, MeasurementValidity.SampleRegionClipped, "sample-region-clipped");
        }

        if (sampleBounds.Width <= 0 || sampleBounds.Height <= 0)
        {
            return CreateInvalidSample(placement, method, MeasurementValidity.EmptySample, "empty-sample-region");
        }

        int sampleCount = sampleBounds.Width * sampleBounds.Height;
        List<byte> rValues = new(sampleCount);
        List<byte> gValues = new(sampleCount);
        List<byte> bValues = new(sampleCount);

        for (int y = sampleTop; y < sampleBottom; y++)
        {
            int rowStart = y * frame.PackedRowStride;
            for (int x = sampleLeft; x < sampleRight; x++)
            {
                int offset = rowStart + x * 4;
                bValues.Add(frame.ContentPixels[offset]);
                gValues.Add(frame.ContentPixels[offset + 1]);
                rValues.Add(frame.ContentPixels[offset + 2]);
            }
        }

        bool useMedian = method == SampleMethod.CenterMedian;
        byte r = useMedian ? Median(rValues) : (byte)Math.Round(rValues.Average(v => v));
        byte g = useMedian ? Median(gValues) : (byte)Math.Round(gValues.Average(v => v));
        byte b = useMedian ? Median(bValues) : (byte)Math.Round(bValues.Average(v => v));

        Rgb8 rgb = new(r, g, b);
        ChannelStatistics statistics = new(
            new ChannelStatistic(rValues.Min(), rValues.Max(), rValues.Average(v => v), MedianAsDouble(rValues), StdDev(rValues), rValues.Distinct().Count()),
            new ChannelStatistic(gValues.Min(), gValues.Max(), gValues.Average(v => v), MedianAsDouble(gValues), StdDev(gValues), gValues.Distinct().Count()),
            new ChannelStatistic(bValues.Min(), bValues.Max(), bValues.Average(v => v), MedianAsDouble(bValues), StdDev(bValues), bValues.Distinct().Count()));

        List<string> warnings = new();
        if (IsNonuniform(statistics))
        {
            warnings.Add("sample-region-nonuniform");
        }

        return new PatchSample(
            placement.PatchId,
            method,
            sampleCount,
            rgb,
            null,
            statistics,
            warnings);
    }

    private static PatchSample CreateInvalidSample(
        PatchPlacement placement,
        SampleMethod method,
        MeasurementValidity validity,
        string warning)
    {
        ChannelStatistic emptyStat = new(0, 0, 0, 0, 0, 0);
        ChannelStatistics emptyStats = new(emptyStat, emptyStat, emptyStat);
        return new PatchSample(
            placement.PatchId,
            method,
            0,
            null,
            null,
            emptyStats,
            new List<string> { warning, validity.ToString().ToLowerInvariant() });
    }

    private static bool IsNonuniform(ChannelStatistics stats)
    {
        return IsNonuniform(stats.R) || IsNonuniform(stats.G) || IsNonuniform(stats.B);
    }

    private static bool IsNonuniform(ChannelStatistic stat)
    {
        return Math.Abs(stat.Mean - stat.Median) > 3.0 || stat.StandardDeviation > 5.0;
    }

    private static byte Median(List<byte> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        values.Sort();
        return values.Count % 2 == 1
            ? values[values.Count / 2]
            : (byte)((values[values.Count / 2 - 1] + values[values.Count / 2]) / 2);
    }

    private static double MedianAsDouble(List<byte> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        values.Sort();
        return values.Count % 2 == 1
            ? values[values.Count / 2]
            : (values[values.Count / 2 - 1] + values[values.Count / 2]) / 2.0;
    }

    private static double StdDev(List<byte> values)
    {
        if (values.Count == 0)
        {
            return 0;
        }

        double avg = values.Average(v => v);
        double sum = values.Sum(v => (v - avg) * (v - avg));
        return Math.Sqrt(sum / values.Count);
    }
}
