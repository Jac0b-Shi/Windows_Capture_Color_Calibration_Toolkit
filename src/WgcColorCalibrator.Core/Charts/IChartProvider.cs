namespace WgcColorCalibrator.Core.Charts;

/// <summary>
/// Creates a built-in chart definition without exposing UI text directly.
/// </summary>
public interface IChartProvider
{
    string Id { get; }

    string NameResourceKey { get; }

    string DescriptionResourceKey { get; }

    ChartDefinition Create(ChartGenerationOptions options);
}

