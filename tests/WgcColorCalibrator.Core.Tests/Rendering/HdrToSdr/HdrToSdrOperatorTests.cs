using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering.HdrToSdr;

namespace WgcColorCalibrator.Core.Tests.Rendering.HdrToSdr;

public sealed class HdrToSdrOperatorTests
{
    [Theory]
    [InlineData(0.0f, 0.0f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1.0f, 1.0f)]
    [InlineData(2.0f, 1.0f)]
    public void ClampToSdr_MapsAndClamps(float input, float expected)
    {
        var op = new ClampToSdrOperator();

        RgbaFloat result = op.Map(new RgbaFloat(input, input, input, 1.0f));

        Assert.Equal(expected, result.R, 6);
        Assert.Equal(expected, result.G, 6);
        Assert.Equal(expected, result.B, 6);
    }

    [Theory]
    [InlineData(1.0f, 2.0f, 0.5f)]
    [InlineData(2.0f, 2.0f, 1.0f)]
    [InlineData(4.0f, 2.0f, 1.0f)]
    [InlineData(4.0f, 4.0f, 1.0f)]
    public void LinearScale_MapsInputWhiteToSdrWhite(float input, float inputWhite, float expected)
    {
        var op = new LinearScaleOperator(inputWhite);

        RgbaFloat result = op.Map(new RgbaFloat(input, input, input, 1.0f));

        Assert.Equal(expected, result.R, 6);
    }

    [Fact]
    public void LinearScale_ThrowsForNonPositiveInputWhite()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearScaleOperator(0.0f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LinearScaleOperator(-1.0f));
    }

    [Theory]
    [InlineData(0.0f, 0.0f)]
    [InlineData(1.0f, 0.5f)]
    [InlineData(2.0f, 2.0f / 3.0f)]
    [InlineData(4.0f, 0.8f)]
    public void Reinhard_AppliesToneCurve(float input, float expected)
    {
        var op = new ReinhardOperator();

        RgbaFloat result = op.Map(new RgbaFloat(input, input, input, 1.0f));

        Assert.Equal(expected, result.R, 6);
    }

    [Theory]
    [InlineData(0.0f, 1.0f, 2.2f, 0.0f)]
    [InlineData(1.0f, 1.0f, 2.2f, 0.8118f)]
    [InlineData(4.0f, 0.25f, 2.2f, 0.8118f)]
    public void ExposureGamma_AppliesExposureAndGamma(float input, float exposure, float gamma, float expected)
    {
        var op = new ExposureGammaOperator(exposure, gamma);

        RgbaFloat result = op.Map(new RgbaFloat(input, input, input, 1.0f));

        Assert.Equal(expected, result.R, 4);
    }

    [Fact]
    public void ExposureGamma_ThrowsForNonPositiveParameters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExposureGammaOperator(0.0f, 2.2f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExposureGammaOperator(1.0f, 0.0f));
    }
}
