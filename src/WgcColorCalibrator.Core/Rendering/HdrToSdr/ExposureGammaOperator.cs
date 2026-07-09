using System;
using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Rendering.HdrToSdr;

/// <summary>
/// Applies an exposure scale followed by a gamma-like curve:
/// out = pow(1 - exp(-x * exposure), 1 / gamma).
/// </summary>
public sealed class ExposureGammaOperator : IHdrToSdrOperator
{
    public ExposureGammaOperator(float exposure, float gamma)
    {
        if (!float.IsFinite(exposure) || exposure <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(exposure), "Exposure must be a positive finite value.");
        }

        if (!float.IsFinite(gamma) || gamma <= 0.0f)
        {
            throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be a positive finite value.");
        }

        Exposure = exposure;
        Gamma = gamma;
    }

    public string Id => "exposure-gamma";

    public float Exposure { get; }

    public float Gamma { get; }

    public RgbaFloat Map(RgbaFloat hdr)
    {
        return new RgbaFloat(
            Apply(hdr.R),
            Apply(hdr.G),
            Apply(hdr.B),
            hdr.A);
    }

    private float Apply(float value)
    {
        if (value <= 0.0f)
        {
            return 0.0f;
        }

        float scaled = value * Exposure;
        float compressed = 1.0f - (float)Math.Exp(-scaled);
        return (float)Math.Pow(compressed, 1.0 / Gamma);
    }
}
