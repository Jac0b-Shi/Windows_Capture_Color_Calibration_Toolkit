using System.Numerics;
using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Tests.Colors;

public class ColorSpaceConverterTests
{
    [Fact]
    public void SrgbByteToLinear_BlackAndWhite()
    {
        Assert.Equal(0.0f, ColorSpaceConverter.SrgbByteToLinear(0), 1e-6f);
        Assert.True(MathF.Abs(1.0f - ColorSpaceConverter.SrgbByteToLinear(255)) < 1e-5f);
    }

    [Fact]
    public void SrgbByteToLinear_128IsLinear()
    {
        // sRGB 128 (0.5039 normalized) maps to approximately 0.21586 linear.
        float linear = ColorSpaceConverter.SrgbByteToLinear(128);
        Assert.Equal(0.215860531f, linear, 1e-5f);
    }

    [Fact]
    public void NitsToScRgb_80NitsIsOne()
    {
        Assert.Equal(1.0, ColorSpaceConverter.NitsToScRgb(80.0), 1e-10);
    }

    [Theory]
    [InlineData(80, 1.0)]
    [InlineData(200, 2.5)]
    [InlineData(400, 5.0)]
    [InlineData(1000, 12.5)]
    public void NitsToScRgb_ReturnsExpectedValues(double nits, double expectedScRgb)
    {
        double actual = ColorSpaceConverter.NitsToScRgb(nits);
        Assert.Equal(expectedScRgb, actual, 1e-10);
    }

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(1.0, 2.0)]
    [InlineData(-1.0, 0.5)]
    [InlineData(2.0, 4.0)]
    public void ApplyExposureEv_ScalesCorrectly(double ev, double expected)
    {
        float actual = ColorSpaceConverter.ApplyExposureEv(1.0f, ev);
        Assert.Equal((float)expected, actual, 1e-6f);
    }

    [Fact]
    public void SrgbToLinear_White()
    {
        Vector3 linear = ColorSpaceConverter.SrgbToLinear(new Rgb8(255, 255, 255));
        Assert.Equal(new Vector3(1.0f, 1.0f, 1.0f), linear);
    }
}
