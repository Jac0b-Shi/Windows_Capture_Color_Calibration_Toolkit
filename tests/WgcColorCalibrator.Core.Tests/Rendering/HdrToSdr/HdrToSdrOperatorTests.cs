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
    public void CustomExpression_EvaluatesPerChannel()
    {
        var parameters = new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["scale"] = 0.5f,
        };
        var op = new CustomExpressionOperator(
            "saturate(x * scale)",
            parameters);

        RgbaFloat result = op.Map(new RgbaFloat(2.0f, 4.0f, 1.0f, 1.0f));

        Assert.Equal(1.0f, result.R, 4);
        Assert.Equal(1.0f, result.G, 4);
        Assert.Equal(0.5f, result.B, 4);
        Assert.Equal(1.0f, result.A, 4);
    }

    [Fact]
    public void CustomExpression_CanReferenceOtherChannels()
    {
        var parameters = new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["scale"] = 1.0f,
        };
        var op = new CustomExpressionOperator(
            "saturate((r + g + b) / 3 * scale)",
            parameters);

        RgbaFloat result = op.Map(new RgbaFloat(1.0f, 0.5f, 0.0f, 1.0f));

        Assert.Equal(0.5f, result.R, 6);
        Assert.Equal(0.5f, result.G, 6);
        Assert.Equal(0.5f, result.B, 6);
    }

    [Fact]
    public void CustomExpression_ThrowsOnUnknownFunction()
    {
        Assert.Throws<ExpressionParseException>(() => new CustomExpressionOperator("unknown(x)", ReadOnlyParameters));
    }

    [Fact]
    public void CustomExpression_ThrowsOnMissingParameter()
    {
        Assert.Throws<ExpressionParseException>(() => new CustomExpressionOperator("x * missing", ReadOnlyParameters));
    }

    [Fact]
    public void ExposureGamma_ThrowsForNonPositiveParameters()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExposureGammaOperator(0.0f, 2.2f));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ExposureGammaOperator(1.0f, 0.0f));
    }

    private static IReadOnlyDictionary<string, float> ReadOnlyParameters { get; } = new Dictionary<string, float>(StringComparer.Ordinal);
}
