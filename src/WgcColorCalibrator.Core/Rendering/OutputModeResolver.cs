namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Records the resolution of a requested output mode against the display and user policy.
/// </summary>
public sealed record OutputModeResolution(
    RenderOutputMode RequestedMode,
    RenderOutputMode ActualMode,
    DisplayOutputMetadata DisplayOutput,
    IReadOnlyList<string> Warnings);

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

        OutputModeResolution resolution = ResolveDetailed(requested, metadata, allowHdrClippingExperiment, warnings);
        return resolution.ActualMode;
    }

    /// <summary>
    /// Performs a single resolution that captures both the requested and actual output mode, the display metadata,
    /// and any warnings.
    /// </summary>
    public static OutputModeResolution ResolveDetailed(
        RenderOutputMode requested,
        DisplayOutputMetadata metadata,
        bool allowHdrClippingExperiment,
        ICollection<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(warnings);

        RenderOutputMode actual = requested;

        if (requested != RenderOutputMode.SdrSrgb)
        {
            if (!metadata.HdrSupported)
            {
                warnings.Add($"hdr-display-unsupported: requested {requested}, display reports no HDR capability");
                if (!allowHdrClippingExperiment)
                {
                    warnings.Add("hdr-fallback-to-sdr: clipping experiment not allowed");
                    actual = RenderOutputMode.SdrSrgb;
                }
                else
                {
                    warnings.Add("hdr-clipping-experiment: rendering HDR on a display that reports no HDR capability");
                }
            }
            else if (!metadata.HdrActive)
            {
                warnings.Add($"system-hdr-disabled: requested {requested} but system HDR is off");
                if (!allowHdrClippingExperiment)
                {
                    warnings.Add("hdr-fallback-to-sdr: clipping experiment not allowed");
                    actual = RenderOutputMode.SdrSrgb;
                }
                else
                {
                    warnings.Add("hdr-clipping-experiment: rendering HDR while system HDR is off");
                }
            }
            else if (requested == RenderOutputMode.Hdr10)
            {
                warnings.Add("hdr10-experimental: HDR10 is experimental and not yet fully validated");
            }
        }

        return new OutputModeResolution(requested, actual, metadata, warnings.ToList().AsReadOnly());
    }
}
