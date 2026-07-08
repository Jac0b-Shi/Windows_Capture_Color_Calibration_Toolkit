using System.Numerics;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Tests.Rendering;

public class ToneMapperTests
{
    [Fact]
    public void DirectScRgb_PassesLinearThrough()
    {
        var mapper = new ToneMapperDirectScRgb();
        var input = new Vector4(1.0f, 2.0f, 3.0f, 1.0f);
        var parameters = new ToneMappingParameters(200.0, 1000.0, 0.0);

        Vector4 output = mapper.Map(input, parameters);

        Assert.Equal(input, output);
    }

    [Fact]
    public void DirectScRgb_PaperWhiteChange_DoesNotAlterOutput()
    {
        var mapper = new ToneMapperDirectScRgb();
        var input = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        Vector4 output80 = mapper.Map(input, new ToneMappingParameters(80.0, 1000.0, 0.0));
        Vector4 output400 = mapper.Map(input, new ToneMappingParameters(400.0, 1000.0, 0.0));

        Assert.Equal(output80, output400);
    }

    [Theory]
    [InlineData(80.0, 1.0f)]
    [InlineData(200.0, 2.5f)]
    [InlineData(400.0, 5.0f)]
    public void ReferenceWhiteScaled_WhiteSrgb_MapsToExpectedScRgb(
        double paperWhiteNits,
        float expectedScRgb)
    {
        var mapper = new ToneMapperReferenceWhiteScaled();
        float linear = ColorSpaceConverter.SrgbByteToLinear(255);
        var input = new Vector4(linear, linear, linear, 1.0f);

        Vector4 output = mapper.Map(input, new ToneMappingParameters(paperWhiteNits, 1000.0, 0.0));

        Assert.Equal(expectedScRgb, output.X, precision: 6);
        Assert.Equal(expectedScRgb, output.Y, precision: 6);
        Assert.Equal(expectedScRgb, output.Z, precision: 6);
    }

    [Fact]
    public void ReferenceWhiteScaled_PaperWhiteChange_ChangesOutput()
    {
        var mapper = new ToneMapperReferenceWhiteScaled();
        float linear = ColorSpaceConverter.SrgbByteToLinear(255);
        var input = new Vector4(linear, linear, linear, 1.0f);

        Vector4 output80 = mapper.Map(input, new ToneMappingParameters(80.0, 1000.0, 0.0));
        Vector4 output400 = mapper.Map(input, new ToneMappingParameters(400.0, 1000.0, 0.0));

        Assert.NotEqual(output80, output400);
    }

    [Fact]
    public void ReferenceWhiteScaled_200NitsScalesBy2Point5()
    {
        var mapper = new ToneMapperReferenceWhiteScaled();
        var input = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        var parameters = new ToneMappingParameters(200.0, 1000.0, 0.0);

        Vector4 output = mapper.Map(input, parameters);

        Assert.Equal(new Vector4(2.5f, 2.5f, 2.5f, 1.0f), output);
    }

    [Fact]
    public void ReferenceWhiteScaled_ExposureEvScales()
    {
        var mapper = new ToneMapperReferenceWhiteScaled();
        var input = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        var parameters = new ToneMappingParameters(80.0, 1000.0, 1.0);

        Vector4 output = mapper.Map(input, parameters);

        // 80 nits -> scale 1.0, exposure 1 EV -> scale 2.0, total 2.0
        Assert.Equal(new Vector4(2.0f, 2.0f, 2.0f, 1.0f), output);
    }

    [Theory]
    [InlineData(ToneMappingMode.DirectScRgb, "direct-scrgb")]
    [InlineData(ToneMappingMode.ReferenceWhiteScaled, "reference-white-scaled")]
    public void ToneMapperId_MatchesMode(ToneMappingMode mode, string expectedId)
    {
        IToneMapper mapper = mode switch
        {
            ToneMappingMode.DirectScRgb => new ToneMapperDirectScRgb(),
            ToneMappingMode.ReferenceWhiteScaled => new ToneMapperReferenceWhiteScaled(),
            _ => throw new NotSupportedException()
        };

        Assert.Equal(expectedId, mapper.Id);
    }
}
