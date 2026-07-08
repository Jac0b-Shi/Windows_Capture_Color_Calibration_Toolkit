using System.Runtime.InteropServices;
using SharpGen.Runtime;
using Vortice.Direct3D11;
using Vortice.DXGI;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Probes the display attached to a window using DXGI 1.6 output descriptors.
/// </summary>
public sealed class DisplayOutputProbe : IDisplayOutputProbe
{
    private readonly D3D11DeviceResources _resources;

    public DisplayOutputProbe(D3D11DeviceResources resources)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public DisplayOutputMetadata Probe(nint windowHandle)
    {
        if (windowHandle == IntPtr.Zero)
        {
            return DisplayOutputMetadata.Unknown;
        }

        nint monitor = NativeMethods.MonitorFromWindow(windowHandle, 2); // MONITOR_DEFAULTTONEAREST
        if (monitor == IntPtr.Zero)
        {
            return DisplayOutputMetadata.Unknown;
        }

        IDXGIDevice? dxgiDevice = _resources.Device.QueryInterface<IDXGIDevice>();
        if (dxgiDevice is null)
        {
            return DisplayOutputMetadata.Unknown;
        }

        IDXGIAdapter? adapter;
        try
        {
            adapter = dxgiDevice.GetAdapter();
        }
        finally
        {
            dxgiDevice.Dispose();
        }

        if (adapter is null)
        {
            return DisplayOutputMetadata.Unknown;
        }

        try
        {
            for (uint i = 0; ; i++)
            {
                Result result = adapter.EnumOutputs(i, out IDXGIOutput output);
                if (!result.Success || output is null)
                {
                    break;
                }

                using (output)
                {
                    IDXGIOutput6? output6 = output!.QueryInterface<IDXGIOutput6>();
                    if (output6 is null)
                    {
                        continue;
                    }

                    using (output6)
                    {
                        OutputDescription1 desc = output6.Description1;
                        if (desc.Monitor == monitor)
                        {
                            bool hdrActive =
                                desc.ColorSpace == ColorSpaceType.RgbFullG10NoneP709 ||
                                desc.ColorSpace == ColorSpaceType.RgbFullG2084NoneP2020;
                            bool hdrSupported = desc.MaxLuminance > 80.0f;

                            return new DisplayOutputMetadata(
                                desc.DeviceName,
                                hdrSupported,
                                hdrActive,
                                desc.MaxLuminance,
                                desc.MaxFullFrameLuminance,
                                desc.MinLuminance);
                        }
                    }
                }
            }
        }
        finally
        {
            adapter.Dispose();
        }

        return DisplayOutputMetadata.Unknown;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = false)]
        public static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);
    }
}
