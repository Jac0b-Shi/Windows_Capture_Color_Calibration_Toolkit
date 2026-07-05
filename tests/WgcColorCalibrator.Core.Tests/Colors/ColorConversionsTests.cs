using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Tests.Colors;

public sealed class ColorConversionsTests
{
    [Theory]
    [InlineData(255, 0, 0, 0.0, 1.0, 1.0)]
    [InlineData(0, 255, 0, 120.0, 1.0, 1.0)]
    [InlineData(0, 0, 255, 240.0, 1.0, 1.0)]
    [InlineData(128, 128, 128, 0.0, 0.0, 128.0 / 255.0)]
    public void ToHsv_ConvertsRgb8(byte r, byte g, byte b, double h, double s, double v)
    {
        Hsv actual = ColorConversions.ToHsv(new Rgb8(r, g, b));

        Assert.Equal(h, actual.H, precision: 6);
        Assert.Equal(s, actual.S, precision: 6);
        Assert.Equal(v, actual.V, precision: 6);
    }
}

