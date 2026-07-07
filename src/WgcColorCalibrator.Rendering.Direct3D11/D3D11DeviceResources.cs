using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Holds the D3D11 device and immediate context used by all renderers.
/// </summary>
public sealed class D3D11DeviceResources : IDisposable
{
    private readonly ID3D11Device _device;
    private readonly ID3D11DeviceContext _context;
    private readonly IDXGIFactory2 _factory;
    private bool _disposed;

    public D3D11DeviceResources(ID3D11Device device, ID3D11DeviceContext context, IDXGIFactory2 factory)
    {
        _device = device ?? throw new ArgumentNullException(nameof(device));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
    }

    public ID3D11Device Device => _device;

    public ID3D11DeviceContext Context => _context;

    public IDXGIFactory2 Factory => _factory;

    public static D3D11DeviceResources Create()
    {
        D3D11.D3D11CreateDevice(
            null!,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            null!,
            out ID3D11Device device,
            out ID3D11DeviceContext context).CheckError();

        IDXGIDevice? dxgiDevice = device.QueryInterface<IDXGIDevice>();
        if (dxgiDevice is null)
        {
            device.Dispose();
            context.Dispose();
            throw new Direct3D11RenderingException("Failed to query IDXGIDevice from D3D11 device.");
        }

        IDXGIAdapter? adapter = dxgiDevice.GetAdapter();
        dxgiDevice.Dispose();
        if (adapter is null)
        {
            device.Dispose();
            context.Dispose();
            throw new Direct3D11RenderingException("Failed to get DXGI adapter from device.");
        }

        IDXGIFactory2? factory = adapter.GetParent<IDXGIFactory2>();
        adapter.Dispose();
        if (factory is null)
        {
            device.Dispose();
            context.Dispose();
            throw new Direct3D11RenderingException("Failed to get DXGI factory from adapter.");
        }

        return new D3D11DeviceResources(device, context, factory);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _context.Dispose();
        _device.Dispose();
        _factory.Dispose();
        _disposed = true;
    }
}
