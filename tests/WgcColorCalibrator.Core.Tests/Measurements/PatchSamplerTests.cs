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
