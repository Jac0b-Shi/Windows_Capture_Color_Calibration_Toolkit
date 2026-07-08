namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Resolves the requested output mode to an actual output mode based on display metadata and user policy.
/// </summary>
public static class OutputModeResolver
{
    /// <summary>
    /// Resolves <paramref name="requested"/> to the mode that will actually be rendered.
    /// Warnings are appended to <paramref name="warnings"/> to explain any deviation.
    /// </summary>
    public static RenderOutputMode Resolve(
        RenderOutputMode requested,
        DisplayOutputMetadata metadata,
        bool allowHdrClippingExperiment,
        ICollection<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(warnings);

        if (requested == RenderOutputMode.SdrSrgb)
        {
            return requested;
        }

        if (!metadata.HdrSupported)
        {
            warnings.Add($"hdr-display-unsupported: requested {requested}, display reports no HDR capability");
            if (!allowHdrClippingExperiment)
            {
                warnings.Add("hdr-fallback-to-sdr: clipping experiment not allowed");
                return RenderOutputMode.SdrSrgb;
            }

            warnings.Add("hdr-clipping-experiment: rendering HDR on a display that reports no HDR capability");
            return requested;
        }

        if (!metadata.HdrActive)
        {
            warnings.Add($"system-hdr-disabled: requested {requested} but system HDR is off");
            if (!allowHdrClippingExperiment)
            {
                warnings.Add("hdr-fallback-to-sdr: clipping experiment not allowed");
                return RenderOutputMode.SdrSrgb;
            }

            warnings.Add("hdr-clipping-experiment: rendering HDR while system HDR is off");
            return requested;
        }

        if (requested == RenderOutputMode.Hdr10)
        {
            warnings.Add("hdr10-experimental: HDR10 is experimental and not yet fully validated");
        }

        return requested;
    }
}
