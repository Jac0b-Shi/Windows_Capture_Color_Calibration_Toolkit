using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Rendering;

/// <summary>
/// Renders a chart definition into a host element and returns a session describing what was rendered.
/// </summary>
public interface IChartRenderer
{
    string RendererId { get; }

    ChartRenderSession Render(
        ChartDefinition chart,
        IReadOnlyList<PatchPlacement> placements,
        ChartRenderOptions options,
        object host);
}
