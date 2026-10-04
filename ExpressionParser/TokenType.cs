namespace ExpressionParser;

internal enum TokenType
{
    // Names the programmer chooses
    // samples: x, color, UP
    Identifier,
    // Numeric, logical, textual, reference literals
    // samples: true, 6.02e23, "music"
    Literal,
    // Symbols that operate on arguments and produce results
    // samples: +, <, =
    Operator,
    // Names already in the programming language
    // samples: if, while, return
    Keyword,
    // (also known as punctuators): punctuation characters and paired-delimiters
    // samples: }, (, ;
    Separator
}
