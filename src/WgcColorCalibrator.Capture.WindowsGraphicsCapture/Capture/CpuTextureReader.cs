using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Windows.Graphics.DirectX.Direct3D11;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Rendering;
using WinRT;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture;

[ComImport]
[Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDirect3DDxgiInterfaceAccess
{
    int GetInterface(ref Guid iid, out nint pInterface);
}

/// <summary>
/// Copies a WGC frame surface to a staging texture and reads the ContentSize region into a packed BGRA buffer.
/// </summary>
internal sealed class CpuTextureReader : IDisposable
{
    private static readonly Guid ID3D11Texture2D = new("6F15AAF2-D208-4E89-9AB4-489535D34F9C");

    private readonly ID3D11DeviceContext _context;
    private ID3D11Texture2D? _stagingTexture;
    private bool _disposed;

    public CpuTextureReader(ID3D11DeviceContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public byte[] ReadContentRegion(IDirect3DSurface surface, SizeInt contentSize)
    {
        ArgumentNullException.ThrowIfNull(surface);

        if (contentSize.Width <= 0 || contentSize.Height <= 0)
        {
            throw new ArgumentException("Content size must be positive.", nameof(contentSize));
        }

        using ID3D11Texture2D sourceTexture = GetNativeTexture(surface);
        EnsureStagingTexture(contentSize.Width, contentSize.Height, sourceTexture.Description.Format);

        if (_stagingTexture is null)
        {
            throw new InvalidOperationException("Staging texture was not created.");
        }

        _context.CopySubresourceRegion(
            _stagingTexture,
            0,
            0,
            0,
            0,
            sourceTexture,
            0,
            new Box
            {
                Left = 0,
                Top = 0,
                Front = 0,
                Right = contentSize.Width,
                Bottom = contentSize.Height,
                Back = 1
            });

        MappedSubresource mapped = _context.Map(_stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        try
        {
            return CopyPackedBgra(mapped, contentSize);
        }
        finally
        {
            _context.Unmap(_stagingTexture, 0);
        }
    }

    private static ID3D11Texture2D GetNativeTexture(IDirect3DSurface surface)
    {
        IDirect3DDxgiInterfaceAccess access = surface.As<IDirect3DDxgiInterfaceAccess>();
        Guid iid = ID3D11Texture2D;
        int hr = access.GetInterface(ref iid, out nint pTexture);
        if (hr != 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return new ID3D11Texture2D(pTexture);
    }

    private void EnsureStagingTexture(int width, int height, Format format)
    {
        Texture2DDescription? existing = _stagingTexture?.Description;
        if (existing.HasValue &&
            existing.Value.Width == (uint)width &&
            existing.Value.Height == (uint)height &&
            existing.Value.Format == format)
        {
            return;
        }

        _stagingTexture?.Dispose();
        _stagingTexture = null;

        var description = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        _stagingTexture = _context.Device.CreateTexture2D(description);
    }

    private static unsafe byte[] CopyPackedBgra(MappedSubresource mapped, SizeInt contentSize)
    {
        int packedStride = contentSize.Width * 4;
        byte[] buffer = new byte[packedStride * contentSize.Height];
        byte* sourceRow = (byte*)mapped.DataPointer.ToPointer();

        for (int y = 0; y < contentSize.Height; y++)
        {
            Span<byte> source = new Span<byte>(sourceRow, packedStride);
            Span<byte> destination = buffer.AsSpan(y * packedStride, packedStride);
            source.CopyTo(destination);
            sourceRow += mapped.RowPitch;
        }

        return buffer;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _stagingTexture?.Dispose();
        _disposed = true;
    }
}
