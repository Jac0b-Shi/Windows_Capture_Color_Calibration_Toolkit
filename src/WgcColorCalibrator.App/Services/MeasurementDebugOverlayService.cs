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
        byte[] overlay = CreateBgra8Overlay(frame);

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

    private static byte[] CreateBgra8Overlay(CapturedFrame frame)
    {
        return frame.PixelFormat switch
        {
            CapturePixelFormat.B8G8R8A8UIntNormalized => CreateBgra8OverlayFromBgra8(frame),
            CapturePixelFormat.R16G16B16A16Float => CreateBgra8OverlayFromRgba16Float(frame),
            _ => throw new NotSupportedException($"Debug overlay is not supported for pixel format '{frame.PixelFormat}'.")
        };
    }

    private static byte[] CreateBgra8OverlayFromBgra8(CapturedFrame frame)
    {
        byte[] overlay = new byte[frame.ContentPixels.Length];
        frame.ContentPixels.CopyTo(overlay, 0);
        return overlay;
    }

    private static byte[] CreateBgra8OverlayFromRgba16Float(CapturedFrame frame)
    {
        int width = frame.ContentSize.Width;
        int height = frame.ContentSize.Height;
        byte[] overlay = new byte[width * height * 4];
        double maxObserved = 0.0;

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * frame.PackedRowStride;
            for (int x = 0; x < width; x++)
            {
                int offset = rowStart + x * 8;
                float r = ReadHalf(frame.ContentPixels, offset);
                float g = ReadHalf(frame.ContentPixels, offset + 2);
                float b = ReadHalf(frame.ContentPixels, offset + 4);
                maxObserved = Math.Max(maxObserved, Math.Max(r, Math.Max(g, b)));
            }
        }

        double logMax = Math.Log2(1.0 + maxObserved);

        for (int y = 0; y < height; y++)
        {
            int rowStart = y * frame.PackedRowStride;
            int overlayRowStart = y * width * 4;
            for (int x = 0; x < width; x++)
            {
                int offset = rowStart + x * 8;
                int overlayOffset = overlayRowStart + x * 4;
                float r = ReadHalf(frame.ContentPixels, offset);
                float g = ReadHalf(frame.ContentPixels, offset + 2);
                float b = ReadHalf(frame.ContentPixels, offset + 4);
                double luminance = Math.Max(r, Math.Max(g, b));
                double normalized = logMax > 0.0 ? Math.Log2(1.0 + luminance) / logMax : 0.0;
                byte gray = (byte)Math.Clamp((int)Math.Round(normalized * 255.0, MidpointRounding.AwayFromZero), 0, 255);
                overlay[overlayOffset] = gray;
                overlay[overlayOffset + 1] = gray;
                overlay[overlayOffset + 2] = gray;
                overlay[overlayOffset + 3] = 0xFF;
            }
        }

        return overlay;
    }

    private static float ReadHalf(byte[] pixels, int offset)
    {
        return (float)BitConverter.ToHalf(pixels, offset);
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
