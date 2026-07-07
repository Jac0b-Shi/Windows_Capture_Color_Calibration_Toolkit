using Microsoft.UI.Xaml;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.App.Windows;

/// <summary>
/// Independent window that displays the chart test content.
/// </summary>
public sealed partial class ChartWindow : Window
{
    public ChartWindow()
    {
        InitializeComponent();

        var resourceLoader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader();
        Title = resourceLoader.GetString("ChartWindowTitle");
    }

    public void SetChartSize(SizeInt physicalSize, double scale)
    {
        AppWindow.ResizeClient(new global::Windows.Graphics.SizeInt32
        {
            Width = physicalSize.Width,
            Height = physicalSize.Height
        });
    }

    public double GetRasterizationScale() =>
        ChartSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;

    public ChartRenderSession Render(IChartRenderer renderer, ChartRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(options);

        return renderer.Render(options.Chart, options.Placements, options, ChartSwapChainPanel);
    }
}
