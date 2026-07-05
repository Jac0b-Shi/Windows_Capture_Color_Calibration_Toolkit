using System.Text.Json;
using System.Text.Json.Serialization;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Measurements;

namespace WgcColorCalibrator.Core.Serialization;

/// <summary>
/// Serializes chart definitions and measurement profiles using stable JSON options.
/// </summary>
public static class ProfileJsonSerializer
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter<ColorEncoding>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<CapturePixelFormat>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<CaptureSourceKind>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<SampleMethod>(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static string Serialize<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, CreateOptions());
    }

    public static T Deserialize<T>(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        return JsonSerializer.Deserialize<T>(json, CreateOptions())
            ?? throw new JsonException($"JSON did not contain a {typeof(T).Name} payload.");
    }
}
