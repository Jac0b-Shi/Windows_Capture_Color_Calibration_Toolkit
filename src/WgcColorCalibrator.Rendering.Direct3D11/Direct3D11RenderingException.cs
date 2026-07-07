namespace WgcColorCalibrator.Rendering.Direct3D11;

/// <summary>
/// Exception thrown when Direct3D11 rendering fails in a way that should be surfaced to the caller.
/// </summary>
public sealed class Direct3D11RenderingException : Exception
{
    public Direct3D11RenderingException(string message)
        : base(message)
    {
    }

    public Direct3D11RenderingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
