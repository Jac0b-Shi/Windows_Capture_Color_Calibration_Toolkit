using Vortice.Direct3D11;
using Vortice.DXGI;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Renders a chart by filling a dynamic texture on the CPU and copying it to the swap chain back buffer.
/// </summary>
public sealed class TextureChartRenderer
{
    private readonly D3D11DeviceResources _resources;
    private ID3D11Texture2D? _stagingTexture;
    private int _width;
    private int _height;
    private Format _format;

    public TextureChartRenderer(D3D11DeviceResources resources)
    {
        _resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public void Render(
        ID3D11Texture2D backBuffer,
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> placements,
        ChartRenderOptions options,
        bool debugOverlayEnabled)
    {
        Texture2DDescription backBufferDesc = backBuffer.Description;
        int width = (int)backBufferDesc.Width;
        int height = (int)backBufferDesc.Height;
        Format format = backBufferDesc.Format;

        EnsureTexture(width, height, format);
        if (_stagingTexture is null)
        {
            throw new Direct3D11RenderingException("Failed to create staging texture.");
        }

        MappedSubresource mapped = _resources.Context.Map(_stagingTexture, 0, MapMode.WriteDiscard, Vortice.Direct3D11.MapFlags.None);
        try
        {
            FillPixels(mapped, chart, placements, options, debugOverlayEnabled, width, height, format);
        }
        finally
        {
            _resources.Context.Unmap(_stagingTexture, 0);
        }

        _resources.Context.CopyResource(backBuffer, _stagingTexture);
    }

    public void Dispose()
    {
        _stagingTexture?.Dispose();
        _stagingTexture = null;
    }

    private void EnsureTexture(int width, int height, Format format)
    {
        if (_stagingTexture is not null && _width == width && _height == height && _format == format)
        {
            return;
        }

        _stagingTexture?.Dispose();
        _stagingTexture = null;

        _width = width;
        _height = height;
        _format = format;

        var desc = new Texture2DDescription
        {
            Width = (uint)width,
            Height = (uint)height,
            MipLevels = 1,
            ArraySize = 1,
            Format = format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Dynamic,
            BindFlags = BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.Write,
            MiscFlags = ResourceOptionFlags.None
        };

        _stagingTexture = _resources.Device.CreateTexture2D(desc);
    }

    private unsafe void FillPixels(
        MappedSubresource mapped,
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> placements,
        ChartRenderOptions options,
        bool debugOverlayEnabled,
        int width,
        int height,
        Format format)
    {
        byte* ptr = (byte*)mapped.DataPointer;
        uint background = PackColor(format, chart.Layout.WindowBackground);

        for (int y = 0; y < height; y++)
        {
            byte* row = ptr + (y * mapped.RowPitch);
            for (int x = 0; x < width; x++)
            {
                *(uint*)row = background;
                row += 4;
            }
        }

        foreach (PatchPlacement placement in placements)
        {
            ColorPatchDefinition patch = chart.Patches.Single(p => p.Id == placement.PatchId);
            uint color = PackColor(format, patch.ExpectedColor);
            FillRectangle(ptr, mapped.RowPitch, placement.Bounds, color, width, height);

            if (debugOverlayEnabled)
            {
                DrawDebugOverlay(ptr, mapped.RowPitch, placement, width, height);
            }
        }
    }

    private unsafe void FillRectangle(byte* ptr, uint rowPitch, PixelRect bounds, uint color, int width, int height)
    {
        int left = Math.Max(0, bounds.X);
        int top = Math.Max(0, bounds.Y);
        int right = Math.Min(width, bounds.X + bounds.Width);
        int bottom = Math.Min(height, bounds.Y + bounds.Height);

        for (int y = top; y < bottom; y++)
        {
            byte* row = ptr + (y * rowPitch);
            uint* pixelRow = (uint*)(row + left * 4);
            for (int x = left; x < right; x++)
            {
                *pixelRow++ = color;
            }
        }
    }

    private unsafe void DrawDebugOverlay(byte* ptr, uint rowPitch, PatchPlacement placement, int width, int height)
    {
        uint outline = 0xFFFFFFFF;
        uint inset = 0xFF0000FF;

        PixelRect bounds = placement.Bounds;
        int left = Math.Max(0, bounds.X);
        int top = Math.Max(0, bounds.Y);
        int right = Math.Min(width, bounds.X + bounds.Width);
        int bottom = Math.Min(height, bounds.Y + bounds.Height);

        for (int x = left; x < right; x++)
        {
            SetPixel(ptr, rowPitch, x, top, outline, width, height);
            SetPixel(ptr, rowPitch, x, bottom - 1, outline, width, height);
        }

        for (int y = top; y < bottom; y++)
        {
            SetPixel(ptr, rowPitch, left, y, outline, width, height);
            SetPixel(ptr, rowPitch, right - 1, y, outline, width, height);
        }

        PixelRect safe = placement.SafeSampleBounds;
        int sLeft = Math.Max(0, safe.X);
        int sTop = Math.Max(0, safe.Y);
        int sRight = Math.Min(width, safe.X + safe.Width);
        int sBottom = Math.Min(height, safe.Y + safe.Height);

        for (int x = sLeft; x < sRight; x++)
        {
            SetPixel(ptr, rowPitch, x, sTop, inset, width, height);
            SetPixel(ptr, rowPitch, x, sBottom - 1, inset, width, height);
        }

        for (int y = sTop; y < sBottom; y++)
        {
            SetPixel(ptr, rowPitch, sLeft, y, inset, width, height);
            SetPixel(ptr, rowPitch, sRight - 1, y, inset, width, height);
        }
    }

    private unsafe void SetPixel(byte* ptr, uint rowPitch, int x, int y, uint color, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        uint* pixel = (uint*)(ptr + (y * rowPitch) + (x * 4));
        *pixel = color;
    }

    private static uint PackColor(Format format, Rgb8 color)
    {
        return format switch
        {
            Format.B8G8R8A8_UNorm => PackBgra(color.B, color.G, color.R, 255),
            Format.R8G8B8A8_UNorm => PackBgra(color.R, color.G, color.B, 255),
            _ => throw new NotSupportedException($"Format {format} is not supported in this renderer.")
        };
    }

    private static uint PackBgra(byte b, byte g, byte r, byte a)
    {
        return (uint)(a << 24 | r << 16 | g << 8 | b);
    }
}
