using System.Runtime.InteropServices;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture.Native;

internal static class NativeMethods
{
    [DllImport(
        "d3d11.dll",
        EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice",
        SetLastError = false,
        ExactSpelling = true,
        CallingConvention = CallingConvention.StdCall)]
    public static extern int CreateDirect3D11DeviceFromDXGIDevice(nint dxgiDevice, out nint direct3DDevice);
}
