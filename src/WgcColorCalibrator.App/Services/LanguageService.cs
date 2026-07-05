using Windows.Globalization;

namespace WgcColorCalibrator.App.Services;

public sealed class LanguageService
{
    public IReadOnlyList<string> SupportedLanguageTags { get; } = ["system", "zh-CN", "en-US"];

    public string CurrentLanguageTag =>
        string.IsNullOrWhiteSpace(ApplicationLanguages.PrimaryLanguageOverride)
            ? "system"
            : ApplicationLanguages.PrimaryLanguageOverride;

    public void SetLanguageOverride(string languageTag)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(languageTag);

        if (!SupportedLanguageTags.Contains(languageTag, StringComparer.Ordinal))
        {
            throw new ArgumentOutOfRangeException(nameof(languageTag), "Unsupported language tag.");
        }

        ApplicationLanguages.PrimaryLanguageOverride = languageTag == "system" ? string.Empty : languageTag;
    }
}

