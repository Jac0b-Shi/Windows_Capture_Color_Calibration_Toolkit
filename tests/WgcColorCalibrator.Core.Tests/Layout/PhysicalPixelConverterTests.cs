using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Tests.Layout;

public class PhysicalPixelConverterTests
{
    [Theory]
    [InlineData(64, 1.0, 64.0)]
    [InlineData(64, 1.25, 51.2)]
    [InlineData(64, 1.5, 42.666666666666664)]
    [InlineData(64, 1.75, 36.571428571428569)]
    [InlineData(64, 2.0, 32.0)]
    public void ToDip_ReturnsExpectedValues(int physicalPixels, double scale, double expected)
    {
        double actual = PhysicalPixelConverter.ToDip(physicalPixels, scale);
        Assert.Equal(expected, actual, 1e-10);
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(1.25)]
    [InlineData(1.5)]
    [InlineData(1.75)]
    [InlineData(2.0)]
    public void ToPhysicalPixels_RoundTripWithinTolerance(double scale)
    {
        int original = 64;
        double dip = PhysicalPixelConverter.ToDip(original, scale);
        int roundTripped = PhysicalPixelConverter.ToPhysicalPixels(dip, scale);
        Assert.Equal(original, roundTripped);
    }

    [Fact]
    public void ToDip_NonIntegerDip_ReturnsFractionalValue()
    {
        double dip = PhysicalPixelConverter.ToDip(100, 1.5);
        Assert.Equal(66.66666666666667, dip, 1e-10);
    }

    [Fact]
    public void ToPhysicalPixels_RoundsHalfUp()
    {
        int physical = PhysicalPixelConverter.ToPhysicalPixels(50.5, 1.0);
        Assert.Equal(51, physical);
    }
}
