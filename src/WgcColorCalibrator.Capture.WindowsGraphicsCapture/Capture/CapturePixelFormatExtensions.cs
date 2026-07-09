using Windows.Graphics.DirectX;
using WgcColorCalibrator.Core.Measurements;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture;

/// <summary>
/// Maps logical <see cref="CapturePixelFormat"/> values to WinRT DirectX pixel formats.
/// </summary>
internal static class CapturePixelFormatExtensions
{
    public static DirectXPixelFormat ToDirectXPixelFormat(this CapturePixelFormat format)
    {
        return format switch
        {
            CapturePixelFormat.B8G8R8A8UIntNormalized => DirectXPixelFormat.B8G8R8A8UIntNormalized,
            CapturePixelFormat.R16G16B16A16Float => DirectXPixelFormat.R16G16B16A16Float,
            _ => throw new NotSupportedException($"Capture pixel format '{format}' is not supported.")
        };
    }

    public static int GetBytesPerPixel(this CapturePixelFormat format)
    {
        return format switch
        {
            CapturePixelFormat.B8G8R8A8UIntNormalized => 4,
            CapturePixelFormat.R16G16B16A16Float => 8,
            _ => throw new NotSupportedException($"Capture pixel format '{format}' is not supported.")
        };
    }
}
