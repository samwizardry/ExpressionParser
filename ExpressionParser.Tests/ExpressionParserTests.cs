using System.Linq.Expressions;
using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace ExpressionParser.Tests;

public class ExpressionParserTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private TestDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _context = new TestDbContext(_postgresContainer.GetConnectionString());
        await _context.Database.EnsureCreatedAsync();

        await SeedDataAsync();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }

    private async Task SeedDataAsync()
    {
        var users = new List<User>
        {
            new()
            {
                Id = 1,
                String = "Alice",
                NullableString = "ActiveUser",
                ByteVal = 10,
                NullableByteVal = 20,
                ShortVal = 100,
                NullableShortVal = 200,
                IntVal = 1000,
                NullableIntVal = 2000,
                LongVal = 10000L,
                NullableLongVal = 20000L,
                DoubleVal = 10.5,
                NullableDoubleVal = 20.5,
                DecimalVal = 100.50m,
                NullableDecimalVal = 200.75m,
                DateOnlyVal = new DateOnly(2025, 1, 1),
                NullableDateOnlyVal = new DateOnly(2025, 6, 1),
                BoolVal = true,
                NullableBoolVal = false
            },
            new()
            {
                Id = 2,
                String = "Bob",
                NullableString = null,
                ByteVal = 30,
                NullableByteVal = null,
                ShortVal = 300,
                NullableShortVal = null,
                IntVal = 3000,
                NullableIntVal = null,
                LongVal = 30000L,
                NullableLongVal = null,
                DoubleVal = 30.5,
                NullableDoubleVal = null,
                DecimalVal = 300.50m,
                NullableDecimalVal = null,
                DateOnlyVal = new DateOnly(2026, 1, 1),
                NullableDateOnlyVal = null,
                BoolVal = false,
                NullableBoolVal = null
            },
            new()
            {
                Id = 3,
                String = "Charlie",
                NullableString = "AdminUser",
                ByteVal = 50,
                NullableByteVal = 50,
                ShortVal = 500,
                NullableShortVal = 500,
                IntVal = 5000,
                NullableIntVal = 5000,
                LongVal = 50000L,
                NullableLongVal = 50000L,
                DoubleVal = 50.5,
                NullableDoubleVal = 50.5,
                DecimalVal = 500.50m,
                NullableDecimalVal = 500.50m,
                DateOnlyVal = new DateOnly(2027, 1, 1),
                NullableDateOnlyVal = new DateOnly(2027, 12, 31),
                BoolVal = true,
                NullableBoolVal = true
            }
        };

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task Queryable_EmptyOrWhitespaceExpression_ReturnsSourceWithoutFiltering()
    {
        var allUsers = await _context.Users.ToListAsync();

        (await _context.Users.Where(null!).ToListAsync()).Count.Should().Be(allUsers.Count);
        (await _context.Users.Where("").ToListAsync()).Count.Should().Be(allUsers.Count);
        (await _context.Users.Where("   ").ToListAsync()).Count.Should().Be(allUsers.Count);
    }

    [Fact]
    public void Tokenize_CoversAllTokenTypesLiteralsAndParenthesesBranches()
    {
        // String with escaped quotes
        var tokens = ExpressionParser.Tokenize("String eq 'O''Reilly'");
        tokens.Select(t => t.Representation).Should().Contain("O''Reilly");

        // Numbers (positive, negative, float), booleans, null, parentheses
        var complexExpression = "(IntVal gt -10 and DoubleVal leq +30.5) or (BoolVal eq false and NullableIntVal eq null)";
        var parsed = ExpressionParser.Tokenize(complexExpression);
        parsed.Should().NotBeEmpty();

        // Operator precedence break branch: lower precedence followed by higher precedence (Or then And)
        var orAnd = ExpressionParser.Tokenize("BoolVal eq true Or String eq 'Alice' And IntVal gt 5");
        orAnd.Should().NotBeEmpty();

        // Operator precedence greater-or-equal branch: higher precedence followed by lower (And then Or)
        var andOr = ExpressionParser.Tokenize("BoolVal eq true And String eq 'Alice' Or IntVal gt 5");
        andOr.Should().NotBeEmpty();

        // Operator inside parentheses with peek '('
        var inParen = ExpressionParser.Tokenize("(BoolVal eq true and String eq 'Alice')");
        inParen.Should().NotBeEmpty();

        // Unmatched close parenthesis (operators.Count == 0 on pop)
        var unmatchedClose = ExpressionParser.Tokenize("BoolVal eq true)");
        unmatchedClose.Should().NotBeEmpty();

        // Unmatched open parenthesis (drain remaining in stack)
        var unmatchedOpen = ExpressionParser.Tokenize("(BoolVal eq true");
        unmatchedOpen.Should().NotBeEmpty();

        // Cyrillic identifier
        var cyrillic = ExpressionParser.Tokenize("Имя eq 'Тест'");
        cyrillic.Should().NotBeEmpty();
    }

    [Fact]
    public void ProceedLiteral_InvalidLiteralType_ThrowsInvalidOperationException()
    {
        var action = () =>
        {
            var tokens = new Queue<Token>();
            tokens.Enqueue(new Token("test", TokenType.Literal, (LiteralType)999));
            tokens.ToExpression<User>();
        };

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*LiteralType*");
    }

    [Fact]
    public void InternalDictionaries_InvokeDirectMethodsAndFallbacks()
    {
        // ToNullableStringConstantExpression via dictionary
        var dictField = typeof(ExpressionParser).GetField("IdentifierTypeConstantExpressions", BindingFlags.NonPublic | BindingFlags.Static)!;
        var dict = (Dictionary<string, Func<ConstantExpression, ConstantExpression>>)dictField.GetValue(null)!;
        
        var nullableStringRes = dict["Nullable`1String"](Expression.Constant("someString"));
        nullableStringRes.Value.Should().Be("someString");

        // Int8 and Nullable`1Int8 keys
        dict["Int8"](Expression.Constant("42")).Value.Should().Be((byte)42);
        dict["Nullable`1Int8"](Expression.Constant("42")).Value.Should().Be((byte?)42);
    }

    [Fact]
    public async Task Filtering_ConstantExpressions_NeitherMemberAccess()
    {
        var users = await _context.Users.Where("1 eq 1").ToListAsync();
        users.Count.Should().Be(3);

        var noUsers = await _context.Users.Where("1 neq 1").ToListAsync();
        noUsers.Should().BeEmpty();
    }

    [Fact]
    public async Task Filtering_LogicalOperators_And_Or_Not()
    {
        // And
        var andUsers = await _context.Users.Where("BoolVal eq true And IntVal gt 2000").ToListAsync();
        andUsers.Select(u => u.String).Should().ContainSingle().Which.Should().Be("Charlie");

        // Or
        var orUsers = await _context.Users.Where("String eq 'Alice' Or String eq 'Bob'").ToListAsync();
        orUsers.Select(u => u.String).Should().BeEquivalentTo(["Alice", "Bob"]);

        // Not
        var notUsers = await _context.Users.Where("Not (BoolVal eq true)").ToListAsync();
        notUsers.Select(u => u.String).Should().ContainSingle().Which.Should().Be("Bob");
    }

    [Fact]
    public async Task Filtering_StringOperators_Contains_StartsWith_EndsWith()
    {
        var containsUsers = await _context.Users.Where("String Contains 'li'").ToListAsync();
        containsUsers.Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie"]);

        var startsWithUsers = await _context.Users.Where("String StartsWith 'Al'").ToListAsync();
        startsWithUsers.Select(u => u.String).Should().ContainSingle().Which.Should().Be("Alice");

        var endsWithUsers = await _context.Users.Where("String EndsWith 'e'").ToListAsync();
        endsWithUsers.Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie"]);

        var endsWithIe = await _context.Users.Where("String EndsWith 'ie'").ToListAsync();
        endsWithIe.Select(u => u.String).Should().Equal("Charlie");
    }

    [Fact]
    public async Task Filtering_ComparisonOperators_AllKinds()
    {
        // Lt, Leq, Gt, Geq
        (await _context.Users.Where("IntVal lt 3000").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Alice");

        (await _context.Users.Where("IntVal leq 3000").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Alice", "Bob"]);

        (await _context.Users.Where("IntVal gt 3000").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Charlie");

        (await _context.Users.Where("IntVal geq 3000").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Bob", "Charlie"]);

        // Neq
        (await _context.Users.Where("String Neq 'Alice'").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Bob", "Charlie"]);
    }

    [Fact]
    public async Task Filtering_AllDataTypes_LeftAndRightMemberAccess_NonNullAndNull()
    {
        // Byte and Nullable<Byte>
        (await _context.Users.Where("ByteVal eq 10").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("10 eq ByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableByteVal eq 20").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("20 eq NullableByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableByteVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // Short and Nullable<Short>
        (await _context.Users.Where("ShortVal eq 100").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("100 eq ShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableShortVal eq 200").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("200 eq NullableShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableShortVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // Int and Nullable<Int>
        (await _context.Users.Where("IntVal eq 1000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("1000 eq IntVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableIntVal eq 2000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("2000 eq NullableIntVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableIntVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableIntVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // Long and Nullable<Long>
        (await _context.Users.Where("LongVal eq 10000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("10000 eq LongVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableLongVal eq 20000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("20000 eq NullableLongVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableLongVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableLongVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // Double and Nullable<Double>
        (await _context.Users.Where("DoubleVal eq 10.5").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("10.5 eq DoubleVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDoubleVal eq 20.5").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("20.5 eq NullableDoubleVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDoubleVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableDoubleVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // Decimal and Nullable<Decimal>
        (await _context.Users.Where("DecimalVal eq 100.50").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("100.50 eq DecimalVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDecimalVal eq 200.75").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("200.75 eq NullableDecimalVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDecimalVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableDecimalVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // DateOnly and Nullable<DateOnly>
        (await _context.Users.Where("DateOnlyVal eq '2025-01-01'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'2025-01-01' eq DateOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDateOnlyVal eq '2025-06-01'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'2025-06-01' eq NullableDateOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDateOnlyVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableDateOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // String and Nullable String
        (await _context.Users.Where("String eq 'Alice'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'Alice' eq String").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableString eq 'ActiveUser'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'ActiveUser' eq NullableString").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableString eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableString").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // Boolean and Nullable<Boolean> (both true and false)
        (await _context.Users.Where("BoolVal eq true").ToListAsync()).Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie"]);
        (await _context.Users.Where("true eq BoolVal").ToListAsync()).Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie"]);
        (await _context.Users.Where("BoolVal eq false").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("false eq BoolVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("NullableBoolVal eq false").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("false eq NullableBoolVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableBoolVal eq true").ToListAsync()).Select(u => u.String).Should().Equal("Charlie");
        (await _context.Users.Where("true eq NullableBoolVal").ToListAsync()).Select(u => u.String).Should().Equal("Charlie");
        (await _context.Users.Where("NullableBoolVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableBoolVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
    }
}
