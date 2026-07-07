using WgcColorCalibrator.App.Windows;

namespace WgcColorCalibrator.App.Services;

/// <summary>
/// Creates chart windows without exposing implementation details to callers.
/// </summary>
public interface IChartWindowFactory
{
    ChartWindow Create();
}

/// <summary>
/// Default implementation that creates a new chart window.
/// </summary>
public sealed class ChartWindowFactory : IChartWindowFactory
{
    public ChartWindow Create() => new();
}
