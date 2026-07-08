using Microsoft.Extensions.DependencyInjection;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Registers Direct3D 11 rendering services with the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirect3D11Rendering(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(D3D11DeviceResources.Create());
        services.AddSingleton<IToneMapper, ToneMapperDirectScRgb>();
        services.AddSingleton<IToneMapper, ToneMapperReferenceWhiteScaled>();
        services.AddSingleton<IChartRenderer, D3D11ChartRenderer>();
        services.AddSingleton<IDisplayOutputProbe, DisplayOutputProbe>();
        return services;
    }
}
