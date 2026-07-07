using System.Runtime.InteropServices;
using Vortice.DXGI;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Native COM interface used to bind a DXGI swap chain to a WinUI 3 SwapChainPanel.
/// </summary>
[ComImport]
[Guid("63AAD0B8-7C24-4469-8518-A71A07A2461F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface ISwapChainPanelNative
{
    [PreserveSig]
    int SetSwapChain(IDXGISwapChain1 swapChain);
}

internal static class SwapChainPanelNative
{
    public static ISwapChainPanelNative FromObject(object panel)
    {
        ArgumentNullException.ThrowIfNull(panel);

        var native = panel as ISwapChainPanelNative
                     ?? throw new Direct3D11RenderingException("Host does not implement ISwapChainPanelNative. Ensure it is a SwapChainPanel.");
        return native;
    }
}
