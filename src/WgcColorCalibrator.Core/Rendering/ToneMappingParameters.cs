namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Parameters that control HDR tone mapping and brightness scaling.
/// </summary>
public sealed record ToneMappingParameters
{
    public ToneMappingParameters(double paperWhiteNits, double peakBrightnessNits, double exposureEv)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(paperWhiteNits);
        ArgumentOutOfRangeException.ThrowIfLessThan(peakBrightnessNits, paperWhiteNits);

        if (double.IsNaN(exposureEv) || double.IsInfinity(exposureEv))
        {
            throw new ArgumentOutOfRangeException(nameof(exposureEv), "Exposure EV must be a finite number.");
        }

        PaperWhiteNits = paperWhiteNits;
        PeakBrightnessNits = peakBrightnessNits;
        ExposureEv = exposureEv;
    }

    public double PaperWhiteNits { get; }

    public double PeakBrightnessNits { get; }

    public double ExposureEv { get; }

    public static ToneMappingParameters Default { get; } = new(
        paperWhiteNits: 200.0,
        peakBrightnessNits: 1000.0,
        exposureEv: 0.0);
}
