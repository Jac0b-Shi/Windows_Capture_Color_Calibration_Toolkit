using System.Runtime.InteropServices;
using WinRT;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Native COM interface used to bind a DXGI swap chain
/// to a WinUI 3 Microsoft.UI.Xaml.Controls.SwapChainPanel.
/// </summary>
[ComImport]
[Guid("63AAD0B8-7C24-40FF-85A8-640D944CC325")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISwapChainPanelNative
{
    [PreserveSig]
    int SetSwapChain(nint swapChain);
}

internal static class SwapChainPanelNative
{
    public static ISwapChainPanelNative FromObject(object panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        try
        {
            // A normal C# cast cannot discover implementation-only
            // native interfaces on a projected WinRT object.
            return panel.As<ISwapChainPanelNative>();
        }
        catch (Exception ex) when (
            ex is COMException or
            InvalidCastException or
            ArgumentException)
        {
            throw new Direct3D11RenderingException(
                $"Failed to query ISwapChainPanelNative from '{panel.GetType().FullName}'.",
                ex);
        }
    }
}
