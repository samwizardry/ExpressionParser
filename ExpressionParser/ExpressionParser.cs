using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ExpressionParser;

internal static class ExpressionParser
{
    // Регулярное выражение для токенизации входной строки выражения
    private static readonly Regex Tokenizer = new Regex(@"(?:\b)[A-Za-zА-Яа-я_]+(?:\b)|'(?:(?:''|[^'])*)'|[-+]?\d*\.?\d+|[()]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Паттерн для распознавания числовых литералов
    private static readonly Regex NumberPattern = new Regex(@"[-+]?\d*\.?\d+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Множество поддерживаемых операторов сравнения и логики
    private static readonly HashSet<string> Operators =
        ["and", "or", "not", "eq", "ne", "lt", "lte", "gt", "gte", "before", "beforeeq", "after", "aftereq", "contain", "start", "end"];

    // Поддерживаемые логические константы
    private static readonly HashSet<string> Literals = ["true", "false"];

    // Поддерживаемые ключевые слова
    private static readonly HashSet<string> Keywords = ["null"];

    // Рефлексивное получение строковых методов для трансляции в SQL
    private static readonly MethodInfo ContainsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;
    private static readonly MethodInfo StartsWithMethod = typeof(string).GetMethod("StartsWith", [typeof(string)])!;
    private static readonly MethodInfo EndsWithMethod = typeof(string).GetMethod("EndsWith", [typeof(string)])!;

    // Приоритеты операторов для построения дерева выражений
    private static readonly Dictionary<string, int> Precedence = new()
    {
        { "or", 0 },
        { "and", 1 },
        { "eq", 10 },
        { "ne", 10 },
        { "lt", 10 },
        { "lte", 10 },
        { "gt", 10 },
        { "gte", 10 },
        { "after", 10 },
        { "aftereq", 10 },
        { "before", 10 },
        { "beforeeq", 10 },
        { "contains", 20 },
        { "starts", 20 },
        { "ends", 20 },
        { "not", 100 }
    };

    // Процедуры обработки токенов в зависимости от их типа
    private static readonly Dictionary<TokenType, Action<Stack<Expression>, ParameterExpression, Token>> TokenTypeProcedures = new()
    {
        { TokenType.Identifier, ProceedIdentifier },
        { TokenType.Literal, ProceedLiteral },
        { TokenType.Operator, ProceedOperator },
        { TokenType.Keyword, ProceedKeyword }
    };

    // Кэшированные константные выражения для булевых литералов
    private static readonly Dictionary<string, ConstantExpression> LiteralExpressions = new()
    {
        { "true", Expression.Constant(true, typeof(bool)) },
        { "false", Expression.Constant(false, typeof(bool)) }
    };

    // Словарь функций построения выражений по операторам
    private static readonly Dictionary<string, Func<Stack<Expression>, Expression>> OperatorFunctions = new()
    {
        { "and", And },
        { "or", Or },
        { "not", Not },
        { "eq", Eq },
        { "ne", Neq },
        { "lt", Lt },
        { "lte", Leq },
        { "gt", Gt },
        { "gte", Geq },
        { "before", Lt },
        { "beforeeq", Leq },
        { "after", Gt },
        { "aftereq", Geq },
        { "contains", Contains },
        { "starts", StartsWith },
        { "ends", EndsWith }
    };

    // Кэшированные константные выражения для ключевых слов
    private static readonly Dictionary<string, ConstantExpression> KeywordExpressions = new()
    {
        { "null", Expression.Constant(null) }
    };

    // Фабрики для конвертации констант во внутренние типы C#
    private static readonly Dictionary<string, Func<ConstantExpression, ConstantExpression>> IdentifierTypeConstantExpressions = new()
    {
        { "Int8", ToInt8ConstantExpression },
        { "Nullable`1Int8", ToNullableInt8ConstantExpression },
        { "Int16", ToInt16ConstantExpression },
        { "Nullable`1Int16", ToNullableInt16ConstantExpression },
        { "Int32", ToInt32ConstantExpression },
        { "Nullable`1Int32", ToNullableInt32ConstantExpression },
        { "Int64", ToInt64ConstantExpression },
        { "Nullable`1Int64", ToNullableInt64ConstantExpression },
        { "Double", ToDoubleConstantExpression },
        { "Nullable`1Double", ToNullableDoubleConstantExpression },
        { "Decimal", ToDecimalConstantExpression },
        { "Nullable`1Decimal", ToNullableDecimalConstantExpression },
        { "DateOnly", ToDateOnlyConstantExpression },
        { "Nullable`1DateOnly", ToNullableDateOnlyConstantExpression },
        { "String", ToStringConstantExpression },
        { "Nullable`1String", ToNullableStringConstantExpression },
        { "Boolean", ToBooleanConstantExpression },
        { "Nullable`1Boolean", ToNullableBooleanConstantExpression },
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

        foreach (Match match in Tokenizer.Matches(expression))
        {
            // Получаем текстовое представление токена
            string representation = match.Value;

            // Если токен — строковый литерал (одинарные кавычки)
            if (representation[0] == '\'' && representation[representation.Length - 1] == '\'')
            {
                tokens.Enqueue(new Token(representation.Substring(1, representation.Length - 2), TokenType.Literal, LiteralType.Character));
            }
            // Если токен — число
            else if (NumberPattern.IsMatch(representation))
            {
                tokens.Enqueue(new Token(representation, TokenType.Literal, LiteralType.Numeric));
            }
            // Если токен — булево значение
            else if (Literals.Contains(representation.ToLower()))
            {
                tokens.Enqueue(new Token(representation.ToLower(), TokenType.Literal, LiteralType.Boolean));
            }
            // Если токен — системное ключевое слово
            else if (Keywords.Contains(representation.ToLower()))
            {
                tokens.Enqueue(new Token(representation.ToLower(), TokenType.Keyword));
            }
            // Если токен — один из операторов сравнения или логики
            else if (Operators.Contains(representation.ToLower()))
            {
                var token = new Token(representation.ToLower(), TokenType.Operator);

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
            TokenTypeProcedures[token.TokenType](expressions, parameter, token);
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
                expressions.Push(LiteralExpressions[token.Representation]);
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
        expressions.Push(KeywordExpressions[token.Representation]);
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

    /// <summary>
    /// Автоматическое приведение типов константных выражений к типам свойств C#-класса.
    /// Без этой конвертации EF Core падает при попытке сравнить строку с типом Nullable или числом.
    /// </summary>
    private static (Expression left, Expression right) ConvertExpressions(Expression left, Expression right)
    {
        if (left.NodeType == ExpressionType.MemberAccess)
        {
            var constantExpression = (ConstantExpression)right;

            if (left.Type.IsGenericType && left.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (constantExpression.Value is null)
                {
                    right = constantExpression;
                }
                else
                {
                    right = IdentifierTypeConstantExpressions[left.Type.Name + Nullable.GetUnderlyingType(left.Type)!.Name](constantExpression);
                }
            }
            else
            {
                right = IdentifierTypeConstantExpressions[left.Type.Name](constantExpression);
            }
        }
        else if (right.NodeType == ExpressionType.MemberAccess)
        {
            var constantExpression = (ConstantExpression)left;

            if (right.Type.IsGenericType && right.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (constantExpression.Value is null)
                {
                    left = constantExpression;
                }
                else
                {
                    left = IdentifierTypeConstantExpressions[right.Type.Name + Nullable.GetUnderlyingType(right.Type)!.Name](constantExpression);
                }
            }
            else
            {
                left = IdentifierTypeConstantExpressions[right.Type.Name](constantExpression);
            }
        }

        return (left, right);
    }

    // Конвертация в тип byte (Int8)
    private static ConstantExpression ToInt8ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        byte value = byte.Parse(representation);
        return Expression.Constant(value);
    }

    // Конвертация в тип Nullable<byte>
    private static ConstantExpression ToNullableInt8ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        byte? value = byte.Parse(representation);
        return Expression.Constant(value, typeof(byte?));
    }

    // Конвертация в тип short (Int16)
    private static ConstantExpression ToInt16ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        short value = short.Parse(representation);
        return Expression.Constant(value);
    }

    // Конвертация в тип Nullable<short>
    private static ConstantExpression ToNullableInt16ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        short? value = short.Parse(representation);
        return Expression.Constant(value, typeof(short?));
    }

    // Конвертация в тип int (Int32)
    private static ConstantExpression ToInt32ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        int value = int.Parse(representation);
        return Expression.Constant(value);
    }

    // Конвертация в тип Nullable<int>
    private static ConstantExpression ToNullableInt32ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        int? value = int.Parse(representation);
        return Expression.Constant(value, typeof(int?));
    }

    // Конвертация в тип long (Int64)
    private static ConstantExpression ToInt64ConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        long value = long.Parse(representation);
        return Expression.Constant(value);
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
        return Expression.Constant(value);
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
        return Expression.Constant(value);
    }

    // Конвертация в тип Nullable<decimal>
    private static ConstantExpression ToNullableDecimalConstantExpression(ConstantExpression literal)
    {
        string representation = literal.Value!.ToString()!;
        decimal? value = decimal.Parse(representation, CultureInfo.InvariantCulture);
        return Expression.Constant(value, typeof(decimal?));
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
}