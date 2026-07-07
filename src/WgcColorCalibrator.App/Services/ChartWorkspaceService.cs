using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
using WgcColorCalibrator.App.Rendering.Abstractions;
using WgcColorCalibrator.App.Windows;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Holds the current chart state, manages import/export, and coordinates the chart window lifecycle.
/// </summary>
public sealed class ChartWorkspaceService
{
    private readonly IReadOnlyDictionary<string, IChartProvider> _providers;
    private readonly IChartRenderer _renderer;
    private readonly IChartWindowFactory _windowFactory;
    private ChartWindow? _chartWindow;

    public ChartWorkspaceService(
        IEnumerable<IChartProvider> providers,
        IChartRenderer renderer,
        IChartWindowFactory windowFactory)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(windowFactory);

        _providers = providers.ToDictionary(p => p.Id, StringComparer.Ordinal);
        _renderer = renderer;
        _windowFactory = windowFactory;

        CurrentLayout = ChartLayoutDefinition.Default;
        CurrentToneMappingParameters = ToneMappingParameters.Default;
        CurrentOutputMode = RenderOutputMode.SdrSrgb;
    }

    public event EventHandler? StateChanged;

    public ChartDefinition? CurrentChart { get; private set; }

    public ChartLayoutDefinition CurrentLayout { get; set; }

    public IReadOnlyList<PatchPlacement>? CurrentPlacements { get; private set; }

    public ChartRenderSession? CurrentSession { get; private set; }

    public RenderOutputMode CurrentOutputMode { get; set; }

    public ToneMappingParameters CurrentToneMappingParameters { get; set; }

    public bool IsDebugOverlayEnabled { get; set; }

    public IReadOnlyList<IChartProvider> Providers => _providers.Values.ToList();

    public void GenerateChart(string providerId, ChartGenerationOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentNullException.ThrowIfNull(options);

        if (!_providers.TryGetValue(providerId, out IChartProvider? provider))
        {
            throw new ArgumentException($"Unknown chart provider: {providerId}", nameof(providerId));
        }

        ChartDefinition chart = provider.Create(options);
        IReadOnlyList<PatchPlacement> placements = ChartLayoutEngine.CreatePlacements(chart);

        CurrentChart = chart;
        CurrentLayout = chart.Layout;
        CurrentPlacements = placements;
        CurrentOutputMode = options.OutputMode;
        CurrentToneMappingParameters = options.ToneMappingParameters ?? ToneMappingParameters.Default;
        CurrentSession = null;

        NotifyStateChanged();
    }

    public void OpenChartWindow()
    {
        if (CurrentChart is null || CurrentPlacements is null)
        {
            throw new InvalidOperationException("No chart has been generated.");
        }

        CloseChartWindow();

        _chartWindow = _windowFactory.Create();
        _chartWindow.Closed += OnChartWindowClosed;
        _chartWindow.Activate();

        RenderChartWindow(_chartWindow);
    }

    public void CloseChartWindow()
    {
        if (_chartWindow is null)
        {
            return;
        }

        _chartWindow.Closed -= OnChartWindowClosed;
        _chartWindow.Close();
        _chartWindow = null;
    }

    public void ToggleDebugOverlay()
    {
        IsDebugOverlayEnabled = !IsDebugOverlayEnabled;

        if (_chartWindow is not null)
        {
            RenderChartWindow(_chartWindow);
        }

        NotifyStateChanged();
    }

    public ChartDefinition ImportJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ChartDefinition chart = ChartJsonSerializer.Deserialize(json);
        SetImportedChart(chart);
        return chart;
    }

    public string ExportJson()
    {
        if (CurrentChart is null)
        {
            throw new InvalidOperationException("No chart to export.");
        }

        return ChartJsonSerializer.Serialize(CurrentChart);
    }

    public ChartDefinition ImportCsv(string csv, string chartId, string chartName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csv);
        ArgumentException.ThrowIfNullOrWhiteSpace(chartId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chartName);

        ChartDefinition chart = ChartCsvSerializer.DeserializeChart(csv, chartId, chartName, CurrentLayout);
        SetImportedChart(chart);
        return chart;
    }

    public string ExportCsv()
    {
        if (CurrentChart is null)
        {
            throw new InvalidOperationException("No chart to export.");
        }

        return ChartCsvSerializer.SerializePatches(CurrentChart.Patches);
    }

    private void SetImportedChart(ChartDefinition chart)
    {
        CurrentChart = chart;
        CurrentLayout = chart.Layout;
        CurrentPlacements = ChartLayoutEngine.CreatePlacements(chart);
        CurrentOutputMode = chart.RenderingParameters?.RequestedOutputMode ?? RenderOutputMode.SdrSrgb;
        CurrentToneMappingParameters = chart.RenderingParameters?.ToneMappingParameters ?? ToneMappingParameters.Default;
        CurrentSession = null;
        NotifyStateChanged();
    }

    private void RenderChartWindow(ChartWindow window)
    {
        if (CurrentChart is null || CurrentPlacements is null)
        {
            return;
        }

        var options = new ChartRenderOptions(
            CurrentChart,
            CurrentPlacements,
            window.GetRasterizationScale(),
            IsDebugOverlayEnabled,
            CurrentOutputMode,
            CurrentToneMappingParameters);

        CurrentSession = window.Render(_renderer, options);
    }

    private void OnChartWindowClosed(object sender, WindowEventArgs args)
    {
        if (_chartWindow is not null)
        {
            _chartWindow.Closed -= OnChartWindowClosed;
            _chartWindow = null;
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
