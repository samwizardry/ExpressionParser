namespace ExpressionParser;

public static class QueryableExtensions
{
    /// <summary>
    /// Filters a sequence of values based on a string expression.
    /// </summary>
    public static IQueryable<T> Where<T>(this IQueryable<T> source, string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return source;
        }

        var tokens = ExpressionParser.Tokenize(expression);
        var predicate = tokens.ToExpression<T>();

        return source.Where(predicate);
    }
}
