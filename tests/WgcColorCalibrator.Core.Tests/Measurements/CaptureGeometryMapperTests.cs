using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Tests.Measurements;

public sealed class CaptureGeometryMapperTests
{
    [Fact]
    public void Map_GeometryChanged_ReturnsUnverified()
    {
        WindowGeometrySnapshot before = CreateSnapshot(0, 0, 100, 100);
        WindowGeometrySnapshot after = CreateSnapshot(1, 0, 100, 100);
        CapturedFrame frame = CreateFrame(100, 100);

        CaptureGeometry geometry = CaptureGeometryMapper.Map(before, after, frame, new PixelPoint(0, 0));

        Assert.Equal(CaptureMappingStatus.Unverified, geometry.MappingStatus);
        Assert.Contains("capture-geometry-changed", geometry.Warnings);
    }

    [Fact]
    public void Map_ClientRectMatchesFrame_ReturnsVerified()
    {
        WindowGeometrySnapshot before = CreateSnapshot(0, 0, 100, 100);
        CapturedFrame frame = CreateFrame(100, 100);

        CaptureGeometry geometry = CaptureGeometryMapper.Map(before, before, frame, new PixelPoint(0, 0));

        Assert.Equal(CaptureMappingStatus.Verified, geometry.MappingStatus);
        Assert.Equal(CaptureFrameOriginBasis.ClientRect, geometry.FrameOriginBasis);
    }

    [Fact]
    public void Map_ClientRectOffset_ComputesContentOffset()
    {
        WindowGeometrySnapshot before = CreateSnapshotWithClient(
            windowX: 0, windowY: 0, windowWidth: 200, windowHeight: 150,
            clientX: 20, clientY: 30, clientWidth: 160, clientHeight: 90);
        CapturedFrame frame = CreateFrame(160, 90);

        CaptureGeometry geometry = CaptureGeometryMapper.Map(before, before, frame, new PixelPoint(5, 10));

        Assert.Equal(CaptureMappingStatus.Verified, geometry.MappingStatus);
        Assert.Equal(CaptureFrameOriginBasis.ClientRect, geometry.FrameOriginBasis);
        Assert.Equal(new PixelPoint(5, 10), geometry.ContentOffset);
    }

    [Fact]
    public void Map_AmbiguousCandidates_ReturnsUnverified()
    {
        WindowGeometrySnapshot before = CreateSnapshotWithClient(
            windowX: 0, windowY: 0, windowWidth: 200, windowHeight: 150,
            clientX: 0, clientY: 0, clientWidth: 180, clientHeight: 90);
        before = before with
        {
            ExtendedFrameBounds = new ScreenRectInt(5, 5, 180, 90)
        };

        CapturedFrame frame = CreateFrame(180, 90);

        CaptureGeometry geometry = CaptureGeometryMapper.Map(before, before, frame, new PixelPoint(0, 0));

        Assert.Equal(CaptureMappingStatus.Unverified, geometry.MappingStatus);
        Assert.Contains("capture-frame-origin-unverified", geometry.Warnings);
    }

    [Fact]
    public void Map_WindowRectSmallerThanContent_UsesClientRect()
    {
        WindowGeometrySnapshot before = CreateSnapshotWithClient(0, 0, 100, 100, 8, 31, 84, 61);
        CapturedFrame frame = CreateFrame(84, 61);

        CaptureGeometry geometry = CaptureGeometryMapper.Map(before, before, frame, new PixelPoint(0, 0));

        Assert.Equal(CaptureMappingStatus.Verified, geometry.MappingStatus);
        Assert.Equal(CaptureFrameOriginBasis.ClientRect, geometry.FrameOriginBasis);
    }

    private static WindowGeometrySnapshot CreateSnapshot(int x, int y, int width, int height)
    {
        return CreateSnapshotWithClient(x, y, width, height, x, y, width, height);
    }

    private static WindowGeometrySnapshot CreateSnapshotWithClient(
        int windowX, int windowY, int windowWidth, int windowHeight,
        int clientX, int clientY, int clientWidth, int clientHeight)
    {
        return new WindowGeometrySnapshot(
            new ScreenRectInt(windowX, windowY, windowWidth, windowHeight),
            null,
            new ScreenRectInt(clientX, clientY, clientWidth, clientHeight));
    }

    private static CapturedFrame CreateFrame(int width, int height)
    {
        return new CapturedFrame(
            new SizeInt(width, height),
            new SizeInt(width, height),
            new SizeInt(width, height),
            CapturePixelFormat.B8G8R8A8UIntNormalized,
            width * 4,
            new byte[width * height * 4],
            CaptureSourceKind.Window,
            null,
            DateTimeOffset.UtcNow,
            []);
    }
}
