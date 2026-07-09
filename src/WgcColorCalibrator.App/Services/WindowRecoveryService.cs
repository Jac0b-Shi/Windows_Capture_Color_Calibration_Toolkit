using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Provides last-resort recovery for owner HWND state after a system picker or modal dialog.
/// </summary>
public static class WindowRecoveryService
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowEnabled(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnableWindow(nint hWnd, [MarshalAs(UnmanagedType.Bool)] bool bEnable);

    /// <summary>
    /// If the specified window is disabled, re-enable it. This is intended as a recovery
    /// mechanism when a system picker or modal flyout leaves the owner window in a disabled state.
    /// </summary>
    public static void RecoverIfDisabled(nint hWnd)
    {
        if (hWnd == 0)
        {
            return;
        }

        if (!IsWindowEnabled(hWnd))
        {
            _ = EnableWindow(hWnd, true);
        }
    }

    /// <summary>
    /// Activates the window and recovers its HWND enabled state.
    /// </summary>
    public static void ActivateAndRecover(Window? window)
    {
        window?.Activate();

        if (window is null)
        {
            return;
        }

        nint hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        RecoverIfDisabled(hWnd);
    }
}
