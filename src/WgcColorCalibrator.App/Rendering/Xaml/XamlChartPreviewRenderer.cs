using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.App.Rendering.Xaml;

/// <summary>
/// Experimental SDR preview renderer for the chart configuration page. Not intended for measurement.
/// </summary>
public sealed class XamlChartPreviewRenderer : IChartRenderer
{
    public string RendererId => "xaml-preview";

    public ChartRenderSession Render(
        Core.Charts.ChartDefinition chart,
        IReadOnlyList<PatchPlacement> placements,
        ChartRenderOptions options,
        object host)
    {
        ArgumentNullException.ThrowIfNull(chart);
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(host);

        if (host is not Panel target)
        {
            throw new ArgumentException("Host must be a Panel.", nameof(host));
        }

        var panel = new Canvas();
        var background = new SolidColorBrush(ToWindowsColor(chart.Layout.WindowBackground));
        panel.Background = background;

        foreach (PatchPlacement placement in placements)
        {
            var patch = chart.Patches.Single(p => p.Id == placement.PatchId);
            var rect = new Rectangle
            {
                Fill = new SolidColorBrush(ToWindowsColor(patch.ExpectedColor)),
                Width = PhysicalPixelConverter.ToDip(placement.Bounds.Width, options.RasterizationScale),
                Height = PhysicalPixelConverter.ToDip(placement.Bounds.Height, options.RasterizationScale)
            };

            Canvas.SetLeft(rect, PhysicalPixelConverter.ToDip(placement.Bounds.X, options.RasterizationScale));
            Canvas.SetTop(rect, PhysicalPixelConverter.ToDip(placement.Bounds.Y, options.RasterizationScale));

            panel.Children.Add(rect);
        }

        target.Children.Clear();
        target.Children.Add(panel);

        // Preview renderer does not support real output mode tracking; it is always SDR.
        return new ChartRenderSession(
            RendererId,
            chart,
            placements,
            options.OutputMode,
            RenderOutputMode.SdrSrgb,
            "B8G8R8A8_UNORM",
            "RGB_FULL_G22_NONE_P709",
            false,
            options.RasterizationScale,
            new Size(0, 0),
            new SizeInt(0, 0),
            new SizeInt(0, 0),
            options.ToneMappingParameters,
            ["xaml-preview-not-for-measurement"],
            DateTimeOffset.UtcNow);
    }

    private static global::Windows.UI.Color ToWindowsColor(Core.Colors.Rgb8 color) =>
        Microsoft.UI.ColorHelper.FromArgb(255, color.R, color.G, color.B);
}
