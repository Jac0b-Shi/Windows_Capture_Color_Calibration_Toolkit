namespace WgcColorCalibrator.Core.Layout;

/// <summary>
/// Reusable validation for chart layout parameters.
/// </summary>
public static class LayoutValidation
{
    /// <summary>
    /// Validates layout parameters and returns a localized error key if invalid, otherwise null.
    /// </summary>
    public static string? ValidateLayoutParameters(
        int patchWidth,
        int patchHeight,
        int gap,
        int border,
        int safeSampleInset,
        int columnCount)
    {
        if (patchWidth <= 0)
        {
            return "ValidationErrorInvalidPatchWidth";
        }

        if (patchHeight <= 0)
        {
            return "ValidationErrorInvalidPatchHeight";
        }

        if (gap < 0)
        {
            return "ValidationErrorInvalidGap";
        }

        if (border < 0)
        {
            return "ValidationErrorInvalidBorder";
        }

        if (safeSampleInset < 0)
        {
            return "ValidationErrorInvalidSafeSampleInset";
        }

        if ((safeSampleInset * 2) >= patchWidth || (safeSampleInset * 2) >= patchHeight)
        {
            return "ValidationErrorSafeSampleInsetTooLarge";
        }

        if (columnCount <= 0)
        {
            return "ValidationErrorInvalidColumnCount";
        }

        return null;
    }
}
