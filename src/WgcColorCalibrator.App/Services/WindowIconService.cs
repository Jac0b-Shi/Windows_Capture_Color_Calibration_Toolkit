using Microsoft.UI.Xaml;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Applies the application's icon to a window's title bar, taskbar, and Alt+Tab preview.
/// </summary>
public static class WindowIconService
{
    public static void ApplyIcon(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);

        string iconPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "AppIcon.ico");

        if (!File.Exists(iconPath))
        {
            System.Diagnostics.Debug.WriteLine($"Window icon not found: {iconPath}");
            return;
        }

        try
        {
            window.AppWindow.SetIcon(iconPath);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to set window icon: {ex.Message}");
        }
    }
}
