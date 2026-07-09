using System.Runtime.InteropServices;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Capture.WindowsGraphicsCapture.Native;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture;

/// <summary>
/// Probes native window geometry using Win32/DWM APIs.
/// </summary>
internal sealed class WindowsWindowGeometryProbe : IWindowGeometryProbe
{
    public WindowGeometrySnapshot Capture(nint hwnd)
    {
        if (hwnd == 0)
        {
            throw new ArgumentException("Window handle cannot be zero.", nameof(hwnd));
        }

        if (!WindowGeometryNative.GetWindowRect(hwnd, out RECT windowRect))
        {
            throw new InvalidOperationException("GetWindowRect failed.");
        }

        if (!WindowGeometryNative.GetClientRect(hwnd, out RECT clientRect))
        {
            throw new InvalidOperationException("GetClientRect failed.");
        }

        POINT clientOrigin = default;
        if (!WindowGeometryNative.ClientToScreen(hwnd, ref clientOrigin))
        {
            throw new InvalidOperationException("ClientToScreen failed.");
        }

        ScreenRectInt window = new(windowRect.Left, windowRect.Top, windowRect.Width, windowRect.Height);
        ScreenRectInt clientInScreen = new(clientOrigin.X, clientOrigin.Y, clientRect.Width, clientRect.Height);
        ScreenRectInt? extended = TryGetExtendedFrameBounds(hwnd);

        return new WindowGeometrySnapshot(window, extended, clientInScreen);
    }

    private static ScreenRectInt? TryGetExtendedFrameBounds(nint hwnd)
    {
        try
        {
            WindowGeometryNative.DwmGetWindowAttribute(
                hwnd,
                WindowGeometryNative.DWMWA_EXTENDED_FRAME_BOUNDS,
                out RECT rect,
                Marshal.SizeOf<RECT>());

            return new ScreenRectInt(rect.Left, rect.Top, rect.Width, rect.Height);
        }
        catch
        {
            return null;
        }
    }
}
