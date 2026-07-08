using Microsoft.UI.Xaml;
using Microsoft.Extensions.DependencyInjection;
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
    private readonly IDisplayOutputProbe _displayOutputProbe;
    private ChartWindow? _chartWindow;

    public ChartWorkspaceService(
        IEnumerable<IChartProvider> providers,
        IChartRenderer renderer,
        IChartWindowFactory windowFactory,
        IDisplayOutputProbe displayOutputProbe)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(windowFactory);
        ArgumentNullException.ThrowIfNull(displayOutputProbe);

        _providers = providers.ToDictionary(p => p.Id, StringComparer.Ordinal);
        _renderer = renderer;
        _windowFactory = windowFactory;
        _displayOutputProbe = displayOutputProbe;

        CurrentLayout = ChartLayoutDefinition.Default;
        CurrentToneMappingParameters = ToneMappingParameters.Default;
        CurrentOutputMode = RenderOutputMode.SdrSrgb;
        HdrUnsupportedBehavior = HdrUnsupportedBehavior.Cancel;
    }

    public event EventHandler? StateChanged;

    public ChartDefinition? CurrentChart { get; private set; }

    public ChartLayoutDefinition CurrentLayout { get; set; }

    public IReadOnlyList<PatchPlacement>? CurrentPlacements { get; private set; }

    public ChartRenderSession? CurrentSession { get; private set; }

    public RenderOutputMode CurrentOutputMode { get; set; }

    public ToneMappingMode CurrentToneMappingMode { get; set; }

    public ToneMappingParameters CurrentToneMappingParameters { get; set; }

    public HdrUnsupportedBehavior HdrUnsupportedBehavior { get; set; }

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
        CurrentToneMappingMode = options.ToneMappingMode;
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
        _chartWindow.SurfaceReady += OnChartWindowSurfaceReady;
        _chartWindow.DisplayChanged += OnChartWindowDisplayChanged;
        _chartWindow.SetChartSize(CalculateIntendedPhysicalSize(CurrentPlacements, CurrentChart.Layout));
        _chartWindow.Activate();
    }

    private void OnChartWindowSurfaceReady(object? sender, EventArgs e)
    {
        if (_chartWindow is null || CurrentChart is null || CurrentPlacements is null)
        {
            return;
        }

        ReProbeAndRender();
        NotifyStateChanged();
    }

    private void OnChartWindowDisplayChanged(object? sender, EventArgs e)
    {
        if (_chartWindow is null || CurrentChart is null || CurrentPlacements is null)
        {
            return;
        }

        ReProbeAndRender();
        NotifyStateChanged();
    }

    private void ReProbeAndRender()
    {
        if (_chartWindow is null || CurrentChart is null || CurrentPlacements is null)
        {
            return;
        }

        DisplayOutputMetadata displayMetadata = _displayOutputProbe.Probe(_chartWindow.WindowHandle);
        var warnings = new List<string>();
        bool allowHdrClippingExperiment = CurrentOutputMode != RenderOutputMode.SdrSrgb &&
                                           HdrUnsupportedBehavior == HdrUnsupportedBehavior.AllowClippingExperiment;
        OutputModeResolution resolution = OutputModeResolver.ResolveDetailed(
            CurrentOutputMode,
            displayMetadata,
            allowHdrClippingExperiment,
            warnings);

        RenderChartWindow(_chartWindow, resolution, CurrentToneMappingMode, warnings);
    }

    public void CloseChartWindow()
    {
        if (_chartWindow is null)
        {
            return;
        }

        _chartWindow.Closed -= OnChartWindowClosed;
        _chartWindow.SurfaceReady -= OnChartWindowSurfaceReady;
        _chartWindow.DisplayChanged -= OnChartWindowDisplayChanged;
        _chartWindow.Close();
        _chartWindow = null;
    }

    public void ToggleDebugOverlay()
    {
        IsDebugOverlayEnabled = !IsDebugOverlayEnabled;

        if (_chartWindow is not null)
        {
            DisplayOutputMetadata metadata = _displayOutputProbe.Probe(_chartWindow.WindowHandle);
            var warnings = new List<string>();
            bool allowHdrClippingExperiment = CurrentOutputMode != RenderOutputMode.SdrSrgb &&
                                              HdrUnsupportedBehavior == HdrUnsupportedBehavior.AllowClippingExperiment;
            OutputModeResolution resolution = OutputModeResolver.ResolveDetailed(
                CurrentOutputMode,
                metadata,
                allowHdrClippingExperiment,
                warnings);

            RenderChartWindow(_chartWindow, resolution, CurrentToneMappingMode, warnings);
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
        CurrentToneMappingMode = chart.RenderingParameters?.ToneMappingMode ?? ToneMappingMode.DirectScRgb;
        CurrentSession = null;
        NotifyStateChanged();
    }

    private void RenderChartWindow(
        ChartWindow window,
        OutputModeResolution resolution,
        ToneMappingMode toneMappingMode,
        IReadOnlyList<string> warnings)
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
            resolution.RequestedMode,
            resolution.ActualMode,
            CurrentToneMappingParameters,
            resolution.DisplayOutput,
            HdrUnsupportedBehavior == HdrUnsupportedBehavior.AllowClippingExperiment,
            warnings,
            window.ClientPhysicalSize,
            window.ContentOrigin);

        CurrentSession = window.Render(_renderer, options);
    }

    private void OnChartWindowClosed(object sender, WindowEventArgs args)
    {
        if (_chartWindow is not null)
        {
            _chartWindow.Closed -= OnChartWindowClosed;
            _chartWindow.SurfaceReady -= OnChartWindowSurfaceReady;
            _chartWindow.DisplayChanged -= OnChartWindowDisplayChanged;
            _chartWindow = null;
        }
    }

    private static SizeInt CalculateIntendedPhysicalSize(IReadOnlyList<PatchPlacement> placements, ChartLayoutDefinition layout)
    {
        int maxRight = 0;
        int maxBottom = 0;

        foreach (PatchPlacement placement in placements)
        {
            maxRight = Math.Max(maxRight, placement.Bounds.X + placement.Bounds.Width);
            maxBottom = Math.Max(maxBottom, placement.Bounds.Y + placement.Bounds.Height);
        }

        return new SizeInt(maxRight + layout.Border, maxBottom + layout.Border);
    }

    private void NotifyStateChanged() => StateChanged?.Invoke(this, EventArgs.Empty);
}
