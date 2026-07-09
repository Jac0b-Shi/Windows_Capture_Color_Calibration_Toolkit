using System.Globalization;
using WgcColorCalibrator.Core.Colors;

namespace WgcColorCalibrator.Core.Rendering.HdrToSdr;

/// <summary>
/// A user-defined HDR-to-SDR operator described by a restricted mathematical expression.
/// The expression is evaluated independently for each RGB channel; alpha is passed through unchanged.
/// </summary>
public sealed class CustomExpressionOperator : IHdrToSdrOperator
{
    private readonly ExpressionEvaluator _evaluator;
    private readonly Dictionary<string, float> _variables;

    public CustomExpressionOperator(string expression, IReadOnlyDictionary<string, float> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(parameters);

        _evaluator = new ExpressionEvaluator(expression, parameters);
        _variables = new Dictionary<string, float>(StringComparer.Ordinal)
        {
            ["x"] = 0.0f,
            ["r"] = 0.0f,
            ["g"] = 0.0f,
            ["b"] = 0.0f,
            ["a"] = 0.0f,
        };
    }

    public string Id => "custom-expression";

    public string Expression => _evaluator.Expression;

    public RgbaFloat Map(RgbaFloat hdr)
    {
        _variables["r"] = hdr.R;
        _variables["g"] = hdr.G;
        _variables["b"] = hdr.B;
        _variables["a"] = hdr.A;

        _variables["x"] = hdr.R;
        float r = Saturate(_evaluator.Evaluate(_variables));

        _variables["x"] = hdr.G;
        float g = Saturate(_evaluator.Evaluate(_variables));

        _variables["x"] = hdr.B;
        float b = Saturate(_evaluator.Evaluate(_variables));

        return new RgbaFloat(r, g, b, hdr.A);
    }

    private static float Saturate(float value)
    {
        return value < 0.0f ? 0.0f : value > 1.0f ? 1.0f : value;
    }
}
