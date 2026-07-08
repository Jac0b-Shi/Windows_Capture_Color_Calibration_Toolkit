using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Tests.Rendering;

public sealed class OutputModeResolverTests
{
    [Fact]
    public void Resolve_SdrRequested_ReturnsSdrWithoutWarnings()
    {
        var warnings = new List<string>();
        RenderOutputMode actual = OutputModeResolver.Resolve(
            RenderOutputMode.SdrSrgb,
            new DisplayOutputMetadata("Test", hdrSupported: false, hdrActive: false, 0, 0, 0),
            allowHdrClippingExperiment: false,
            warnings);

        Assert.Equal(RenderOutputMode.SdrSrgb, actual);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Resolve_HdrRequestedOnActiveDisplay_ReturnsRequested()
    {
        var warnings = new List<string>();
        RenderOutputMode actual = OutputModeResolver.Resolve(
            RenderOutputMode.HdrScRgb,
            new DisplayOutputMetadata("Test", hdrSupported: true, hdrActive: true, 1000, 400, 0.1),
            allowHdrClippingExperiment: false,
            warnings);

        Assert.Equal(RenderOutputMode.HdrScRgb, actual);
        Assert.Empty(warnings);
    }

    [Fact]
    public void Resolve_HdrRequestedOnUnsupportedDisplayWithNoClipping_FallsBackToSdr()
    {
        var warnings = new List<string>();
        RenderOutputMode actual = OutputModeResolver.Resolve(
            RenderOutputMode.HdrScRgb,
            new DisplayOutputMetadata("Test", hdrSupported: false, hdrActive: false, 80, 80, 0.5),
            allowHdrClippingExperiment: false,
            warnings);

        Assert.Equal(RenderOutputMode.SdrSrgb, actual);
        Assert.Contains(warnings, w => w.Contains("hdr-display-unsupported"));
        Assert.Contains(warnings, w => w.Contains("hdr-fallback-to-sdr"));
    }

    [Fact]
    public void Resolve_HdrRequestedOnUnsupportedDisplayWithClipping_AllowsExperiment()
    {
        var warnings = new List<string>();
        RenderOutputMode actual = OutputModeResolver.Resolve(
            RenderOutputMode.HdrScRgb,
            new DisplayOutputMetadata("Test", hdrSupported: false, hdrActive: false, 80, 80, 0.5),
            allowHdrClippingExperiment: true,
            warnings);

        Assert.Equal(RenderOutputMode.HdrScRgb, actual);
        Assert.Contains(warnings, w => w.Contains("hdr-clipping-experiment"));
    }

    [Fact]
    public void Resolve_HdrRequestedSystemHdrOffWithNoClipping_FallsBackToSdr()
    {
        var warnings = new List<string>();
        RenderOutputMode actual = OutputModeResolver.Resolve(
            RenderOutputMode.HdrScRgb,
            new DisplayOutputMetadata("Test", hdrSupported: true, hdrActive: false, 1000, 400, 0.1),
            allowHdrClippingExperiment: false,
            warnings);

        Assert.Equal(RenderOutputMode.SdrSrgb, actual);
        Assert.Contains(warnings, w => w.Contains("system-hdr-disabled"));
        Assert.Contains(warnings, w => w.Contains("hdr-fallback-to-sdr"));
    }

    [Fact]
    public void ResolveDetailed_HdrFallback_RecordsRequestedAndActualModes()
    {
        var warnings = new List<string>();
        var metadata = new DisplayOutputMetadata("Test", hdrSupported: true, hdrActive: false, 1000, 400, 0.1);
        OutputModeResolution resolution = OutputModeResolver.ResolveDetailed(
            RenderOutputMode.HdrScRgb,
            metadata,
            allowHdrClippingExperiment: false,
            warnings);

        Assert.Equal(RenderOutputMode.HdrScRgb, resolution.RequestedMode);
        Assert.Equal(RenderOutputMode.SdrSrgb, resolution.ActualMode);
        Assert.Equal(metadata, resolution.DisplayOutput);
        Assert.Contains(resolution.Warnings, w => w.Contains("system-hdr-disabled"));
    }

    [Fact]
    public void ResolveDetailed_HdrActive_RequestedEqualsActual()
    {
        var warnings = new List<string>();
        var metadata = new DisplayOutputMetadata("Test", hdrSupported: true, hdrActive: true, 1000, 400, 0.1);
        OutputModeResolution resolution = OutputModeResolver.ResolveDetailed(
            RenderOutputMode.HdrScRgb,
            metadata,
            allowHdrClippingExperiment: false,
            warnings);

        Assert.Equal(RenderOutputMode.HdrScRgb, resolution.RequestedMode);
        Assert.Equal(RenderOutputMode.HdrScRgb, resolution.ActualMode);
        Assert.Empty(resolution.Warnings);
    }
}
