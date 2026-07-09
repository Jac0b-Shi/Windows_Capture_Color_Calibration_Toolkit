using System.Globalization;

namespace WgcColorCalibrator.Core.Rendering.HdrToSdr;

/// <summary>
/// Evaluates a restricted mathematical expression language.
/// Supported: + - * /, parentheses, unary +/-, variables, parameters,
/// and the functions clamp, saturate, pow, exp, log, sqrt, min, max.
/// No assignment, no member access, no external calls, no reflection.
/// </summary>
internal sealed class ExpressionEvaluator
{
    private readonly IReadOnlyDictionary<string, float> _parameters;
    private readonly INode _root;

    public ExpressionEvaluator(string expression, IReadOnlyDictionary<string, float> parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(expression);
        ArgumentNullException.ThrowIfNull(parameters);

        Expression = expression;
        _parameters = parameters;

        Token[] tokens = Tokenize(expression).ToArray();
        var parser = new Parser(tokens, this);
        _root = parser.ParseExpression();
        if (parser.Current.Type != TokenType.End)
        {
            throw new ExpressionParseException($"Unexpected token '{parser.Current.Text}' at position {parser.Current.Position}.");
        }
    }

    public string Expression { get; }

    public float Evaluate(IReadOnlyDictionary<string, float> variables)
    {
        ArgumentNullException.ThrowIfNull(variables);
        return _root.Evaluate(variables, _parameters);
    }

    private static IEnumerable<Token> Tokenize(string expression)
    {
        int position = 0;
        while (position < expression.Length)
        {
            char c = expression[position];

            if (char.IsWhiteSpace(c))
            {
                position++;
                continue;
            }

            if (char.IsDigit(c) || c == '.')
            {
                int start = position;
                bool seenDot = c == '.';
                position++;
                while (position < expression.Length)
                {
                    char next = expression[position];
                    if (char.IsDigit(next))
                    {
                        position++;
                    }
                    else if (next == '.' && !seenDot)
                    {
                        seenDot = true;
                        position++;
                    }
                    else
                    {
                        break;
                    }
                }

                string text = expression[start..position];
                if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    throw new ExpressionParseException($"Invalid number '{text}' at position {start}.");
                }

                yield return new Token(TokenType.Number, text, value, start);
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                int start = position;
                position++;
                while (position < expression.Length && (char.IsLetterOrDigit(expression[position]) || expression[position] == '_'))
                {
                    position++;
                }

                yield return new Token(TokenType.Identifier, expression[start..position], start);
                continue;
            }

            TokenType type = c switch
            {
                '+' => TokenType.Plus,
                '-' => TokenType.Minus,
                '*' => TokenType.Multiply,
                '/' => TokenType.Divide,
                '(' => TokenType.LeftParen,
                ')' => TokenType.RightParen,
                ',' => TokenType.Comma,
                _ => throw new ExpressionParseException($"Unexpected character '{c}' at position {position}.")
            };

            yield return new Token(type, c.ToString(), position);
            position++;
        }

        yield return new Token(TokenType.End, string.Empty, position);
    }

    private interface INode
    {
        float Evaluate(IReadOnlyDictionary<string, float> variables, IReadOnlyDictionary<string, float> parameters);
    }

    private sealed record LiteralNode(float Value) : INode
    {
        public float Evaluate(IReadOnlyDictionary<string, float> variables, IReadOnlyDictionary<string, float> parameters) => Value;
    }

    private sealed record VariableNode(string Name) : INode
    {
        public float Evaluate(IReadOnlyDictionary<string, float> variables, IReadOnlyDictionary<string, float> parameters)
        {
            if (variables.TryGetValue(Name, out float value))
            {
                return value;
            }

            throw new ExpressionParseException($"Variable '{Name}' is not defined.");
        }
    }

    private sealed record ParameterNode(string Name) : INode
    {
        public float Evaluate(IReadOnlyDictionary<string, float> variables, IReadOnlyDictionary<string, float> parameters)
        {
            if (parameters.TryGetValue(Name, out float value))
            {
                return value;
            }

            throw new ExpressionParseException($"Parameter '{Name}' is not defined.");
        }
    }

    private sealed record UnaryNode(UnaryOp Op, INode Operand) : INode
    {
        public float Evaluate(IReadOnlyDictionary<string, float> variables, IReadOnlyDictionary<string, float> parameters)
        {
            float value = Operand.Evaluate(variables, parameters);
            return Op == UnaryOp.Negate ? -value : value;
        }
    }

    private sealed record BinaryNode(BinaryOp Op, INode Left, INode Right) : INode
    {
        public float Evaluate(IReadOnlyDictionary<string, float> variables, IReadOnlyDictionary<string, float> parameters)
        {
            float left = Left.Evaluate(variables, parameters);
            float right = Right.Evaluate(variables, parameters);
            return Op switch
            {
                BinaryOp.Add => left + right,
                BinaryOp.Subtract => left - right,
                BinaryOp.Multiply => left * right,
                BinaryOp.Divide => right == 0.0f
                    ? throw new ExpressionParseException("Division by zero.")
                    : left / right,
                _ => throw new ExpressionParseException($"Unknown binary operator '{Op}'.")
            };
        }
    }

    private sealed record FunctionCallNode(string Name, IReadOnlyList<INode> Arguments) : INode
    {
        public float Evaluate(IReadOnlyDictionary<string, float> variables, IReadOnlyDictionary<string, float> parameters)
        {
            return Name.ToLowerInvariant() switch
            {
                "clamp" => EvaluateClamp(),
                "saturate" => EvaluateSaturate(),
                "pow" => EvaluatePow(),
                "exp" => EvaluateExp(),
                "log" => EvaluateLog(),
                "sqrt" => EvaluateSqrt(),
                "min" => EvaluateMin(),
                "max" => EvaluateMax(),
                _ => throw new ExpressionParseException($"Unknown function '{Name}'.")
            };

            float EvaluateArg(int index) => Arguments[index].Evaluate(variables, parameters);

            float EvaluateClamp()
            {
                if (Arguments.Count != 3)
                {
                    throw new ExpressionParseException("clamp(x, min, max) expects 3 arguments.");
                }

                float x = EvaluateArg(0);
                float min = EvaluateArg(1);
                float max = EvaluateArg(2);
                return x < min ? min : x > max ? max : x;
            }

            float EvaluateSaturate()
            {
                if (Arguments.Count != 1)
                {
                    throw new ExpressionParseException("saturate(x) expects 1 argument.");
                }

                float x = EvaluateArg(0);
                return x < 0.0f ? 0.0f : x > 1.0f ? 1.0f : x;
            }

            float EvaluatePow()
            {
                if (Arguments.Count != 2)
                {
                    throw new ExpressionParseException("pow(x, y) expects 2 arguments.");
                }

                return (float)Math.Pow(EvaluateArg(0), EvaluateArg(1));
            }

            float EvaluateExp()
            {
                if (Arguments.Count != 1)
                {
                    throw new ExpressionParseException("exp(x) expects 1 argument.");
                }

                return (float)Math.Exp(EvaluateArg(0));
            }

            float EvaluateLog()
            {
                if (Arguments.Count != 1)
                {
                    throw new ExpressionParseException("log(x) expects 1 argument.");
                }

                return (float)Math.Log(EvaluateArg(0));
            }

            float EvaluateSqrt()
            {
                if (Arguments.Count != 1)
                {
                    throw new ExpressionParseException("sqrt(x) expects 1 argument.");
                }

                return (float)Math.Sqrt(EvaluateArg(0));
            }

            float EvaluateMin()
            {
                if (Arguments.Count != 2)
                {
                    throw new ExpressionParseException("min(x, y) expects 2 arguments.");
                }

                return Math.Min(EvaluateArg(0), EvaluateArg(1));
            }

            float EvaluateMax()
            {
                if (Arguments.Count != 2)
                {
                    throw new ExpressionParseException("max(x, y) expects 2 arguments.");
                }

                return Math.Max(EvaluateArg(0), EvaluateArg(1));
            }
        }
    }

    private enum UnaryOp
    {
        Plus,
        Negate
    }

    private enum BinaryOp
    {
        Add,
        Subtract,
        Multiply,
        Divide
    }

    private enum TokenType
    {
        Number,
        Identifier,
        Plus,
        Minus,
        Multiply,
        Divide,
        LeftParen,
        RightParen,
        Comma,
        End
    }

    private readonly record struct Token(TokenType Type, string Text, float Value, int Position)
    {
        public Token(TokenType type, string text, int position)
            : this(type, text, 0.0f, position)
        {
        }
    }

    private sealed class Parser
    {
        private readonly Token[] _tokens;
        private readonly ExpressionEvaluator _evaluator;
        private int _index;

        public Parser(Token[] tokens, ExpressionEvaluator evaluator)
        {
            _tokens = tokens;
            _evaluator = evaluator;
            _index = 0;
        }

        public Token Current => _tokens[_index];

        public INode ParseExpression()
        {
            INode node = ParseTerm();

            while (Current.Type is TokenType.Plus or TokenType.Minus)
            {
                Token token = Current;
                Advance();
                INode right = ParseTerm();
                node = new BinaryNode(
                    token.Type == TokenType.Plus ? BinaryOp.Add : BinaryOp.Subtract,
                    node,
                    right);
            }

            return node;
        }

        private INode ParseTerm()
        {
            INode node = ParseUnary();

            while (Current.Type is TokenType.Multiply or TokenType.Divide)
            {
                Token token = Current;
                Advance();
                INode right = ParseUnary();
                node = new BinaryNode(
                    token.Type == TokenType.Multiply ? BinaryOp.Multiply : BinaryOp.Divide,
                    node,
                    right);
            }

            return node;
        }

        private INode ParseUnary()
        {
            if (Current.Type is TokenType.Plus or TokenType.Minus)
            {
                Token token = Current;
                Advance();
                INode operand = ParseUnary();
                return new UnaryNode(
                    token.Type == TokenType.Minus ? UnaryOp.Negate : UnaryOp.Plus,
                    operand);
            }

            return ParsePrimary();
        }

        private INode ParsePrimary()
        {
            if (Current.Type == TokenType.Number)
            {
                Token token = Current;
                Advance();
                return new LiteralNode(token.Value);
            }

            if (Current.Type == TokenType.Identifier)
            {
                string name = Current.Text;
                int position = Current.Position;
                Advance();

                if (Current.Type == TokenType.LeftParen)
                {
                    return ParseFunctionCall(name);
                }

                return ResolveIdentifier(name, position);
            }

            if (Current.Type == TokenType.LeftParen)
            {
                Advance();
                INode node = ParseExpression();
                if (Current.Type != TokenType.RightParen)
                {
                    throw new ExpressionParseException($"Expected ')' at position {Current.Position}.");
                }

                Advance();
                return node;
            }

            throw new ExpressionParseException($"Unexpected token '{Current.Text}' at position {Current.Position}.");
        }

        private FunctionCallNode ParseFunctionCall(string name)
        {
            Advance(); // consume '('
            var arguments = new List<INode>();

            if (Current.Type != TokenType.RightParen)
            {
                arguments.Add(ParseExpression());
                while (Current.Type == TokenType.Comma)
                {
                    Advance();
                    arguments.Add(ParseExpression());
                }
            }

            if (Current.Type != TokenType.RightParen)
            {
                throw new ExpressionParseException($"Expected ')' at position {Current.Position}.");
            }

            Advance(); // consume ')'

            if (!IsFunction(name))
            {
                throw new ExpressionParseException($"Unknown function '{name}' at position {Current.Position}.");
            }

            return new FunctionCallNode(name, arguments);
        }

        private INode ResolveIdentifier(string name, int position)
        {
            if (IsVariable(name))
            {
                return new VariableNode(name);
            }

            if (IsFunction(name))
            {
                throw new ExpressionParseException($"Function '{name}' must be called with parentheses at position {position}.");
            }

            if (!_evaluator._parameters.ContainsKey(name))
            {
                throw new ExpressionParseException($"Unknown identifier '{name}' at position {position}.");
            }

            return new ParameterNode(name);
        }

        private static bool IsVariable(string name)
        {
            return name.Equals("x", StringComparison.OrdinalIgnoreCase)
                || name.Equals("r", StringComparison.OrdinalIgnoreCase)
                || name.Equals("g", StringComparison.OrdinalIgnoreCase)
                || name.Equals("b", StringComparison.OrdinalIgnoreCase)
                || name.Equals("a", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsFunction(string name)
        {
            return name.Equals("clamp", StringComparison.OrdinalIgnoreCase)
                || name.Equals("saturate", StringComparison.OrdinalIgnoreCase)
                || name.Equals("pow", StringComparison.OrdinalIgnoreCase)
                || name.Equals("exp", StringComparison.OrdinalIgnoreCase)
                || name.Equals("log", StringComparison.OrdinalIgnoreCase)
                || name.Equals("sqrt", StringComparison.OrdinalIgnoreCase)
                || name.Equals("min", StringComparison.OrdinalIgnoreCase)
                || name.Equals("max", StringComparison.OrdinalIgnoreCase);
        }

        private void Advance()
        {
            if (_index < _tokens.Length - 1)
            {
                _index++;
            }
        }
    }
}

public sealed class ExpressionParseException : Exception
{
    public ExpressionParseException(string message)
        : base(message)
    {
    }
}
