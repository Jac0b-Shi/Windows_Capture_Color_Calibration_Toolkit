using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using WgcColorCalibrator.App.Models;
using WgcColorCalibrator.App.Services;

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
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
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
        services.AddTransient<MainWindow>();
        return services.BuildServiceProvider();
    }

    private static AppSettings CreateAppSettings(IConfiguration configuration) => new()
    {
        Name = configuration["Application:Name"] ?? "WgcColorCalibrator",
        Version = configuration["Application:Version"] ?? "0.1.0",
        WindowsAppSdkPackageVersion = configuration["Application:WindowsAppSdkPackageVersion"] ?? "2.2.0"
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
