using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Measurements;

namespace WgcColorCalibrator.Core.Capture;

/// <summary>
/// Probes the window geometry of a native window handle.
/// Implementation lives in the platform-specific capture project.
/// </summary>
public interface IWindowGeometryProbe
{
    WindowGeometrySnapshot Capture(nint hwnd);
}
