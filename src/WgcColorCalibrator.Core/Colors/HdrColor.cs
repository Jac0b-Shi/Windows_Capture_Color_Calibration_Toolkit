using System.Numerics;

namespace WgcColorCalibrator.Core.Colors;

/// <summary>
/// Represents a high-dynamic-range linear RGB color. Values may exceed 1.0 and must be finite.
/// </summary>
public readonly record struct HdrColor(float R, float G, float B)
{
    public HdrColor(Vector3 rgb)
        : this(rgb.X, rgb.Y, rgb.Z)
    {
    }

    public HdrColor(float scalar)
        : this(scalar, scalar, scalar)
    {
    }

    public bool IsFinite => float.IsFinite(R) && float.IsFinite(G) && float.IsFinite(B);

    public bool IsNonNegative => R >= 0.0f && G >= 0.0f && B >= 0.0f;

    public Vector3 ToVector3() => new(R, G, B);

    public Vector4 ToVector4(float alpha = 1.0f) => new(R, G, B, alpha);
}
