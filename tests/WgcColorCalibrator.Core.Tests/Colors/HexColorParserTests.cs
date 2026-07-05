using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Tests.Colors;

public sealed class HexColorParserTests
{
    [Theory]
    [InlineData("#FFFFFF", 255, 255, 255)]
    [InlineData("000000", 0, 0, 0)]
    [InlineData("#0f8", 0, 255, 136)]
    [InlineData("336699", 51, 102, 153)]
    public void ParseRgb8_AcceptsSupportedHexForms(string input, byte r, byte g, byte b)
    {
        Rgb8 color = HexColorParser.ParseRgb8(input);

        Assert.Equal(new Rgb8(r, g, b), color);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#12")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public void TryParseRgb8_RejectsInvalidValues(string input)
    {
        Assert.False(HexColorParser.TryParseRgb8(input, out _));
    }

    [Fact]
    public void ToHexString_UsesUppercaseRgbOrder()
    {
        Assert.Equal("#0C22FF", new Rgb8(12, 34, 255).ToHexString());
    }
}

