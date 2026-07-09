using System.Runtime.InteropServices;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture.Native;

/// <summary>
/// Win32 window and DWM geometry helpers.
/// </summary>
internal static class WindowGeometryNative
{
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetClientRect(nint hWnd, out RECT lpRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ClientToScreen(nint hWnd, ref POINT lpPoint);

    [DllImport("dwmapi.dll", PreserveSig = false)]
    public static extern void DwmGetWindowAttribute(nint hWnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

    [DllImport("dwmapi.dll")]
    public static extern int DwmFlush();
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public int Width => Right - Left;
    public int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT
{
    public int X;
    public int Y;
}
