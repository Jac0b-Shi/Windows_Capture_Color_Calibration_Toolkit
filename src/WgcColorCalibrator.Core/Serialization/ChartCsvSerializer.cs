using System.Globalization;
using System.Text;
using WgcColorCalibrator.Core.Charts;
using WgcColorCalibrator.Core.Colors;
using WgcColorCalibrator.Core.Layout;

namespace WgcColorCalibrator.Core.Serialization;

/// <summary>
/// Reads and writes the MVP chart CSV format.
/// </summary>
public static class ChartCsvSerializer
{
    private const string Header = "id,label,r,g,b,category,weight";

    public static string SerializePatches(IEnumerable<ColorPatchDefinition> patches)
    {
        ArgumentNullException.ThrowIfNull(patches);

        var builder = new StringBuilder();
        builder.AppendLine(Header);

        foreach (ColorPatchDefinition patch in patches)
        {
            builder.Append(Escape(patch.Id)).Append(',')
                .Append(Escape(patch.Label)).Append(',')
                .Append(patch.ExpectedColor.R.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(patch.ExpectedColor.G.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(patch.ExpectedColor.B.ToString(CultureInfo.InvariantCulture)).Append(',')
                .Append(Escape(patch.Category ?? string.Empty)).Append(',')
                .Append(patch.Weight.ToString(CultureInfo.InvariantCulture))
                .AppendLine();
        }

        return builder.ToString();
    }

    public static ChartDefinition DeserializeChart(string csv, string chartId, string chartName, ChartLayoutDefinition layout)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csv);
        ArgumentException.ThrowIfNullOrWhiteSpace(chartId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chartName);
        ArgumentNullException.ThrowIfNull(layout);

        string[] lines = SplitCsvRecords(csv);

        if (lines.Length == 0 || !string.Equals(lines[0], Header, StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException("CSV header must be id,label,r,g,b,category,weight.");
        }

        var patches = new List<ColorPatchDefinition>();
        for (int index = 1; index < lines.Length; index++)
        {
            string[] columns = ParseLine(lines[index]);
            if (columns.Length != 7)
            {
                throw new FormatException($"CSV line {index + 1} must contain 7 columns.");
            }

            byte r = ParseByte(columns[2], index, "r");
            byte g = ParseByte(columns[3], index, "g");
            byte b = ParseByte(columns[4], index, "b");
            double weight = ParseWeight(columns[6], index);

            patches.Add(new ColorPatchDefinition(
                columns[0],
                columns[1],
                new Rgb8(r, g, b),
                string.IsNullOrWhiteSpace(columns[5]) ? null : columns[5],
                weight,
                null));
        }

        return new ChartDefinition(chartId, chartName, patches, layout, new Dictionary<string, string>
        {
            ["source"] = "csv"
        });
    }

    private static byte ParseByte(string value, int lineIndex, string columnName)
    {
        if (!byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte result))
        {
            throw new FormatException($"CSV line {lineIndex + 1} has invalid {columnName} value.");
        }

        return result;
    }

    private static double ParseWeight(string value, int lineIndex)
    {
        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double result))
        {
            throw new FormatException($"CSV line {lineIndex + 1} has invalid weight value.");
        }

        if (double.IsNaN(result) || double.IsInfinity(result))
        {
            throw new FormatException($"CSV line {lineIndex + 1} has invalid weight value: weight must be a finite number.");
        }

        return result;
    }

    /// <summary>
    /// Splits CSV text into logical records, respecting that newlines inside
    /// quoted fields are part of the field value, not record separators.
    /// </summary>
    private static string[] SplitCsvRecords(string csv)
    {
        var records = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int index = 0; index < csv.Length; index++)
        {
            char character = csv[index];

            if (inQuotes)
            {
                // Escaped quote ("") inside a quoted field
                if (character == '"' && index + 1 < csv.Length && csv[index + 1] == '"')
                {
                    current.Append("\"\"");
                    index++;
                }
                else if (character == '"')
                {
                    inQuotes = false;
                    current.Append(character);
                }
                else
                {
                    current.Append(character);
                }
            }
            else if (character == '"')
            {
                inQuotes = true;
                current.Append(character);
            }
            else if (character == '\r')
            {
                // CRLF or standalone CR: end of record
                FlushRecord();
                // Skip trailing LF after CR (CRLF)
                if (index + 1 < csv.Length && csv[index + 1] == '\n')
                {
                    index++;
                }
            }
            else if (character == '\n')
            {
                // Standalone LF: end of record
                FlushRecord();
            }
            else
            {
                current.Append(character);
            }
        }

        // Emit final record and check for unclosed quotes
        bool endedInQuotes = inQuotes;
        FlushRecord();

        if (endedInQuotes)
        {
            throw new FormatException("CSV contains an unclosed quoted field.");
        }

        return records.ToArray();

        void FlushRecord()
        {
            string record = current.ToString();
            current.Clear();
            inQuotes = false;

            // Trim trailing CR and reject empty records (trailing newline artifacts)
            record = record.Trim('\r');
            if (record.Length > 0)
            {
                records.Add(record);
            }
        }
    }

    private static string Escape(string value)
    {
        if (value.Contains('"', StringComparison.Ordinal) ||
            value.Contains(',', StringComparison.Ordinal) ||
            value.Contains('\n', StringComparison.Ordinal) ||
            value.Contains('\r', StringComparison.Ordinal))
        {
            return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
        }

        return value;
    }

    private static string[] ParseLine(string line)
    {
        var columns = new List<string>();
        var current = new StringBuilder();
        bool inQuotes = false;

        for (int index = 0; index < line.Length; index++)
        {
            char character = line[index];

            if (inQuotes)
            {
                if (character == '"' && index + 1 < line.Length && line[index + 1] == '"')
                {
                    current.Append('"');
                    index++;
                }
                else if (character == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    current.Append(character);
                }
            }
            else if (character == ',')
            {
                columns.Add(current.ToString());
                current.Clear();
            }
            else if (character == '"')
            {
                inQuotes = true;
            }
            else
            {
                current.Append(character);
            }
        }

        if (inQuotes)
        {
            throw new FormatException("CSV contains an unclosed quoted field.");
        }

        columns.Add(current.ToString());
        return columns.ToArray();
    }
}

