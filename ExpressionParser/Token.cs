namespace ExpressionParser;

internal class Token
{
    public string Representation { get; }

    public TokenType TokenType { get; }

    public LiteralType LiteralType { get; }

    public Operator Operator { get; }

    public Token(string representation, TokenType tokenType, LiteralType literalType = LiteralType.None, Operator @operator = Operator.None)
    {
        Representation = representation;
        TokenType = tokenType;
        LiteralType = literalType;
        Operator = @operator;
    }
}
