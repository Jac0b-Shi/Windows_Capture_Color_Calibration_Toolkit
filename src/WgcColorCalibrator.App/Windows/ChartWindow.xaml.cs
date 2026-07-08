using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using WinRT.Interop;
using WgcColorCalibrator.App.Services;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.App.Windows;

/// <summary>
/// Independent window that displays the chart test content.
/// </summary>
public sealed partial class ChartWindow : Window
{
    private IChartRenderer? _renderer;
    private bool _readyRaised;
    private bool _surfaceReadyRaised;
    private bool _appWindowChangedSubscribed;
    private SizeInt _intendedPhysicalSize;
    private Point _contentOrigin;
    private nint _lastMonitor;
    private double _lastScale = 1.0;
    private DispatcherQueueTimer? _hotplugTimer;

    public ChartWindow()
    {
        InitializeComponent();
        WindowIconService.ApplyIcon(this);

        var resourceLoader = new Microsoft.Windows.ApplicationModel.Resources.ResourceLoader();
        Title = resourceLoader.GetString("ChartWindowTitle");

        _lastMonitor = NativeMethods.MonitorFromWindow(WindowHandle, 2);
        _lastScale = GetRasterizationScale();

        ChartSwapChainPanel.Loaded += OnPanelLoadedOrSizeChanged;
        ChartSwapChainPanel.SizeChanged += OnPanelLoadedOrSizeChanged;
        ChartSwapChainPanel.CompositionScaleChanged += OnCompositionScaleChanged;
        Closed += OnClosed;

        _hotplugTimer = DispatcherQueue.GetForCurrentThread().CreateTimer();
        _hotplugTimer.Interval = TimeSpan.FromSeconds(2);
        _hotplugTimer.Tick += OnHotplugTimerTick;
        _hotplugTimer.Start();
    }

    public event EventHandler? PanelReady;

    public event EventHandler? SurfaceReady;

    public event EventHandler? DisplayChanged;

    public nint WindowHandle => WindowNative.GetWindowHandle(this);

    public void SetChartSize(SizeInt physicalSize)
    {
        _intendedPhysicalSize = physicalSize;
        _surfaceReadyRaised = false;

        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            p.IsResizable = false;
            p.IsMaximizable = false;
        }

        UpdatePanelLogicalSize();

        AppWindow.ResizeClient(new global::Windows.Graphics.SizeInt32
        {
            Width = physicalSize.Width,
            Height = physicalSize.Height
        });

        if (!_appWindowChangedSubscribed)
        {
            AppWindow.Changed += AppWindow_Changed;
            _appWindowChangedSubscribed = true;
        }

        CheckSizeSettled();
    }

    public double GetRasterizationScale() =>
        ChartSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;

    public SizeInt ClientPhysicalSize
    {
        get
        {
        global::Windows.Graphics.SizeInt32 clientSize = AppWindow.ClientSize;
        _contentOrigin = new Point(0, 0);
            return new SizeInt(clientSize.Width, clientSize.Height);
        }
    }

    public Point ContentOrigin => _contentOrigin;

    public ChartRenderSession Render(IChartRenderer renderer, ChartRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(options);

        _renderer = renderer;
        return renderer.Render(options.Chart, options.Placements, options, ChartSwapChainPanel);
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange)
        {
            CheckSizeSettled();
        }
    }

    private void OnPanelLoadedOrSizeChanged(object sender, object e)
    {
        if (ChartSwapChainPanel.XamlRoot is null)
        {
            return;
        }

        if (ChartSwapChainPanel.ActualWidth <= 0 || ChartSwapChainPanel.ActualHeight <= 0)
        {
            return;
        }

        _lastScale = GetRasterizationScale();

        if (!_readyRaised)
        {
            _readyRaised = true;
            PanelReady?.Invoke(this, EventArgs.Empty);
        }

        CheckSizeSettled();
        CheckDisplayChanged();
    }

    private void OnHotplugTimerTick(DispatcherQueueTimer sender, object args)
    {
        CheckDisplayChanged();
    }

    private void OnCompositionScaleChanged(SwapChainPanel sender, object args)
    {
        if (sender.XamlRoot is null)
        {
            return;
        }

        _surfaceReadyRaised = false;
        _lastScale = GetRasterizationScale();
        UpdatePanelLogicalSize();
        CheckSizeSettled();
    }

    private void UpdatePanelLogicalSize()
    {
        (double scaleX, double scaleY) = GetCurrentCompositionScale();

        ChartSwapChainPanel.Width = _intendedPhysicalSize.Width / scaleX;
        ChartSwapChainPanel.Height = _intendedPhysicalSize.Height / scaleY;
    }

    private void CheckDisplayChanged()
    {
        if (!_readyRaised)
        {
            return;
        }

        nint currentMonitor = NativeMethods.MonitorFromWindow(WindowHandle, 2);
        double currentScale = GetRasterizationScale();

        if (currentMonitor != _lastMonitor || currentScale != _lastScale)
        {
            _lastMonitor = currentMonitor;
            _lastScale = currentScale;

            if (_surfaceReadyRaised)
            {
                DisplayChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void CheckSizeSettled()
    {
        if (_surfaceReadyRaised)
        {
            return;
        }

        if (ChartSwapChainPanel.XamlRoot is null)
        {
            return;
        }

        if (ChartSwapChainPanel.ActualWidth <= 0 || ChartSwapChainPanel.ActualHeight <= 0)
        {
            return;
        }

        (double scaleX, double scaleY) = GetCurrentCompositionScale();

        int panelPhysicalWidth = (int)Math.Round(ChartSwapChainPanel.ActualWidth * scaleX);
        int panelPhysicalHeight = (int)Math.Round(ChartSwapChainPanel.ActualHeight * scaleY);

        global::Windows.Graphics.SizeInt32 clientSize = AppWindow.ClientSize;

        bool clientSizeLargeEnough =
            clientSize.Width >= _intendedPhysicalSize.Width - 1 &&
            clientSize.Height >= _intendedPhysicalSize.Height - 1;

        bool panelSizeMatches =
            Math.Abs(panelPhysicalWidth - _intendedPhysicalSize.Width) <= 1 &&
            Math.Abs(panelPhysicalHeight - _intendedPhysicalSize.Height) <= 1;

        if (clientSizeLargeEnough && panelSizeMatches)
        {
            _surfaceReadyRaised = true;
            SurfaceReady?.Invoke(this, EventArgs.Empty);
        }
    }

    private (double ScaleX, double ScaleY) GetCurrentCompositionScale()
    {
        double fallbackScale = ChartSwapChainPanel.XamlRoot?.RasterizationScale ?? 1.0;
        double scaleX = ChartSwapChainPanel.CompositionScaleX > 0
            ? ChartSwapChainPanel.CompositionScaleX
            : fallbackScale;
        double scaleY = ChartSwapChainPanel.CompositionScaleY > 0
            ? ChartSwapChainPanel.CompositionScaleY
            : fallbackScale;
        return (scaleX, scaleY);
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _hotplugTimer?.Stop();
        _hotplugTimer = null;

        ChartSwapChainPanel.Loaded -= OnPanelLoadedOrSizeChanged;
        ChartSwapChainPanel.SizeChanged -= OnPanelLoadedOrSizeChanged;
        ChartSwapChainPanel.CompositionScaleChanged -= OnCompositionScaleChanged;

        if (_appWindowChangedSubscribed)
        {
            AppWindow.Changed -= AppWindow_Changed;
            _appWindowChangedSubscribed = false;
        }

        if (_renderer is not null)
        {
            _renderer.DetachHost(ChartSwapChainPanel);
            _renderer = null;
        }
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = false)]
        public static extern nint MonitorFromWindow(nint hwnd, uint dwFlags);
    }
}
