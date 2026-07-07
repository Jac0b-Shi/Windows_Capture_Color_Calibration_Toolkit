using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Tests.Layout;

public class LayoutValidationTests
{
    [Theory]
    [InlineData(0, 64, "ValidationErrorInvalidPatchWidth")]
    [InlineData(64, 0, "ValidationErrorInvalidPatchHeight")]
    [InlineData(-1, 64, "ValidationErrorInvalidPatchWidth")]
    public void ValidateLayoutParameters_RejectsInvalidPatchSize(int patchWidth, int patchHeight, string expectedKey)
    {
        string? actual = LayoutValidation.ValidateLayoutParameters(patchWidth, patchHeight, 0, 0, 0, 1);
        Assert.Equal(expectedKey, actual);
    }

    [Theory]
    [InlineData(64, 64, -1, 0, 0, 1, "ValidationErrorInvalidGap")]
    [InlineData(64, 64, 0, -1, 0, 1, "ValidationErrorInvalidBorder")]
    [InlineData(64, 64, 0, 0, -1, 1, "ValidationErrorInvalidSafeSampleInset")]
    [InlineData(64, 64, 0, 0, 0, 0, "ValidationErrorInvalidColumnCount")]
    [InlineData(64, 64, 0, 0, 32, 1, "ValidationErrorSafeSampleInsetTooLarge")]
    [InlineData(64, 64, 0, 0, 64, 1, "ValidationErrorSafeSampleInsetTooLarge")]
    public void ValidateLayoutParameters_RejectsInvalidValues(
        int patchWidth,
        int patchHeight,
        int gap,
        int border,
        int safeSampleInset,
        int columnCount,
        string expectedKey)
    {
        string? actual = LayoutValidation.ValidateLayoutParameters(patchWidth, patchHeight, gap, border, safeSampleInset, columnCount);
        Assert.Equal(expectedKey, actual);
    }

    [Fact]
    public void ValidateLayoutParameters_ValidLayout_ReturnsNull()
    {
        string? actual = LayoutValidation.ValidateLayoutParameters(64, 64, 8, 16, 8, 4);
        Assert.Null(actual);
    }

    [Fact]
    public void ValidateLayoutParameters_SafeSampleInsetAtLimit_ReturnsNull()
    {
        // Inset of 31 leaves 2 pixels width for a 64-pixel patch (31*2 = 62 < 64).
        string? actual = LayoutValidation.ValidateLayoutParameters(64, 64, 0, 0, 31, 1);
        Assert.Null(actual);
    }
}
