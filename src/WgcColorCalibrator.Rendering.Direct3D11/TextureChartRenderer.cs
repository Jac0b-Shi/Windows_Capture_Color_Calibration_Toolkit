using System.Numerics;
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
        IToneMapper toneMapper,
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
            FillPixels(mapped, chart, placements, toneMapper, options, debugOverlayEnabled, width, height, format);
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

    private static unsafe void FillPixels(
        MappedSubresource mapped,
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> placements,
        IToneMapper toneMapper,
        ChartRenderOptions options,
        bool debugOverlayEnabled,
        int width,
        int height,
        Format format)
    {
        byte* ptr = (byte*)mapped.DataPointer;
        Vector4 background = MapColor(format, chart.Layout.WindowBackground, toneMapper, options.ToneMappingParameters);

        for (int y = 0; y < height; y++)
        {
            byte* row = ptr + (y * mapped.RowPitch);
            for (int x = 0; x < width; x++)
            {
                WritePixel(ref row, format, background);
            }
        }

        foreach (PatchPlacement placement in placements)
        {
            ColorPatchDefinition patch = chart.Patches.Single(p => p.Id == placement.PatchId);
            Vector4 color = MapColor(format, patch, toneMapper, options.ToneMappingParameters);
            FillRectangle(ptr, mapped.RowPitch, placement.Bounds, color, format, width, height);

            if (debugOverlayEnabled)
            {
                DrawDebugOverlay(ptr, mapped.RowPitch, placement, format, width, height);
            }
        }
    }

    private static unsafe void FillRectangle(byte* ptr, uint rowPitch, PixelRect bounds, Vector4 color, Format format, int width, int height)
    {
        int left = Math.Max(0, bounds.X);
        int top = Math.Max(0, bounds.Y);
        int right = Math.Min(width, bounds.X + bounds.Width);
        int bottom = Math.Min(height, bounds.Y + bounds.Height);

        int pixelByteSize = PixelByteSize(format);
        for (int y = top; y < bottom; y++)
        {
            byte* row = ptr + (y * rowPitch);
            byte* pixelRow = row + (left * pixelByteSize);
            for (int x = left; x < right; x++)
            {
                WritePixel(ref pixelRow, format, color);
            }
        }
    }

    private static unsafe void DrawDebugOverlay(byte* ptr, uint rowPitch, PatchPlacement placement, Format format, int width, int height)
    {
        Vector4 outline = PackSdrColor(255, 255, 255, 255);
        Vector4 inset = PackSdrColor(255, 0, 0, 255);

        PixelRect bounds = placement.Bounds;
        int left = Math.Max(0, bounds.X);
        int top = Math.Max(0, bounds.Y);
        int right = Math.Min(width, bounds.X + bounds.Width);
        int bottom = Math.Min(height, bounds.Y + bounds.Height);

        for (int x = left; x < right; x++)
        {
            SetPixel(ptr, rowPitch, x, top, outline, format, width, height);
            SetPixel(ptr, rowPitch, x, bottom - 1, outline, format, width, height);
        }

        for (int y = top; y < bottom; y++)
        {
            SetPixel(ptr, rowPitch, left, y, outline, format, width, height);
            SetPixel(ptr, rowPitch, right - 1, y, outline, format, width, height);
        }

        PixelRect safe = placement.SafeSampleBounds;
        int sLeft = Math.Max(0, safe.X);
        int sTop = Math.Max(0, safe.Y);
        int sRight = Math.Min(width, safe.X + safe.Width);
        int sBottom = Math.Min(height, safe.Y + safe.Height);

        for (int x = sLeft; x < sRight; x++)
        {
            SetPixel(ptr, rowPitch, x, sTop, inset, format, width, height);
            SetPixel(ptr, rowPitch, x, sBottom - 1, inset, format, width, height);
        }

        for (int y = sTop; y < sBottom; y++)
        {
            SetPixel(ptr, rowPitch, sLeft, y, inset, format, width, height);
            SetPixel(ptr, rowPitch, sRight - 1, y, inset, format, width, height);
        }
    }

    private static unsafe void SetPixel(byte* ptr, uint rowPitch, int x, int y, Vector4 color, Format format, int width, int height)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        byte* pixel = ptr + (y * rowPitch) + (x * PixelByteSize(format));
        WritePixel(ref pixel, format, color);
    }

    private static unsafe void WritePixel(ref byte* pixel, Format format, Vector4 color)
    {
        switch (format)
        {
            case Format.B8G8R8A8_UNorm:
                {
                    uint packed = PackBgra((byte)color.Z, (byte)color.Y, (byte)color.X, (byte)color.W);
                    *(uint*)pixel = packed;
                    pixel += 4;
                    break;
                }

            case Format.R16G16B16A16_Float:
                {
                    ushort* p = (ushort*)pixel;
                    p[0] = HalfToBits(color.X);
                    p[1] = HalfToBits(color.Y);
                    p[2] = HalfToBits(color.Z);
                    p[3] = HalfToBits(color.W);
                    pixel += 8;
                    break;
                }

            default:
                throw new NotSupportedException($"Format {format} is not supported in this renderer.");
        }
    }

    private static Vector4 MapColor(Format format, ColorPatchDefinition patch, IToneMapper toneMapper, ToneMappingParameters parameters)
    {
        return format switch
        {
            Format.B8G8R8A8_UNorm => PackSdrColor(patch.ExpectedColor.R, patch.ExpectedColor.G, patch.ExpectedColor.B, 255),
            Format.R16G16B16A16_Float => patch.SourceEncoding switch
            {
                ColorEncoding.LinearScRgb when patch.HdrColor.HasValue =>
                    toneMapper.Map(patch.HdrColor.Value.ToVector4(), parameters),
                ColorEncoding.SrgbEncoded =>
                    toneMapper.Map(SrgbToLinear(patch.ExpectedColor), parameters),
                _ => throw new NotSupportedException($"Source encoding {patch.SourceEncoding} is not supported for HDR output.")
            },
            _ => throw new NotSupportedException($"Format {format} is not supported in this renderer.")
        };
    }

    private static Vector4 MapColor(Format format, Rgb8 color, IToneMapper toneMapper, ToneMappingParameters parameters)
    {
        return format switch
        {
            Format.B8G8R8A8_UNorm => PackSdrColor(color.R, color.G, color.B, 255),
            Format.R16G16B16A16_Float => toneMapper.Map(SrgbToLinear(color), parameters),
            _ => throw new NotSupportedException($"Format {format} is not supported in this renderer.")
        };
    }

    private static Vector4 SrgbToLinear(Rgb8 color)
    {
        return new Vector4(
            ColorSpaceConverter.SrgbByteToLinear(color.R),
            ColorSpaceConverter.SrgbByteToLinear(color.G),
            ColorSpaceConverter.SrgbByteToLinear(color.B),
            1.0f);
    }

    private static Vector4 PackSdrColor(byte r, byte g, byte b, byte a)
    {
        return new Vector4(r, g, b, a);
    }

    private static uint PackBgra(byte b, byte g, byte r, byte a)
    {
        return (uint)(a << 24 | r << 16 | g << 8 | b);
    }

    private static ushort HalfToBits(float value)
    {
        return System.BitConverter.HalfToUInt16Bits((System.Half)value);
    }

    private static int PixelByteSize(Format format)
    {
        return format switch
        {
            Format.B8G8R8A8_UNorm => 4,
            Format.R16G16B16A16_Float => 8,
            _ => throw new NotSupportedException($"Format {format} is not supported in this renderer.")
        };
    }
}
