using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using WgcColorCalibrator.Capture.WindowsGraphicsCapture.Native;
using Windows.Graphics.DirectX.Direct3D11;
using WinRT;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture;

/// <summary>
/// Creates and owns a lightweight D3D11 device used only for WGC capture and readback.
/// </summary>
internal sealed class D3D11CaptureDevice : IDisposable
{
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly IDirect3DDevice _winrtDevice;
    private bool _disposed;

    public D3D11CaptureDevice()
    {
        _device = D3D11CreateDeviceForCapture();
        _context = _device.ImmediateContext;
        _winrtDevice = CreateWinRtDevice(_device);
    }

    public ID3D11Device Device => _device;

    public ID3D11DeviceContext Context => _context;

    public IDirect3DDevice WinrtDevice => _winrtDevice;

    private static ID3D11Device D3D11CreateDeviceForCapture()
    {
        DeviceCreationFlags flags = DeviceCreationFlags.BgraSupport;

        D3D11.D3D11CreateDevice(
            null!,
            DriverType.Hardware,
            flags,
            null!,
            out ID3D11Device device,
            out ID3D11DeviceContext _).CheckError();

        return device;
    }

    private static IDirect3DDevice CreateWinRtDevice(ID3D11Device d3dDevice)
    {
        using IDXGIDevice dxgiDevice = d3dDevice.QueryInterface<IDXGIDevice>();
        int hr = NativeMethods.CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer, out nint pUnknown);

        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return MarshalInterface<IDirect3DDevice>.FromAbi(pUnknown);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context?.Dispose();
        _winrtDevice?.Dispose();
        _device?.Dispose();
        _disposed = true;
    }
}
