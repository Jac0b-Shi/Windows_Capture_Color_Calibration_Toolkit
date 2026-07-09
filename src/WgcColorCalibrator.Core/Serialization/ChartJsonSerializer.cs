using System.Text.Json;
using System.Text.Json.Serialization;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Common;
using WgcColorCalibrator.Core.Rendering;

namespace WgcColorCalibrator.Core.Serialization;

/// <summary>
/// Reads and writes chart definitions to JSON using the project schema format.
/// </summary>
public static class ChartJsonSerializer
{
    public static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = ProfileJsonSerializer.CreateOptions();
        options.Converters.Add(new Rgb8JsonConverter());
        options.Converters.Add(new HdrColorJsonConverter());
        options.Converters.Add(new JsonStringEnumConverter<RenderOutputMode>(JsonNamingPolicy.CamelCase));
        options.Converters.Add(new JsonStringEnumConverter<ToneMappingMode>(JsonNamingPolicy.CamelCase));
        return options;
    }

    public static string Serialize(ChartDefinition chart)
    {
        ArgumentNullException.ThrowIfNull(chart);
        return JsonSerializer.Serialize(chart, CreateOptions());
    }

    public static ChartDefinition Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        ChartDefinition chart = JsonSerializer.Deserialize<ChartDefinition>(json, CreateOptions())
            ?? throw new JsonException("JSON did not contain a ChartDefinition payload.");

        if (!string.Equals(chart.SchemaVersion, SchemaVersions.ChartCurrent, StringComparison.Ordinal))
        {
            throw new JsonException($"Unsupported schema version '{chart.SchemaVersion}'. Expected '{SchemaVersions.ChartCurrent}'.");
        }

        return chart;
    }

    private sealed class Rgb8JsonConverter : JsonConverter<Rgb8>
    {
        public override Rgb8 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            byte r = 0;
            byte g = 0;
            byte b = 0;

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected object for RGB color.");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return new Rgb8(r, g, b);
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name in RGB color object.");
                }

                string propertyName = reader.GetString()!;
                reader.Read();
                byte value = reader.GetByte();

                switch (propertyName.ToLowerInvariant())
                {
                    case "r":
                        r = value;
                        break;
                    case "g":
                        g = value;
                        break;
                    case "b":
                        b = value;
                        break;
                    default:
                        throw new JsonException($"Unexpected RGB property '{propertyName}'.");
                }
            }

            throw new JsonException("Unterminated RGB color object.");
        }

        public override void Write(Utf8JsonWriter writer, Rgb8 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("r", value.R);
            writer.WriteNumber("g", value.G);
            writer.WriteNumber("b", value.B);
            writer.WriteEndObject();
        }
    }

    private sealed class HdrColorJsonConverter : JsonConverter<HdrColor>
    {
        public override HdrColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            float r = 0.0f;
            float g = 0.0f;
            float b = 0.0f;

            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("Expected object for HDR color.");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    var color = new HdrColor(r, g, b);
                    if (!color.IsFinite || !color.IsNonNegative)
                    {
                        throw new JsonException("HDR color values must be finite and non-negative.");
                    }

                    return color;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("Expected property name in HDR color object.");
                }

                string propertyName = reader.GetString()!;
                reader.Read();
                float value = reader.GetSingle();

                switch (propertyName.ToLowerInvariant())
                {
                    case "r":
                        r = value;
                        break;
                    case "g":
                        g = value;
                        break;
                    case "b":
                        b = value;
                        break;
                    default:
                        throw new JsonException($"Unexpected HDR property '{propertyName}'.");
                }
            }

            throw new JsonException("Unterminated HDR color object.");
        }

        public override void Write(Utf8JsonWriter writer, HdrColor value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("r", value.R);
            writer.WriteNumber("g", value.G);
            writer.WriteNumber("b", value.B);
            writer.WriteEndObject();
        }
    }
}
