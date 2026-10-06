using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ExpressionParser;

internal static partial class ExpressionParser
{
    // Регулярное выражение для токенизации входной строки выражения
    private static readonly Regex s_tokenizerRegex = TokenizerRegex();

    // Паттерн для распознавания числовых литералов
    private static readonly Regex s_numberRegex = NumberRegex();

    // Множество поддерживаемых операторов сравнения и логики
    private static readonly HashSet<string> Operators = new(StringComparer.OrdinalIgnoreCase)
    {
        "And",
        "Or",
        "Not",
        "Eq",
        "Neq",
        "Lt",
        "Leq",
        "Gt",
        "Geq",
        "Contains",
        "StartsWith",
        "EndsWith"
    };

    // Рефлексивное получение строковых методов для трансляции в SQL
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)])!;
    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod("EndsWith", [typeof(string)])!;

    // Константные выражения для ключевых слов
    private static readonly ConstantExpression NullConstant = Expression.Constant(null);
    private static readonly ConstantExpression TrueConstant = Expression.Constant(true, typeof(bool));
    private static readonly ConstantExpression FalseConstant = Expression.Constant(false, typeof(bool));

    // Приоритеты операторов для построения дерева выражений
    private static readonly Dictionary<string, int> Precedence = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Or", 0 },
        { "And", 1 },
        { "Eq", 10 },
        { "Neq", 10 },
        { "Lt", 10 },
        { "Leq", 10 },
        { "Gt", 10 },
        { "Geq", 10 },
        { "Contains", 20 },
        { "StartsWith", 20 },
        { "EndsWith", 20 },
        { "Not", 100 }
    };

    // Словарь функций построения выражений по операторам
    private static readonly Dictionary<string, Func<Stack<Expression>, Expression>> OperatorFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        { "And", And },
        { "Or", Or },
        { "Not", Not },
        { "Eq", Eq },
        { "Neq", Neq },
        { "Lt", Lt },
        { "Leq", Leq },
        { "Gt", Gt },
        { "Geq", Geq },
        { "Contains", Contains },
        { "StartsWith", StartsWith },
        { "EndsWith", EndsWith }
    };

    /// <summary>
    /// Токенизация выражения с использованием алгоритма Дейкстры (Shunting-Yard)
    /// для перевода инфиксной записи в обратную польскую нотацию (RPN).
    /// </summary>
    public static Queue<Token> Tokenize(string expression)
    {
        // Очередь токенов в постфиксной записи (RPN)
        var tokens = new Queue<Token>();

        // Стек для временного хранения операторов
        var operators = new Stack<Token>();

        foreach (Match match in s_tokenizerRegex.Matches(expression))
        {
            // Получаем текстовое представление токена
            string representation = match.Value;

            // Если токен — строковый литерал (одинарные кавычки)
            if (representation[0] == '\'' && representation[representation.Length - 1] == '\'')
            {
                tokens.Enqueue(new Token(representation.Substring(1, representation.Length - 2), TokenType.Literal, LiteralType.Character));
            }
            // Если токен — число
            else if (s_numberRegex.IsMatch(representation))
            {
                tokens.Enqueue(new Token(representation, TokenType.Literal, LiteralType.Numeric));
            }
            // Если токен — булево значение
            else if (bool.TryParse(representation, out bool boolVal))
            {
                tokens.Enqueue(new Token(representation, TokenType.Literal, LiteralType.Boolean));
            }
            // Если токен — системное ключевое слово
            else if (representation.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                tokens.Enqueue(new Token(representation, TokenType.Keyword));
            }
            // Если токен — один из операторов сравнения или логики
            else if (Operators.Contains(representation))
            {
                var token = new Token(representation, TokenType.Operator);

                // Выталкиваем из стека операторы с большим или равным приоритетом
                while (operators.Count != 0 && operators.Peek().Representation != "(")
                {
                    if (Precedence[operators.Peek().Representation] >= Precedence[token.Representation])
                    {
                        tokens.Enqueue(operators.Pop());
                    }
                    else
                    {
                        break;
                    }
                }

                operators.Push(token);
            }
            // Если токен — открывающая круглая скобка
            else if (representation == "(")
            {
                operators.Push(new Token(representation, TokenType.Separator));
            }
            // Если токен — закрывающая круглая скобка
            else if (representation == ")")
            {
                // Переносим операторы в выходную очередь до тех пор, пока не встретим открывающую скобку
                while (operators.Count != 0 && operators.Peek().Representation != "(")
                {
                    tokens.Enqueue(operators.Pop());
                }

                // Извлекаем и удаляем открывающую скобку из стека
                if (operators.Count != 0)
                    operators.Pop();
            }
            // Иначе токен интерпретируется как идентификатор свойства (колонка таблицы)
            else
            {
                tokens.Enqueue(new Token(representation, TokenType.Identifier));
            }
        }

        // Переносим все оставшиеся операторы из стека в выходную очередь
        while (operators.Count != 0)
        {
            tokens.Enqueue(operators.Pop());
        }

        return tokens;
    }

    /// <summary>
    /// Преобразует очередь токенов RPN в лямбда-выражение LINQ Expression Tree.
    /// </summary>
    public static Expression<Func<T, bool>> ToExpression<T>(this Queue<Token> tokens)
    {
        Stack<Expression> expressions = new Stack<Expression>();
        var parameter = Expression.Parameter(typeof(T), "p");

        while (tokens.Count > 0)
        {
            var token = tokens.Dequeue();
            // Вызываем обработчик в зависимости от типа токена
            switch (token.TokenType)
            {
                case TokenType.Identifier:
                    ProceedIdentifier(expressions, parameter, token);
                    break;
                case TokenType.Literal:
                    ProceedLiteral(expressions, parameter, token);
                    break;
                case TokenType.Operator:
                    ProceedOperator(expressions, parameter, token);
                    break;
                case TokenType.Keyword:
                    ProceedKeyword(expressions, parameter, token);
                    break;
                default:
                    throw new NotSupportedException($"Unsupported token type: {token.TokenType}.");
            }
        }

        return Expression.Lambda<Func<T, bool>>(expressions.Pop(), parameter);
    }

    // Обработка идентификатора свойства (извлекает свойство по имени из параметра лямбды)
    private static void ProceedIdentifier(Stack<Expression> expressions, ParameterExpression parameter, Token token)
    {
        expressions.Push(Expression.Property(parameter, token.Representation));
    }

    // Обработка литерального токена (строка, число или boolean)
    private static void ProceedLiteral(Stack<Expression> expressions, ParameterExpression parameter, Token token)
    {
        switch (token.LiteralType)
        {
            case LiteralType.Numeric:
            case LiteralType.Character:
                expressions.Push(Expression.Constant(token.Representation));
                break;
            case LiteralType.Boolean:
                expressions.Push(bool.Parse(token.Representation) ? TrueConstant : FalseConstant);
                break;
            default:
                throw new InvalidOperationException(nameof(token.LiteralType));
        }
    }

    // Обработка оператора (извлекает выражение оператора на основе OperatorFunctions)
    private static void ProceedOperator(Stack<Expression> expressions, ParameterExpression parameter, Token token)
    {
        expressions.Push(OperatorFunctions[token.Representation](expressions));
    }

    // Обработка ключевого слова
    private static void ProceedKeyword(Stack<Expression> expressions, ParameterExpression parameter, Token token)
    {
        // Так как у нас один keyword (null), можно без проверок сразу его добавить
        expressions.Push(NullConstant);
    }

    // Логическое И
    private static Expression And(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        return Expression.And(left, right);
    }

    // Логическое ИЛИ
    private static Expression Or(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        return Expression.Or(left, right);
    }

    // Логическое ОТРИЦАНИЕ
    private static Expression Not(Stack<Expression> expressions)
    {
        return Expression.Not(expressions.Pop());
    }

    // Оператор Равно (eq)
    private static Expression Eq(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.Equal(exps.left, exps.right);
    }

    // Оператор Не Равно (ne)
    private static Expression Neq(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.NotEqual(exps.left, exps.right);
    }

    // Оператор Меньше (lt / before)
    private static Expression Lt(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.LessThan(exps.left, exps.right);
    }

    // Оператор Меньше или Равно (lte / beforeeq)
    private static Expression Leq(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.LessThanOrEqual(exps.left, exps.right);
    }

    // Оператор Больше (gt / after)
    private static Expression Gt(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.GreaterThan(exps.left, exps.right);
    }

    // Оператор Больше или Равно (gte / aftereq)
    private static Expression Geq(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.GreaterThanOrEqual(exps.left, exps.right);
    }

    // Проверка содержания подстроки (contain)
    private static Expression Contains(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.Call(exps.left, ContainsMethod, exps.right);
    }

    // Проверка начала строки (start)
    private static Expression StartsWith(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.Call(exps.left, StartsWithMethod, exps.right);
    }

    // Проверка окончания строки (end)
    private static Expression EndsWith(Stack<Expression> expressions)
    {
        var right = expressions.Pop();
        var left = expressions.Pop();
        var exps = ConvertExpressions(left, right);
        return Expression.Call(exps.left, EndsWithMethod, exps.right);
    }

    private static Func<ConstantExpression, ConstantExpression> GetConstantExpressionByTypeCode(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        bool isNullable = true;

        if (underlying is null)
        {
            underlying = type;
            isNullable = false;
        }

        switch (Type.GetTypeCode(underlying))
        {
            case TypeCode.Empty:
                throw new NotImplementedException();

            case TypeCode.Object:
                {
                    switch (underlying.Name)
                    {
                        case "DateOnly":
                            return isNullable ? ToNullableDateOnlyConstantExpression : ToDateOnlyConstantExpression;

                        case "TimeOnly":
                            return isNullable ? ToNullableTimeOnlyConstantExpression : ToTimeOnlyConstantExpression;

                        default:
                            throw new NotSupportedException($"Unsupported type: {type.FullName}.");
                    }
                }

            case TypeCode.Boolean:
                return isNullable ? ToNullableBooleanConstantExpression : ToBooleanConstantExpression;
            case TypeCode.Char:
                return isNullable ? ToNullableCharConstantExpression : ToCharConstantExpression;
            case TypeCode.SByte:
                throw new NotImplementedException();
            case TypeCode.Byte:
                return isNullable ? ToNullableInt8ConstantExpression : ToInt8ConstantExpression;
            case TypeCode.Int16:
                return isNullable ? ToNullableInt16ConstantExpression : ToInt16ConstantExpression;
            case TypeCode.UInt16:
                throw new NotImplementedException();
            case TypeCode.Int32:
                return isNullable ? ToNullableInt32ConstantExpression : ToInt32ConstantExpression;
            case TypeCode.UInt32:
                throw new NotImplementedException();
            case TypeCode.Int64:
                return isNullable ? ToNullableInt64ConstantExpression : ToInt64ConstantExpression;
            case TypeCode.UInt64:
                throw new NotImplementedException();
            case TypeCode.Single:
                throw new NotImplementedException();
            case TypeCode.Double:
                return isNullable ? ToNullableDoubleConstantExpression : ToDoubleConstantExpression;
            case TypeCode.Decimal:
                return isNullable ? ToNullableDecimalConstantExpression : ToDecimalConstantExpression;
            case TypeCode.DateTime:
                return isNullable ? ToNullableDateTimeConstantExpression : ToDateTimeConstantExpression;
            case TypeCode.String:
                return isNullable ? ToNullableStringConstantExpression : ToStringConstantExpression;
            default:
                throw new NotSupportedException($"Unsupported type: {type.FullName}.");
        }
    }

    /// <summary>
    /// Автоматическое приведение типов константных выражений к типам свойств C#-класса.
    /// Без этой конвертации EF Core падает при попытке сравнить строку с типом Nullable или числом.
    /// </summary>
    private static (Expression left, Expression right) ConvertExpressions(Expression left, Expression right)
    {
        if (left.NodeType == ExpressionType.MemberAccess)
        {
            var constantExpression = (ConstantExpression)right;

            if (constantExpression.Value is null)
            {
                right = constantExpression;
            }
            else
            {
                right = GetConstantExpressionByTypeCode(left.Type)(constantExpression);
            }
        }
        else if (right.NodeType == ExpressionType.MemberAccess)
        {
            var constantExpression = (ConstantExpression)left;

            if (constantExpression.Value is null)
            {
                left = constantExpression;
            }
            else
            {
                left = GetConstantExpressionByTypeCode(right.Type)(constantExpression);
            }
        }

        return (left, right);
    }

    // Конвертация в тип bool
    private static ConstantExpression ToBooleanConstantExpression(ConstantExpression literal)
    {
        return literal;
    }

    // Конвертация в тип Nullable<bool>
    private static ConstantExpression ToNullableBooleanConstantExpression(ConstantExpression literal)
    {
        object representation = literal.Value!;
        bool? value = Convert.ToBoolean(representation);
        return Expression.Constant(value, typeof(bool?));
    }

    // Конвертация в тип Char
    private static ConstantExpression ToCharConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        char value = char.Parse(representation);
        return Expression.Constant(value, typeof(char));
    }

    // Конвертация в тип Nullable<Char>
    private static ConstantExpression ToNullableCharConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        char? value = char.Parse(representation);
        return Expression.Constant(value, typeof(char?));
    }

    // Конвертация в тип byte
    private static ConstantExpression ToInt8ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        byte value = byte.Parse(representation);
        return Expression.Constant(value, typeof(byte));
    }

    // Конвертация в тип Nullable<byte>
    private static ConstantExpression ToNullableInt8ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        byte? value = byte.Parse(representation);
        return Expression.Constant(value, typeof(byte?));
    }

    // Конвертация в тип short
    private static ConstantExpression ToInt16ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        short value = short.Parse(representation);
        return Expression.Constant(value, typeof(short));
    }

    // Конвертация в тип Nullable<short>
    private static ConstantExpression ToNullableInt16ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        short? value = short.Parse(representation);
        return Expression.Constant(value, typeof(short?));
    }

    // Конвертация в тип int
    private static ConstantExpression ToInt32ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        int value = int.Parse(representation);
        return Expression.Constant(value, typeof(int));
    }

    // Конвертация в тип Nullable<int>
    private static ConstantExpression ToNullableInt32ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        int? value = int.Parse(representation);
        return Expression.Constant(value, typeof(int?));
    }

    // Конвертация в тип long
    private static ConstantExpression ToInt64ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        long value = long.Parse(representation);
        return Expression.Constant(value, typeof(long));
    }

    // Конвертация в тип Nullable<long>
    private static ConstantExpression ToNullableInt64ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        long? value = long.Parse(representation);
        return Expression.Constant(value, typeof(long?));
    }

    // Конвертация в тип double
    private static ConstantExpression ToDoubleConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        double value = double.Parse(representation, CultureInfo.InvariantCulture);
        return Expression.Constant(value, typeof(double));
    }

    // Конвертация в тип Nullable<double>
    private static ConstantExpression ToNullableDoubleConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        double? value = double.Parse(representation, CultureInfo.InvariantCulture);
        return Expression.Constant(value, typeof(double?));
    }

    // Конвертация в тип decimal
    private static ConstantExpression ToDecimalConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        decimal value = decimal.Parse(representation, CultureInfo.InvariantCulture);
        return Expression.Constant(value, typeof(decimal));
    }

    // Конвертация в тип Nullable<decimal>
    private static ConstantExpression ToNullableDecimalConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        decimal? value = decimal.Parse(representation, CultureInfo.InvariantCulture);
        return Expression.Constant(value, typeof(decimal?));
    }

    // Конвертация в тип DateTime
    private static ConstantExpression ToDateTimeConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        DateTime dateTime = DateTimeOffset.Parse(representation).DateTime;
        return Expression.Constant(dateTime, typeof(DateTime));
    }

    // Конвертация в тип Nullable<DateTime>
    private static ConstantExpression ToNullableDateTimeConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        DateTime? dateTime = DateTimeOffset.Parse(representation).DateTime;
        return Expression.Constant(dateTime, typeof(DateTime?));
    }

    // Конвертация в тип DateOnly
    private static ConstantExpression ToDateOnlyConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        DateOnly dateOnly = DateOnly.FromDateTime(DateTimeOffset.Parse(representation).DateTime);
        return Expression.Constant(dateOnly, typeof(DateOnly));
    }

    // Конвертация в тип Nullable<DateOnly>
    private static ConstantExpression ToNullableDateOnlyConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        DateOnly? dateOnly = DateOnly.FromDateTime(DateTimeOffset.Parse(representation).DateTime);
        return Expression.Constant(dateOnly, typeof(DateOnly?));
    }

    // Конвертация в тип TimeOnly
    private static ConstantExpression ToTimeOnlyConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        TimeOnly timeOnly = TimeOnly.FromDateTime(DateTimeOffset.Parse(representation).DateTime);
        return Expression.Constant(timeOnly, typeof(TimeOnly));
    }

    // Конвертация в тип Nullable<TimeOnly>
    private static ConstantExpression ToNullableTimeOnlyConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        TimeOnly? timeOnly = TimeOnly.FromDateTime(DateTimeOffset.Parse(representation).DateTime);
        return Expression.Constant(timeOnly, typeof(TimeOnly?));
    }

    // Конвертация в тип string
    private static ConstantExpression ToStringConstantExpression(ConstantExpression literal)
    {
        return literal;
    }

    // Конвертация в тип Nullable<string>
    private static ConstantExpression ToNullableStringConstantExpression(ConstantExpression literal)
    {
        string? representation = literal.Value!.ToString();
        return Expression.Constant(representation);
    }

    [GeneratedRegex(@"\b[\p{L}_][\p{L}0-9_]*\b|'(?:''|[^'])*'|[-+]?\d*\.?\d+|[()]", RegexOptions.IgnoreCase)]
    private static partial Regex TokenizerRegex();

    [GeneratedRegex(@"^[-+]?\d*\.?\d+$")]
    private static partial Regex NumberRegex();

}