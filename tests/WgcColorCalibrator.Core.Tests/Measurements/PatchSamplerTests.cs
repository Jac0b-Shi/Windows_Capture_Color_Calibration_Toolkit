using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Tests.Measurements;

public sealed class PatchSamplerTests
{
    [Fact]
    public void Sample_SinglePixelCenter_ReturnsMeanValue()
    {
        CapturedFrame frame = CreateSolidFrame(4, 4, 0x80, 0x90, 0xA0);
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 2, 2), new PixelRect(0, 0, 1, 1));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.Equal("p1", sample.PatchId);
        Assert.True(sample.Rgb8Value.HasValue);
        Assert.Equal((byte)0xA0, sample.Rgb8Value.Value.R);
        Assert.Equal((byte)0x80, sample.Rgb8Value.Value.B);
        Assert.Empty(sample.Warnings);
    }

    [Fact]
    public void Sample_OutOfBounds_ReturnsClippedWarning()
    {
        CapturedFrame frame = CreateSolidFrame(4, 4, 0x80, 0x90, 0xA0);
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 2, 2), new PixelRect(0, 0, 2, 2));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(3, 0), placement, SampleMethod.CenterMean);

        Assert.Contains("sample-region-clipped", sample.Warnings);
    }

    [Fact]
    public void Sample_Mean_ComputesAverage()
    {
        CapturedFrame frame = CreateFrameWithTwoColors(4, 4, new Rgb8(100, 100, 100), new Rgb8(200, 200, 200));
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.True(sample.Rgb8Value.HasValue);
        Assert.Equal((byte)150, sample.Rgb8Value.Value.R);
    }

    [Fact]
    public void Sample_Median_ComputesMedian()
    {
        CapturedFrame frame = CreateFrameWithTwoColors(4, 4, new Rgb8(100, 100, 100), new Rgb8(200, 200, 200));
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMedian);

        Assert.True(sample.Rgb8Value.HasValue);
        Assert.Equal((byte)150, sample.Rgb8Value.Value.R);
    }

    [Fact]
    public void Sample_NonuniformRegion_AddsContaminationWarning()
    {
        CapturedFrame frame = CreateFrameWithTwoColors(4, 4, new Rgb8(100, 100, 100), new Rgb8(200, 200, 200));
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.Contains("sample-region-nonuniform", sample.Warnings);
    }

    [Fact]
    public void Sample_UniformRegion_HasNoContaminationWarning()
    {
        CapturedFrame frame = CreateSolidFrame(4, 4, 0x80, 0x90, 0xA0);
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.DoesNotContain("sample-region-nonuniform", sample.Warnings);
    }

    [Fact]
    public void Sample_Rgba16Float_ReturnsFloatValue()
    {
        CapturedFrame frame = CreateSolidRgba16FloatFrame(4, 4, 1.25f, 1.5f, 2.0f);
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 2, 2), new PixelRect(0, 0, 1, 1));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.Equal("p1", sample.PatchId);
        Assert.Null(sample.Rgb8Value);
        Assert.True(sample.FloatValue.HasValue);
        Assert.Equal(1.25f, sample.FloatValue.Value.R, 4);
        Assert.Equal(1.5f, sample.FloatValue.Value.G, 4);
        Assert.Equal(2.0f, sample.FloatValue.Value.B, 4);
    }

    [Fact]
    public void Sample_Rgba16Float_Mean_ComputesAverage()
    {
        CapturedFrame frame = CreateRgba16FloatFrameWithTwoColors(4, 4, new RgbaFloat(1.0f, 1.0f, 1.0f, 1.0f), new RgbaFloat(2.0f, 2.0f, 2.0f, 1.0f));
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.Null(sample.Rgb8Value);
        Assert.True(sample.FloatValue.HasValue);
        Assert.Equal(1.5f, sample.FloatValue.Value.R, 4);
        Assert.Equal(1.5f, sample.Statistics.R.Mean, 4);
    }

    [Fact]
    public void Sample_Rgba16Float_Median_ComputesMedian()
    {
        CapturedFrame frame = CreateRgba16FloatFrameWithTwoColors(4, 4, new RgbaFloat(1.0f, 1.0f, 1.0f, 1.0f), new RgbaFloat(2.0f, 2.0f, 2.0f, 1.0f));
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMedian);

        Assert.True(sample.FloatValue.HasValue);
        Assert.Equal(1.5f, sample.FloatValue.Value.R, 4);
    }

    [Fact]
    public void Sample_Rgba16Float_NonuniformRegion_AddsContaminationWarning()
    {
        CapturedFrame frame = CreateRgba16FloatFrameWithTwoColors(4, 4, new RgbaFloat(1.0f, 1.0f, 1.0f, 1.0f), new RgbaFloat(20.0f, 20.0f, 20.0f, 1.0f));
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.Contains("sample-region-nonuniform", sample.Warnings);
    }

    [Fact]
    public void Sample_Rgba16Float_UniformRegion_HasNoContaminationWarning()
    {
        CapturedFrame frame = CreateSolidRgba16FloatFrame(4, 4, 1.0f, 1.0f, 1.0f);
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.DoesNotContain("sample-region-nonuniform", sample.Warnings);
    }

    [Fact]
    public void Sample_Rgba16Float_SmallVariation_AddsContaminationWarning()
    {
        CapturedFrame frame = CreateRgba16FloatFrameWithTwoColors(4, 4, new RgbaFloat(1.0f, 1.0f, 1.0f, 1.0f), new RgbaFloat(1.05f, 1.05f, 1.05f, 1.0f));
        PatchPlacement placement = new("p1", new PixelRect(0, 0, 4, 4), new PixelRect(0, 0, 4, 4));

        PatchSample sample = PatchSampler.Sample(frame, new PixelPoint(0, 0), placement, SampleMethod.CenterMean);

        Assert.Contains("sample-region-nonuniform", sample.Warnings);
    }

    private static CapturedFrame CreateSolidRgba16FloatFrame(int width, int height, float r, float g, float b)
    {
        byte[] pixels = new byte[width * height * 8];
        for (int i = 0; i < width * height; i++)
        {
            WriteRgba16Float(pixels, i * 8, r, g, b, 1.0f);
        }

        return new CapturedFrame(
            new SizeInt(width, height),
            new SizeInt(width, height),
            new SizeInt(width, height),
            CapturePixelFormat.R16G16B16A16Float,
            width * 8,
            pixels,
            CaptureSourceKind.Window,
            null,
            DateTimeOffset.UtcNow,
            []);
    }

    private static CapturedFrame CreateRgba16FloatFrameWithTwoColors(int width, int height, RgbaFloat first, RgbaFloat second)
    {
        byte[] pixels = new byte[width * height * 8];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                RgbaFloat color = x < width / 2 ? first : second;
                int offset = (y * width + x) * 8;
                WriteRgba16Float(pixels, offset, color.R, color.G, color.B, color.A);
            }
        }

        return new CapturedFrame(
            new SizeInt(width, height),
            new SizeInt(width, height),
            new SizeInt(width, height),
            CapturePixelFormat.R16G16B16A16Float,
            width * 8,
            pixels,
            CaptureSourceKind.Window,
            null,
            DateTimeOffset.UtcNow,
            []);
    }

    private static void WriteRgba16Float(byte[] pixels, int offset, float r, float g, float b, float a)
    {
        BitConverter.GetBytes((Half)r).CopyTo(pixels, offset);
        BitConverter.GetBytes((Half)g).CopyTo(pixels, offset + 2);
        BitConverter.GetBytes((Half)b).CopyTo(pixels, offset + 4);
        BitConverter.GetBytes((Half)a).CopyTo(pixels, offset + 6);
    }

    private static CapturedFrame CreateSolidFrame(int width, int height, byte b, byte g, byte r)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            pixels[i * 4] = b;
            pixels[i * 4 + 1] = g;
            pixels[i * 4 + 2] = r;
            pixels[i * 4 + 3] = 255;
        }

        return new CapturedFrame(
            new SizeInt(width, height),
            new SizeInt(width, height),
            new SizeInt(width, height),
            CapturePixelFormat.B8G8R8A8UIntNormalized,
            width * 4,
            pixels,
            CaptureSourceKind.Window,
            null,
            DateTimeOffset.UtcNow,
            []);
    }

    private static CapturedFrame CreateFrameWithTwoColors(int width, int height, Rgb8 first, Rgb8 second)
    {
        byte[] pixels = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Rgb8 color = x < width / 2 ? first : second;
                int offset = (y * width + x) * 4;
                pixels[offset] = color.B;
                pixels[offset + 1] = color.G;
                pixels[offset + 2] = color.R;
                pixels[offset + 3] = 255;
            }
        }

        return new CapturedFrame(
            new SizeInt(width, height),
            new SizeInt(width, height),
            new SizeInt(width, height),
            CapturePixelFormat.B8G8R8A8UIntNormalized,
            width * 4,
            pixels,
            CaptureSourceKind.Window,
            null,
            DateTimeOffset.UtcNow,
            []);
    }
}
