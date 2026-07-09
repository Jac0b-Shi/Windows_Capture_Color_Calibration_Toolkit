using System.Runtime.InteropServices;
using Windows.Graphics.Capture;
using WinRT;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture;

/// <summary>
/// Creates a GraphicsCaptureItem from a window handle.
/// </summary>
internal static class GraphicsCaptureItemFactory
{
    private static readonly Guid GraphicsCaptureItemGuid = new("79C3F95B-31F7-4EC2-A464-632EF5D30760");

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        int CreateForWindow(nint window, ref Guid iid, out nint result);

        int CreateForMonitor(nint monitor, ref Guid iid, out nint result);
    }

    public static GraphicsCaptureItem CreateForWindow(nint hwnd)
    {
        IObjectReference factory = ActivationFactory.Get("Windows.Graphics.Capture.GraphicsCaptureItem");
        IGraphicsCaptureItemInterop interop = factory.AsInterface<IGraphicsCaptureItemInterop>();

        Guid iid = GraphicsCaptureItemGuid;
        int hr = interop.CreateForWindow(hwnd, ref iid, out nint itemPointer);
        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return GraphicsCaptureItem.FromAbi(itemPointer);
    }
}
