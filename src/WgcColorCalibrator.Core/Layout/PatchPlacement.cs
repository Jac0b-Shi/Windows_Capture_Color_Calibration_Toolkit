namespace WgcColorCalibrator.Core.Layout;

/// <summary>
/// Records the patch bounds and central safe sample bounds in pixels.
/// </summary>
public sealed record PatchPlacement
{
    public PatchPlacement(string patchId, PixelRect bounds, PixelRect safeSampleBounds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(patchId);

        PatchId = patchId;
        Bounds = bounds;
        SafeSampleBounds = safeSampleBounds;
    }

    public string PatchId { get; }

    public PixelRect Bounds { get; }

    public PixelRect SafeSampleBounds { get; }
}
