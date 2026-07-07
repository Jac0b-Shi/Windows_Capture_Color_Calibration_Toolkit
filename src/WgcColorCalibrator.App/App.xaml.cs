using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using WgcColorCalibrator.Rendering.Direct3D11;
using WgcColorCalibrator.App.Models;
using WgcColorCalibrator.Core.Rendering;
using WgcColorCalibrator.App.Rendering.Xaml;
using WgcColorCalibrator.App.Services;
using WgcColorCalibrator.Core.Charts;

namespace WgcColorCalibrator.App;

public partial class App : Application
{
    private Window? window;

    public App()
    {
        Services = ConfigureServices();
        InitializeComponent();
    }

    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        window = Services.GetRequiredService<MainWindow>();
        window.Activate();
    }

    private static IServiceProvider ConfigureServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(AppContext.BaseDirectory, "appsettings.json"),
                optional: false,
                reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder =>
        {
            builder.AddDebug();
        });

        services.AddSingleton(CreateAppSettings(configuration));
        services.AddSingleton(CreateAppDefaults(configuration));
        services.AddSingleton<LanguageService>();
        services.AddSingleton<DiagnosticsSnapshotService>();

        services.AddSingleton<IChartProvider, ManualSingleColorChartProvider>();
        services.AddSingleton<IChartProvider, NearWhiteChartProvider>();
        services.AddSingleton<IChartProvider, GrayscaleChartProvider>();
        services.AddDirect3D11Rendering();
        services.AddSingleton<IChartWindowFactory, ChartWindowFactory>();
        services.AddSingleton<ChartWorkspaceService>();

        services.AddTransient<MainWindow>();
        return services.BuildServiceProvider();
    }

    private static AppSettings CreateAppSettings(IConfiguration configuration) => new()
    {
        Name = configuration["Application:Name"] ?? "WgcColorCalibrator"
    };

    private static AppDefaults CreateAppDefaults(IConfiguration configuration) => new()
    {
        UiLanguage = configuration["Defaults:UiLanguage"] ?? "system",
        DefaultChart = configuration["Defaults:DefaultChart"] ?? "manual-single-color",
        SampleMethod = configuration["Defaults:SampleMethod"] ?? "center-median",
        PreferredCapturePixelFormat = configuration["Defaults:PreferredCapturePixelFormat"] ?? "b8g8r8a8-uint-normalized",
        FallbackPolicy = configuration["Defaults:FallbackPolicy"] ?? "explicit-user-approval",
        DiagnosticLoggingLevel = configuration["Defaults:DiagnosticLoggingLevel"] ?? "information"
    };
}
