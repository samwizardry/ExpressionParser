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
                // byte / sbyte
                ByteVal = 10,
                NullableByteVal = 20,
                SByteVal = -10,
                NullableSByteVal = -20,
                // short / ushort
                ShortVal = 100,
                NullableShortVal = 200,
                UShortVal = 100,
                NullableUShortVal = 200,
                // int / uint
                IntVal = 1000,
                NullableIntVal = 2000,
                UIntVal = 1000,
                NullableUIntVal = 2000,
                // long / ulong
                LongVal = 10000L,
                NullableLongVal = 20000L,
                ULongVal = 10000UL,
                NullableULongVal = 20000UL,
                // float
                FloatVal = 1.5f,
                NullableFloatVal = 2.5f,
                // double
                DoubleVal = 10.5,
                NullableDoubleVal = 20.5,
                // decimal
                DecimalVal = 100.50m,
                NullableDecimalVal = 200.75m,
                // char
                CharVal = 'A',
                NullableCharVal = 'Z',
                // date / time
                DateOnlyVal = new DateOnly(2025, 1, 1),
                NullableDateOnlyVal = new DateOnly(2025, 6, 1),
                DateTimeVal = new DateTime(2025, 1, 1, 10, 0, 0),
                NullableDateTimeVal = new DateTime(2025, 6, 1, 12, 0, 0),
                TimeOnlyVal = new TimeOnly(10, 0, 0),
                NullableTimeOnlyVal = new TimeOnly(12, 0, 0),
                // bool
                BoolVal = true,
                NullableBoolVal = false
            },
            new()
            {
                Id = 2,
                String = "Bob",
                NullableString = null,
                // byte / sbyte
                ByteVal = 30,
                NullableByteVal = null,
                SByteVal = -30,
                NullableSByteVal = null,
                // short / ushort
                ShortVal = 300,
                NullableShortVal = null,
                UShortVal = 300,
                NullableUShortVal = null,
                // int / uint
                IntVal = 3000,
                NullableIntVal = null,
                UIntVal = 3000,
                NullableUIntVal = null,
                // long / ulong
                LongVal = 30000L,
                NullableLongVal = null,
                ULongVal = 30000UL,
                NullableULongVal = null,
                // float
                FloatVal = 3.5f,
                NullableFloatVal = null,
                // double
                DoubleVal = 30.5,
                NullableDoubleVal = null,
                // decimal
                DecimalVal = 300.50m,
                NullableDecimalVal = null,
                // char
                CharVal = 'B',
                NullableCharVal = null,
                // date / time
                DateOnlyVal = new DateOnly(2026, 1, 1),
                NullableDateOnlyVal = null,
                DateTimeVal = new DateTime(2026, 1, 1, 10, 0, 0),
                NullableDateTimeVal = null,
                TimeOnlyVal = new TimeOnly(14, 0, 0),
                NullableTimeOnlyVal = null,
                // bool
                BoolVal = false,
                NullableBoolVal = null
            },
            new()
            {
                Id = 3,
                String = "Charlie",
                NullableString = "AdminUser",
                // byte / sbyte
                ByteVal = 50,
                NullableByteVal = 50,
                SByteVal = 50,
                NullableSByteVal = 50,
                // short / ushort
                ShortVal = 500,
                NullableShortVal = 500,
                UShortVal = 500,
                NullableUShortVal = 500,
                // int / uint
                IntVal = 5000,
                NullableIntVal = 5000,
                UIntVal = 5000,
                NullableUIntVal = 5000,
                // long / ulong
                LongVal = 50000L,
                NullableLongVal = 50000L,
                ULongVal = 50000UL,
                NullableULongVal = 50000UL,
                // float
                FloatVal = 5.5f,
                NullableFloatVal = 5.5f,
                // double
                DoubleVal = 50.5,
                NullableDoubleVal = 50.5,
                // decimal
                DecimalVal = 500.50m,
                NullableDecimalVal = 500.50m,
                // char
                CharVal = 'C',
                NullableCharVal = 'C',
                // date / time
                DateOnlyVal = new DateOnly(2027, 1, 1),
                NullableDateOnlyVal = new DateOnly(2027, 12, 31),
                DateTimeVal = new DateTime(2027, 1, 1, 10, 0, 0),
                NullableDateTimeVal = new DateTime(2027, 12, 31, 23, 59, 59),
                TimeOnlyVal = new TimeOnly(20, 0, 0),
                NullableTimeOnlyVal = new TimeOnly(23, 59, 59),
                // bool
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

        // SByte and Nullable<SByte>
        (await _context.Users.Where("SByteVal eq -10").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("-10 eq SByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableSByteVal eq -20").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("-20 eq NullableSByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableSByteVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableSByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // UShort and Nullable<UShort>
        (await _context.Users.Where("UShortVal eq 100").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("100 eq UShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableUShortVal eq 200").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("200 eq NullableUShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableUShortVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableUShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // UInt and Nullable<UInt>
        (await _context.Users.Where("UIntVal eq 1000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("1000 eq UIntVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableUIntVal eq 2000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("2000 eq NullableUIntVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableUIntVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableUIntVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // ULong and Nullable<ULong>
        (await _context.Users.Where("ULongVal eq 10000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("10000 eq ULongVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableULongVal eq 20000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("20000 eq NullableULongVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableULongVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableULongVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // Float and Nullable<Float>
        (await _context.Users.Where("FloatVal eq 1.5").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("1.5 eq FloatVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableFloatVal eq 2.5").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("2.5 eq NullableFloatVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableFloatVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableFloatVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // Char and Nullable<Char>
        (await _context.Users.Where("CharVal eq 'A'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'A' eq CharVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableCharVal eq 'Z'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'Z' eq NullableCharVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableCharVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableCharVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // DateTime and Nullable<DateTime>
        (await _context.Users.Where("DateTimeVal eq '2025-01-01T10:00:00'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'2025-01-01T10:00:00' eq DateTimeVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDateTimeVal eq '2025-06-01T12:00:00'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'2025-06-01T12:00:00' eq NullableDateTimeVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDateTimeVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableDateTimeVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");

        // TimeOnly and Nullable<TimeOnly>
        (await _context.Users.Where("TimeOnlyVal eq '10:00:00'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'10:00:00' eq TimeOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableTimeOnlyVal eq '12:00:00'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'12:00:00' eq NullableTimeOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableTimeOnlyVal eq null").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
        (await _context.Users.Where("null eq NullableTimeOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Bob");
    }

    [Fact]
    public void GetConstantExpressionByTypeCode_AllNewTypes_ReturnsCorrectValues()
    {
        var methodInfo = typeof(ExpressionParser).GetMethod("GetConstantExpressionByTypeCode", BindingFlags.NonPublic | BindingFlags.Static)!;
        var getConstantExpressionByTypeCode = (Func<Type, Func<ConstantExpression, ConstantExpression>>)Delegate.CreateDelegate(
            typeof(Func<Type, Func<ConstantExpression, ConstantExpression>>),
            methodInfo);

        // sbyte / Nullable<sbyte>
        getConstantExpressionByTypeCode(typeof(sbyte))(Expression.Constant("-10")).Value.Should().Be((sbyte)-10);
        getConstantExpressionByTypeCode(typeof(sbyte?))(Expression.Constant("-10")).Value.Should().Be((sbyte?)-10);

        // ushort / Nullable<ushort>
        getConstantExpressionByTypeCode(typeof(ushort))(Expression.Constant("100")).Value.Should().Be((ushort)100);
        getConstantExpressionByTypeCode(typeof(ushort?))(Expression.Constant("100")).Value.Should().Be((ushort?)100);

        // uint / Nullable<uint>
        getConstantExpressionByTypeCode(typeof(uint))(Expression.Constant("1000")).Value.Should().Be((uint)1000);
        getConstantExpressionByTypeCode(typeof(uint?))(Expression.Constant("1000")).Value.Should().Be((uint?)1000);

        // ulong / Nullable<ulong>
        getConstantExpressionByTypeCode(typeof(ulong))(Expression.Constant("10000")).Value.Should().Be((ulong)10000);
        getConstantExpressionByTypeCode(typeof(ulong?))(Expression.Constant("10000")).Value.Should().Be((ulong?)10000);

        // float / Nullable<float>
        getConstantExpressionByTypeCode(typeof(float))(Expression.Constant("1.5")).Value.Should().Be(1.5f);
        getConstantExpressionByTypeCode(typeof(float?))(Expression.Constant("1.5")).Value.Should().Be((float?)1.5f);

        // double / Nullable<double>
        getConstantExpressionByTypeCode(typeof(double))(Expression.Constant("10.5")).Value.Should().Be(10.5);
        getConstantExpressionByTypeCode(typeof(double?))(Expression.Constant("10.5")).Value.Should().Be((double?)10.5);

        // decimal / Nullable<decimal>
        getConstantExpressionByTypeCode(typeof(decimal))(Expression.Constant("100.50")).Value.Should().Be(100.50m);
        getConstantExpressionByTypeCode(typeof(decimal?))(Expression.Constant("100.50")).Value.Should().Be((decimal?)100.50m);

        // char / Nullable<char>
        getConstantExpressionByTypeCode(typeof(char))(Expression.Constant("A")).Value.Should().Be('A');
        getConstantExpressionByTypeCode(typeof(char?))(Expression.Constant("A")).Value.Should().Be((char?)'A');

        // DateTime / Nullable<DateTime>
        // Note: ToDateTimeConstantExpression uses DateTimeOffset.Parse(...).DateTime which yields Kind=Unspecified
        var dtLiteral = Expression.Constant("2025-01-01T10:00:00");
        var expectedDt = DateTimeOffset.Parse("2025-01-01T10:00:00").DateTime; // Kind=Unspecified
        getConstantExpressionByTypeCode(typeof(DateTime))(dtLiteral).Value.Should().Be(expectedDt);
        getConstantExpressionByTypeCode(typeof(DateTime?))(dtLiteral).Value.Should().Be((DateTime?)expectedDt);

        // DateOnly / Nullable<DateOnly>
        var dateLiteral = Expression.Constant("2025-01-01");
        var expectedDate = new DateOnly(2025, 1, 1);
        getConstantExpressionByTypeCode(typeof(DateOnly))(dateLiteral).Value.Should().Be(expectedDate);
        getConstantExpressionByTypeCode(typeof(DateOnly?))(dateLiteral).Value.Should().Be((DateOnly?)expectedDate);

        // TimeOnly / Nullable<TimeOnly>
        var timeLiteral = Expression.Constant("10:00:00");
        var expectedTime = new TimeOnly(10, 0, 0);
        getConstantExpressionByTypeCode(typeof(TimeOnly))(timeLiteral).Value.Should().Be(expectedTime);
        getConstantExpressionByTypeCode(typeof(TimeOnly?))(timeLiteral).Value.Should().Be((TimeOnly?)expectedTime);

        // Unsupported Object type throws
        var act = () => getConstantExpressionByTypeCode(typeof(object));
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public async Task Filtering_NewNumericTypes_ComparisonOperators()
    {
        // SByte comparisons
        (await _context.Users.Where("SByteVal lt -10").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Bob");

        (await _context.Users.Where("SByteVal gt -30").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Alice", "Charlie"]);

        // UInt comparisons
        (await _context.Users.Where("UIntVal lt 3000").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Alice");

        (await _context.Users.Where("UIntVal gt 3000").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Charlie");

        // ULong comparisons
        (await _context.Users.Where("ULongVal leq 10000").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Alice");

        (await _context.Users.Where("ULongVal geq 50000").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Charlie");

        // Float comparisons
        (await _context.Users.Where("FloatVal lt 3.5").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Alice");

        (await _context.Users.Where("FloatVal gt 3.5").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Charlie");
    }

    [Fact]
    public async Task Filtering_DateTimeAndTimeOnly_ComparisonOperators()
    {
        // DateTime comparisons
        (await _context.Users.Where("DateTimeVal lt '2026-01-01T10:00:00'").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Alice");

        (await _context.Users.Where("DateTimeVal gt '2026-01-01T10:00:00'").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Charlie");

        (await _context.Users.Where("DateTimeVal geq '2026-01-01T10:00:00'").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Bob", "Charlie"]);

        // TimeOnly comparisons
        (await _context.Users.Where("TimeOnlyVal lt '14:00:00'").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Alice");

        (await _context.Users.Where("TimeOnlyVal gt '14:00:00'").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Charlie");
    }
}
