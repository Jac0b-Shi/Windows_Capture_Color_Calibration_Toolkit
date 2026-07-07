using System.Numerics;
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
}
