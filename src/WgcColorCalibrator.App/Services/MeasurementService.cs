using System.Runtime.InteropServices;
using WgcColorCalibrator.Core.Capture;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Common;
using WgcColorCalibrator.Core.Geometry;
using WgcColorCalibrator.Core.Layout;
using WgcColorCalibrator.Core.Measurements;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.App.Models;
using WgcColorCalibrator.Core.Serialization;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Orchestrates a single-frame capture and measurement workflow.
/// </summary>
public sealed class MeasurementService
{
    private readonly ISingleFrameCaptureBackend _captureBackend;
    private readonly IWindowGeometryProbe _geometryProbe;
    private readonly ChartWorkspaceService _workspace;
    private readonly AppDefaults _defaults;

    public MeasurementService(
        ISingleFrameCaptureBackend captureBackend,
        IWindowGeometryProbe geometryProbe,
        ChartWorkspaceService workspace,
        AppDefaults defaults)
    {
        ArgumentNullException.ThrowIfNull(captureBackend);
        ArgumentNullException.ThrowIfNull(geometryProbe);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(defaults);

        _captureBackend = captureBackend;
        _geometryProbe = geometryProbe;
        _workspace = workspace;
        _defaults = defaults;
    }

    public event EventHandler? StateChanged;

    public MeasurementSession? CurrentSession { get; private set; }

    public CapturedFrame? CurrentFrame { get; private set; }

    public CaptureFailure? LastFailure { get; private set; }

    public string ExportCurrentSessionAsCsv()
    {
        if (CurrentSession is null)
        {
            throw new InvalidOperationException("No measurement session available.");
        }

        return MeasurementCsvSerializer.Serialize(CurrentSession);
    }

    public byte[]? ExportCurrentFrameAsBgra()
    {
        return CurrentFrame?.ContentPixels.ToArray();
    }

    public bool CanCapture(out string? reason)
    {
        if (!_workspace.TryGetCaptureTarget(out ChartCaptureTarget? target))
        {
            reason = "chart-window-not-ready";
            return false;
        }

        if (target.WindowHandle == 0)
        {
            reason = "chart-window-handle-invalid";
            return false;
        }

        if (!IsWindow(target.WindowHandle))
        {
            reason = "chart-window-not-alive";
            return false;
        }

        if (IsWindowMinimized(target.WindowHandle))
        {
            reason = "chart-window-minimized";
            return false;
        }

        if (target.AreParametersDirty)
        {
            reason = "chart-parameters-dirty";
            return false;
        }

        if (target.IsDebugOverlayEnabled)
        {
            reason = "debug-overlay-enabled";
            return false;
        }

        if (target.RenderSession.ActualOutputMode == RenderOutputMode.SdrSrgb && target.RenderSession.RequestedOutputMode != RenderOutputMode.SdrSrgb)
        {
            reason = "actual-output-mode-not-resolved";
            return false;
        }

        reason = null;
        return true;
    }

    public async Task CaptureAsync(CancellationToken cancellationToken = default)
    {
        if (!CanCapture(out string? reason))
        {
            throw new InvalidOperationException($"Cannot capture: {reason}");
        }

        _workspace.TryGetCaptureTarget(out ChartCaptureTarget? target);
        ArgumentNullException.ThrowIfNull(target);

        LastFailure = null;
        WindowGeometrySnapshot before = _geometryProbe.Capture(target.WindowHandle);
        CurrentFrame = await _captureBackend.CaptureAsync(
            new WindowCaptureRequest(target.WindowHandle, CapturePixelFormat.B8G8R8A8UIntNormalized, TimeSpan.FromSeconds(5)),
            cancellationToken);
        WindowGeometrySnapshot after = _geometryProbe.Capture(target.WindowHandle);

        CaptureGeometry captureGeometry = CaptureGeometryMapper.Map(
            before,
            after,
            CurrentFrame,
            new PixelPoint((int)Math.Round(target.RenderSession.ContentOrigin.X), (int)Math.Round(target.RenderSession.ContentOrigin.Y)));

        CaptureSummary captureSummary = new(
            _captureBackend.BackendId,
            CaptureSourceKind.Window,
            CapturePixelFormat.B8G8R8A8UIntNormalized,
            CapturePixelFormat.B8G8R8A8UIntNormalized,
            ColorEncoding.CaptureNative,
            false);

        SampleMethod sampleMethod = ParseSampleMethod(_defaults.SampleMethod);
        List<PatchSample> samples = new(target.Placements.Count);
        foreach (PatchPlacement placement in target.Placements)
        {
            samples.Add(PatchSampler.Sample(CurrentFrame, captureGeometry.ContentOffset, placement, sampleMethod));
        }

        ApplicationInfo application = new(
            "Windows Capture Color Calibration Toolkit",
            GetApplicationVersion());

        CurrentSession = MeasurementSessionBuilder.Build(
            application,
            CollectSystemMetadata(),
            CollectGpuMetadata(),
            CollectDisplayMetadata(target.RenderSession),
            CollectHdrMetadata(target.RenderSession),
            target.Chart,
            target.Placements,
            target.RenderSession,
            captureSummary,
            captureGeometry,
            samples);

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsWindow(nint hwnd)
    {
        return User32.IsWindow(hwnd);
    }

    private static bool IsWindowMinimized(nint hwnd)
    {
        return User32.IsIconic(hwnd);
    }

    private static string GetApplicationVersion()
    {
        return typeof(App).Assembly.GetName().Version?.ToString() ?? "0.0.0";
    }

    private static IReadOnlyDictionary<string, string> CollectSystemMetadata()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["osVersion"] = Environment.OSVersion.ToString(),
            ["framework"] = RuntimeInformation.FrameworkDescription,
            ["machineName"] = Environment.MachineName
        };
    }

    private static IReadOnlyDictionary<string, string> CollectGpuMetadata()
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["gpu"] = "unknown"
        };
    }

    private static IReadOnlyDictionary<string, string> CollectDisplayMetadata(ChartRenderSession session)
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["displayName"] = session.DisplayOutput?.DisplayName ?? "unknown",
            ["hdrActive"] = (session.DisplayOutput?.HdrActive ?? false).ToString(),
            ["hdrSupported"] = (session.DisplayOutput?.HdrSupported ?? false).ToString()
        };

        if (session.DisplayOutput is not null)
        {
            metadata["maxLuminance"] = session.DisplayOutput.MaxLuminance.ToString(System.Globalization.CultureInfo.InvariantCulture);
            metadata["maxFullFrameLuminance"] = session.DisplayOutput.MaxFullFrameLuminance.ToString(System.Globalization.CultureInfo.InvariantCulture);
            metadata["minLuminance"] = session.DisplayOutput.MinLuminance.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return metadata;
    }

    private static IReadOnlyDictionary<string, string> CollectHdrMetadata(ChartRenderSession session)
    {
        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["requestedOutputMode"] = session.RequestedOutputMode.ToString(),
            ["actualOutputMode"] = session.ActualOutputMode.ToString(),
            ["toneMapperId"] = session.ToneMapperId ?? "none",
            ["paperWhiteNits"] = session.ToneMappingParameters.PaperWhiteNits.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["peakBrightnessNits"] = session.ToneMappingParameters.PeakBrightnessNits.ToString(System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    private static SampleMethod ParseSampleMethod(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "center-pixel" or "pixel" => SampleMethod.CenterPixel,
            "center-mean" or "mean" => SampleMethod.CenterMean,
            "center-median" or "median" => SampleMethod.CenterMedian,
            _ => SampleMethod.CenterMedian
        };
    }

    private static class User32
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(nint hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsIconic(nint hWnd);
    }
}

public sealed record CaptureFailure(string Reason, string? Details);
