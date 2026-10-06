using System;
using System.Collections.Generic;
using System.Text;

namespace ExpressionParser;

internal enum Operator
{
    And,
    Or,
    Not,
    Eq,
    Neq,
    Lt,
    Leq,
    Gt,
    Geq,
    Contains,
    StartsWith,
    EndsWith
}
