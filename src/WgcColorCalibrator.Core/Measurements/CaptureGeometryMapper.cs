using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Measurements;

/// <summary>
/// Pure math mapping from captured frame coordinates to chart content coordinates.
/// </summary>
public static class CaptureGeometryMapper
{
    public static CaptureGeometry Map(
        WindowGeometrySnapshot before,
        WindowGeometrySnapshot after,
        CapturedFrame frame,
        PixelPoint chartContentOrigin,
        int sizeTolerancePixels = 1)
    {
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(after);
        ArgumentNullException.ThrowIfNull(frame);

        List<string> warnings = new();

        if (GeometryChanged(before, after))
        {
            warnings.Add("capture-geometry-changed");
            return new CaptureGeometry(before, after, CaptureFrameOriginBasis.Unverified, CaptureMappingStatus.Unverified, PixelPoint.Zero, warnings);
        }

        CaptureFrameOriginBasis basis = DetermineBasis(before, after, frame, chartContentOrigin, sizeTolerancePixels, out PixelPoint? contentOffset, warnings);

        if (contentOffset.HasValue)
        {
            return new CaptureGeometry(
                before,
                after,
                basis,
                basis == CaptureFrameOriginBasis.Unverified ? CaptureMappingStatus.Unverified : CaptureMappingStatus.Verified,
                contentOffset.Value,
                warnings);
        }

        return new CaptureGeometry(before, after, CaptureFrameOriginBasis.Unverified, CaptureMappingStatus.Unverified, PixelPoint.Zero, warnings);
    }

    private static bool GeometryChanged(WindowGeometrySnapshot before, WindowGeometrySnapshot after)
    {
        return before.WindowRect != after.WindowRect ||
               before.ExtendedFrameBounds != after.ExtendedFrameBounds ||
               before.ClientRectInScreen != after.ClientRectInScreen;
    }

    private static CaptureFrameOriginBasis DetermineBasis(
        WindowGeometrySnapshot before,
        WindowGeometrySnapshot after,
        CapturedFrame frame,
        PixelPoint chartContentOrigin,
        int sizeTolerance,
        out PixelPoint? contentOffset,
        List<string> warnings)
    {
        contentOffset = null;
        var candidates = new List<(CaptureFrameOriginBasis Basis, ScreenRectInt Rect)>
        {
            (CaptureFrameOriginBasis.WindowRect, before.WindowRect),
            (CaptureFrameOriginBasis.ClientRect, before.ClientRectInScreen)
        };

        if (before.ExtendedFrameBounds.HasValue)
        {
            candidates.Add((CaptureFrameOriginBasis.ExtendedFrameBounds, before.ExtendedFrameBounds.Value));
        }

        List<(CaptureFrameOriginBasis Basis, PixelPoint Offset)> matches = new();

        foreach ((CaptureFrameOriginBasis basis, ScreenRectInt rect) in candidates)
        {
            if (!IsSizeMatch(rect, frame, sizeTolerance))
            {
                continue;
            }

            PixelPoint? offset = ComputeContentOffset(rect, before.ClientRectInScreen, chartContentOrigin);
            if (offset.HasValue)
            {
                matches.Add((basis, offset.Value));
            }
        }

        if (matches.Count == 0)
        {
            warnings.Add("capture-frame-origin-unverified");
            return CaptureFrameOriginBasis.Unverified;
        }

        PixelPoint firstOffset = matches[0].Offset;
        for (int i = 1; i < matches.Count; i++)
        {
            if (matches[i].Offset != firstOffset)
            {
                warnings.Add("capture-frame-origin-unverified");
                return CaptureFrameOriginBasis.Unverified;
            }
        }

        contentOffset = firstOffset;
        return SelectPreferredBasis(matches.Select(m => m.Basis).ToList());
    }

    private static bool IsSizeMatch(ScreenRectInt rect, SizeInt size, int sizeTolerance)
    {
        return Math.Abs(rect.Width - size.Width) <= sizeTolerance &&
               Math.Abs(rect.Height - size.Height) <= sizeTolerance;
    }

    private static bool IsSizeMatch(ScreenRectInt rect, CapturedFrame frame, int sizeTolerance)
    {
        return IsSizeMatch(rect, frame.ContentSize, sizeTolerance) ||
               IsSizeMatch(rect, frame.SurfaceSize, sizeTolerance) ||
               IsSizeMatch(rect, frame.CaptureItemSize, sizeTolerance);
    }

    private static PixelPoint? ComputeContentOffset(ScreenRectInt frameOriginRect, ScreenRectInt clientRectInScreen, PixelPoint chartContentOrigin)
    {
        int clientOffsetX = clientRectInScreen.X - frameOriginRect.X;
        int clientOffsetY = clientRectInScreen.Y - frameOriginRect.Y;
        return new PixelPoint(clientOffsetX + chartContentOrigin.X, clientOffsetY + chartContentOrigin.Y);
    }

    private static CaptureFrameOriginBasis SelectPreferredBasis(IReadOnlyList<CaptureFrameOriginBasis> matchingBases)
    {
        if (matchingBases.Contains(CaptureFrameOriginBasis.ClientRect))
        {
            return CaptureFrameOriginBasis.ClientRect;
        }

        if (matchingBases.Contains(CaptureFrameOriginBasis.ExtendedFrameBounds))
        {
            return CaptureFrameOriginBasis.ExtendedFrameBounds;
        }

        if (matchingBases.Contains(CaptureFrameOriginBasis.WindowRect))
        {
            return CaptureFrameOriginBasis.WindowRect;
        }

        return CaptureFrameOriginBasis.Unverified;
    }
}
