namespace WgcColorCalibrator.Core.Errors;

/// <summary>
/// Represents a structured application error with a stable machine-readable code.
/// </summary>
public sealed record AppError(
    string Code,
    string MessageResourceKey,
    ErrorSeverity Severity,
    string? TechnicalDetails,
    Exception? Exception);

