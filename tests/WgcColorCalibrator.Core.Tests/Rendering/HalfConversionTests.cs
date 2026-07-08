namespace WgcColorCalibrator.Core.Tests.Rendering;

public sealed class HalfConversionTests
{
    [Theory]
    [InlineData(0.0f, 0)]
    [InlineData(1.0f, 15360)]
    [InlineData(2.0f, 16384)]
    [InlineData(-1.0f, 48128)]
    public void BitConverter_HalfToUInt16Bits_ReturnsExpectedBits(float value, ushort expectedBits)
    {
        ushort actual = System.BitConverter.HalfToUInt16Bits((System.Half)value);
        Assert.Equal(expectedBits, actual);
    }

    [Fact]
    public void BitConverter_RoundTripHalf_PreservesValue()
    {
        float original = 3.5f;
        ushort bits = System.BitConverter.HalfToUInt16Bits((System.Half)original);
        float roundTripped = (float)System.BitConverter.UInt16BitsToHalf(bits);

        Assert.Equal((System.Half)original, (System.Half)roundTripped);
    }
}
