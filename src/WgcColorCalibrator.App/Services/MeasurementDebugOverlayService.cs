using Windows.Security.Cryptography;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Measurements;

namespace WgcColorCalibrator.App.Services;

public sealed class MeasurementDebugOverlayService
{
    public async Task SaveDebugOverlayAsync(
        CapturedFrame frame,
        CaptureGeometry geometry,
        IReadOnlyList<PatchPlacement> placements,
        StorageFile outputFile,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(geometry);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(outputFile);

        int width = frame.ContentSize.Width;
        int height = frame.ContentSize.Height;
        byte[] overlay = new byte[frame.ContentPixels.Length];
        frame.ContentPixels.CopyTo(overlay);

        int contentLeft = geometry.ContentOffset.X;
        int contentTop = geometry.ContentOffset.Y;
        int contentRight = contentLeft + width;
        int contentBottom = contentTop + height;

        DrawRectangle(overlay, width, height, contentLeft, contentTop, contentRight, contentBottom, 0xFF0000FF);

        foreach (PatchPlacement placement in placements)
        {
            PixelRect patch = placement.Bounds;
            int patchLeft = geometry.ContentOffset.X + patch.X;
            int patchTop = geometry.ContentOffset.Y + patch.Y;
            int patchRight = patchLeft + patch.Width;
            int patchBottom = patchTop + patch.Height;
            DrawRectangle(overlay, width, height, patchLeft, patchTop, patchRight, patchBottom, 0xFFFF0000);

            PixelRect safe = placement.SafeSampleBounds;
            int safeLeft = geometry.ContentOffset.X + safe.X;
            int safeTop = geometry.ContentOffset.Y + safe.Y;
            int safeRight = safeLeft + safe.Width;
            int safeBottom = safeTop + safe.Height;
            DrawRectangle(overlay, width, height, safeLeft, safeTop, safeRight, safeBottom, 0xFF00FF00);
        }

        using IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
        BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
        IBuffer buffer = CryptographicBuffer.CreateFromByteArray(overlay);
        encoder.SetSoftwareBitmap(SoftwareBitmap.CreateCopyFromBuffer(
            buffer,
            BitmapPixelFormat.Bgra8,
            width,
            height,
            BitmapAlphaMode.Premultiplied));
        await encoder.FlushAsync();

        cancellationToken.ThrowIfCancellationRequested();
    }

    private static void DrawRectangle(
        byte[] buffer, int width, int height,
        int left, int top, int right, int bottom, uint color)
    {
        for (int x = left; x <= right; x++)
        {
            DrawPixel(buffer, width, height, x, top, color);
            DrawPixel(buffer, width, height, x, bottom, color);
        }

        for (int y = top; y <= bottom; y++)
        {
            DrawPixel(buffer, width, height, left, y, color);
            DrawPixel(buffer, width, height, right, y, color);
        }
    }

    private static void DrawPixel(byte[] buffer, int width, int height, int x, int y, uint color)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int offset = (y * width + x) * 4;
        buffer[offset] = (byte)(color & 0xFF);
        buffer[offset + 1] = (byte)((color >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((color >> 16) & 0xFF);
        buffer[offset + 3] = (byte)((color >> 24) & 0xFF);
    }
}
