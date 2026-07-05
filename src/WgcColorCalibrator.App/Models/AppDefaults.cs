namespace WgcColorCalibrator.App.Models;

public sealed class AppDefaults
{
    public string UiLanguage { get; set; } = "system";

    public string DefaultChart { get; set; } = "manual-single-color";

    public string SampleMethod { get; set; } = "center-median";

    public string PreferredCapturePixelFormat { get; set; } = "b8g8r8a8-uint-normalized";

    public string FallbackPolicy { get; set; } = "explicit-user-approval";

    public string DiagnosticLoggingLevel { get; set; } = "information";
}

