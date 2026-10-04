using BenchmarkDotNet.Attributes;

using ExpressionParser.Benchmark.Models;

namespace ExpressionParser.Benchmark;

public class CommonBenchmark
{
    private const string Expression = "Name eq 'John' and ((Age geq 18 and Age leq 60) or Status eq 'Staff')";

    [Benchmark]
    public void Tokenize()
    {
        var tokens = ExpressionParser.Tokenize(Expression);
    }

    [Benchmark]
    public void ToExpression()
    {
        var tokens = ExpressionParser.Tokenize(Expression);
        var expression = tokens.ToExpression<User>();
    }
}
