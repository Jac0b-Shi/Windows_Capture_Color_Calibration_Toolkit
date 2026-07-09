using Microsoft.Extensions.DependencyInjection;
using WgcColorCalibrator.Core.Capture;

namespace WgcColorCalibrator.Capture.WindowsGraphicsCapture;

/// <summary>
/// Registers the Windows Graphics Capture backend and geometry probe.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWindowsGraphicsCapture(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISingleFrameCaptureBackend, WindowsGraphicsCaptureBackend>();
        services.AddSingleton<IWindowGeometryProbe, WindowsGraphicsCaptureBackend>();
        return services;
    }
}
