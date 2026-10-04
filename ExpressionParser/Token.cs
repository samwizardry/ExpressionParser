namespace ExpressionParser;

internal class Token
{
    public string Representation { get; }

    public TokenType TokenType { get; }

    public LiteralType? LiteralType { get; }

    public Token(string representation, TokenType tokenType, LiteralType? literalType = null)
    {
        Representation = representation;
        TokenType = tokenType;
        LiteralType = literalType;
    }
}
