using System.Linq.Expressions;
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
                SByteVal = -10,
                NullableSByteVal = -20,
                ShortVal = 100,
                NullableShortVal = 200,
                UShortVal = 100,
                NullableUShortVal = 200,
                IntVal = 1000,
                NullableIntVal = 2000,
                UIntVal = 1000,
                NullableUIntVal = 2000,
                LongVal = 10000L,
                NullableLongVal = 20000L,
                ULongVal = 10000UL,
                NullableULongVal = 20000UL,
                FloatVal = 1.5f,
                NullableFloatVal = 2.5f,
                DoubleVal = 10.5,
                NullableDoubleVal = 20.5,
                DecimalVal = 100.50m,
                NullableDecimalVal = 200.75m,
                CharVal = 'A',
                NullableCharVal = 'Z',
                DateOnlyVal = new DateOnly(2025, 1, 1),
                NullableDateOnlyVal = new DateOnly(2025, 6, 1),
                DateTimeVal = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                NullableDateTimeVal = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                TimeOnlyVal = new TimeOnly(10, 0, 0),
                NullableTimeOnlyVal = new TimeOnly(12, 0, 0),
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
                SByteVal = -30,
                NullableSByteVal = null,
                ShortVal = 300,
                NullableShortVal = null,
                UShortVal = 300,
                NullableUShortVal = null,
                IntVal = 3000,
                NullableIntVal = null,
                UIntVal = 3000,
                NullableUIntVal = null,
                LongVal = 30000L,
                NullableLongVal = null,
                ULongVal = 30000UL,
                NullableULongVal = null,
                FloatVal = 3.5f,
                NullableFloatVal = null,
                DoubleVal = 30.5,
                NullableDoubleVal = null,
                DecimalVal = 300.50m,
                NullableDecimalVal = null,
                CharVal = 'B',
                NullableCharVal = null,
                DateOnlyVal = new DateOnly(2026, 1, 1),
                NullableDateOnlyVal = null,
                DateTimeVal = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                NullableDateTimeVal = null,
                TimeOnlyVal = new TimeOnly(14, 0, 0),
                NullableTimeOnlyVal = null,
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
                SByteVal = 50,
                NullableSByteVal = 50,
                ShortVal = 500,
                NullableShortVal = 500,
                UShortVal = 500,
                NullableUShortVal = 500,
                IntVal = 5000,
                NullableIntVal = 5000,
                UIntVal = 5000,
                NullableUIntVal = 5000,
                LongVal = 50000L,
                NullableLongVal = 50000L,
                ULongVal = 50000UL,
                NullableULongVal = 50000UL,
                FloatVal = 5.5f,
                NullableFloatVal = 5.5f,
                DoubleVal = 50.5,
                NullableDoubleVal = 50.5,
                DecimalVal = 500.50m,
                NullableDecimalVal = 500.50m,
                CharVal = 'C',
                NullableCharVal = 'C',
                DateOnlyVal = new DateOnly(2027, 1, 1),
                NullableDateOnlyVal = new DateOnly(2027, 12, 31),
                DateTimeVal = new DateTime(2027, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                NullableDateTimeVal = new DateTime(2027, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                TimeOnlyVal = new TimeOnly(20, 0, 0),
                NullableTimeOnlyVal = new TimeOnly(23, 59, 59),
                BoolVal = true,
                NullableBoolVal = true
            },
            new()
            {
                Id = 4,
                String = "O'Reilly",
                NullableString = "Special'User",
                ByteVal = 70,
                NullableByteVal = 70,
                SByteVal = 70,
                NullableSByteVal = 70,
                ShortVal = 700,
                NullableShortVal = 700,
                UShortVal = 700,
                NullableUShortVal = 700,
                IntVal = 7000,
                NullableIntVal = 3000,
                UIntVal = 7000,
                NullableUIntVal = 3000,
                LongVal = 70000L,
                NullableLongVal = 30000L,
                ULongVal = 70000UL,
                NullableULongVal = 30000UL,
                FloatVal = 7.5f,
                NullableFloatVal = 3.5f,
                DoubleVal = 70.5,
                NullableDoubleVal = 30.5,
                DecimalVal = 700.50m,
                NullableDecimalVal = 300.50m,
                CharVal = 'D',
                NullableCharVal = 'D',
                DateOnlyVal = new DateOnly(2028, 1, 1),
                NullableDateOnlyVal = new DateOnly(2028, 12, 31),
                DateTimeVal = new DateTime(2028, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                NullableDateTimeVal = new DateTime(2028, 6, 1, 12, 0, 0, DateTimeKind.Utc),
                TimeOnlyVal = new TimeOnly(18, 0, 0),
                NullableTimeOnlyVal = new TimeOnly(19, 0, 0),
                BoolVal = false,
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
        // String with escaped quotes: 'O''Reilly' should unescape to "O'Reilly"
        var tokens = ExpressionParser.Tokenize("String eq 'O''Reilly'");
        tokens.Select(t => t.Representation).Should().Contain("O'Reilly");

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
        users.Count.Should().Be(4);

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

        // Not with comparison
        var notUsers = await _context.Users.Where("Not (BoolVal eq true)").ToListAsync();
        notUsers.Select(u => u.String).Should().BeEquivalentTo(["Bob", "O'Reilly"]);

        // Direct boolean identifier
        var boolTrueUsers = await _context.Users.Where("BoolVal").ToListAsync();
        boolTrueUsers.Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie"]);

        // Direct Not boolean identifier
        var notBoolUsers = await _context.Users.Where("Not BoolVal").ToListAsync();
        notBoolUsers.Select(u => u.String).Should().BeEquivalentTo(["Bob", "O'Reilly"]);

        // Complex precedence with parentheses
        var complexUsers = await _context.Users.Where("(IntVal gt 2000 and BoolVal eq true) or String eq 'Alice'").ToListAsync();
        complexUsers.Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie"]);
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

        // Filter by string with escaped single quote in SQL
        var quoteUsers = await _context.Users.Where("String eq 'O''Reilly'").ToListAsync();
        quoteUsers.Select(u => u.String).Should().ContainSingle().Which.Should().Be("O'Reilly");

        // Nullable string operators
        var nullableStringUsers = await _context.Users.Where("NullableString StartsWith 'Active'").ToListAsync();
        nullableStringUsers.Select(u => u.String).Should().ContainSingle().Which.Should().Be("Alice");
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
            .Should().BeEquivalentTo(["Charlie", "O'Reilly"]);

        (await _context.Users.Where("IntVal geq 5000").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Charlie", "O'Reilly"]);

        // Neq
        (await _context.Users.Where("String Neq 'Alice'").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Bob", "Charlie", "O'Reilly"]);
    }

    [Fact]
    public async Task Filtering_PropertyToPropertyComparison()
    {
        // Comparing two non-nullable properties of same type: Id lt IntVal (1 < 1000, 2 < 3000, 3 < 5000, 4 < 7000)
        var idLtInt = await _context.Users.Where("Id lt IntVal").ToListAsync();
        idLtInt.Count.Should().Be(4);

        // Comparing non-nullable property with nullable property of same underlying type: IntVal eq NullableIntVal (Charlie: 5000 == 5000)
        var sameInt = await _context.Users.Where("IntVal eq NullableIntVal").ToListAsync();
        sameInt.Select(u => u.String).Should().ContainSingle().Which.Should().Be("Charlie");

        // Comparing non-nullable property with nullable property: IntVal gt NullableIntVal (O'Reilly: 7000 > 3000)
        var greaterNullable = await _context.Users.Where("IntVal gt NullableIntVal").ToListAsync();
        greaterNullable.Select(u => u.String).Should().ContainSingle().Which.Should().Be("O'Reilly");

        // Comparing two date properties: DateTimeVal lt NullableDateTimeVal (Alice, Charlie, O'Reilly)
        var dateCompare = await _context.Users.Where("DateTimeVal lt NullableDateTimeVal").ToListAsync();
        dateCompare.Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie", "O'Reilly"]);
    }

    [Fact]
    public async Task Filtering_NullHandling_NonNullAndNull()
    {
        // Null checks on nullable types
        (await _context.Users.Where("NullableIntVal eq null").ToListAsync()).Select(u => u.String)
            .Should().Equal("Bob");

        (await _context.Users.Where("null eq NullableIntVal").ToListAsync()).Select(u => u.String)
            .Should().Equal("Bob");

        (await _context.Users.Where("NullableIntVal neq null").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Alice", "Charlie", "O'Reilly"]);

        (await _context.Users.Where("NullableString eq null").ToListAsync()).Select(u => u.String)
            .Should().Equal("Bob");

        (await _context.Users.Where("null eq NullableString").ToListAsync()).Select(u => u.String)
            .Should().Equal("Bob");

        (await _context.Users.Where("NullableDateTimeVal eq null").ToListAsync()).Select(u => u.String)
            .Should().Equal("Bob");
    }

    [Fact]
    public async Task Filtering_AllDataTypes_LeftAndRightMemberAccess()
    {
        // Byte and Nullable<Byte>
        (await _context.Users.Where("ByteVal eq 10").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("10 eq ByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableByteVal eq 20").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("20 eq NullableByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // SByte and Nullable<SByte>
        (await _context.Users.Where("SByteVal eq -10").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("-10 eq SByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableSByteVal eq -20").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("-20 eq NullableSByteVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // Short and Nullable<Short>
        (await _context.Users.Where("ShortVal eq 100").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("100 eq ShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableShortVal eq 200").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("200 eq NullableShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // UShort and Nullable<UShort>
        (await _context.Users.Where("UShortVal eq 100").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("100 eq UShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableUShortVal eq 200").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("200 eq NullableUShortVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // Int and Nullable<Int>
        (await _context.Users.Where("IntVal eq 1000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("1000 eq IntVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableIntVal eq 2000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("2000 eq NullableIntVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // UInt and Nullable<UInt>
        (await _context.Users.Where("UIntVal eq 1000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("1000 eq UIntVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableUIntVal eq 2000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("2000 eq NullableUIntVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // Long and Nullable<Long>
        (await _context.Users.Where("LongVal eq 10000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("10000 eq LongVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableLongVal eq 20000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("20000 eq NullableLongVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // ULong and Nullable<ULong>
        (await _context.Users.Where("ULongVal eq 10000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("10000 eq ULongVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableULongVal eq 20000").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("20000 eq NullableULongVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // Float and Nullable<Float>
        (await _context.Users.Where("FloatVal eq 1.5").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("1.5 eq FloatVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableFloatVal eq 2.5").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("2.5 eq NullableFloatVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // Double and Nullable<Double>
        (await _context.Users.Where("DoubleVal eq 10.5").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("10.5 eq DoubleVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDoubleVal eq 20.5").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("20.5 eq NullableDoubleVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // Decimal and Nullable<Decimal>
        (await _context.Users.Where("DecimalVal eq 100.50").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("100.50 eq DecimalVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDecimalVal eq 200.75").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("200.75 eq NullableDecimalVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // Char and Nullable<Char>
        (await _context.Users.Where("CharVal eq 'A'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'A' eq CharVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableCharVal eq 'Z'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'Z' eq NullableCharVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // DateOnly and Nullable<DateOnly>
        (await _context.Users.Where("DateOnlyVal eq '2025-01-01'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'2025-01-01' eq DateOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDateOnlyVal eq '2025-06-01'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'2025-06-01' eq NullableDateOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // DateTime and Nullable<DateTime> (Standard UTC parsing without legacy switch)
        (await _context.Users.Where("DateTimeVal eq '2025-01-01T10:00:00'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'2025-01-01T10:00:00' eq DateTimeVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableDateTimeVal eq '2025-06-01T12:00:00'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'2025-06-01T12:00:00' eq NullableDateTimeVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // TimeOnly and Nullable<TimeOnly>
        (await _context.Users.Where("TimeOnlyVal eq '10:00:00'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'10:00:00' eq TimeOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableTimeOnlyVal eq '12:00:00'").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("'12:00:00' eq NullableTimeOnlyVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");

        // Boolean and Nullable<Boolean>
        (await _context.Users.Where("BoolVal eq true").ToListAsync()).Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie"]);
        (await _context.Users.Where("true eq BoolVal").ToListAsync()).Select(u => u.String).Should().BeEquivalentTo(["Alice", "Charlie"]);
        (await _context.Users.Where("BoolVal eq false").ToListAsync()).Select(u => u.String).Should().BeEquivalentTo(["Bob", "O'Reilly"]);
        (await _context.Users.Where("false eq BoolVal").ToListAsync()).Select(u => u.String).Should().BeEquivalentTo(["Bob", "O'Reilly"]);
        (await _context.Users.Where("NullableBoolVal eq false").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("false eq NullableBoolVal").ToListAsync()).Select(u => u.String).Should().Equal("Alice");
        (await _context.Users.Where("NullableBoolVal eq true").ToListAsync()).Select(u => u.String).Should().BeEquivalentTo(["Charlie", "O'Reilly"]);
        (await _context.Users.Where("true eq NullableBoolVal").ToListAsync()).Select(u => u.String).Should().BeEquivalentTo(["Charlie", "O'Reilly"]);
    }

    [Fact]
    public async Task Filtering_DateTimeAndTimeOnly_ComparisonOperators()
    {
        // DateTime comparisons
        (await _context.Users.Where("DateTimeVal lt '2026-01-01T10:00:00'").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Alice");

        (await _context.Users.Where("DateTimeVal gt '2027-01-01T10:00:00'").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("O'Reilly");

        (await _context.Users.Where("DateTimeVal geq '2027-01-01T10:00:00'").ToListAsync()).Select(u => u.String)
            .Should().BeEquivalentTo(["Charlie", "O'Reilly"]);

        // TimeOnly comparisons
        (await _context.Users.Where("TimeOnlyVal lt '14:00:00'").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Alice");

        (await _context.Users.Where("TimeOnlyVal gt '18:00:00'").ToListAsync()).Select(u => u.String)
            .Should().ContainSingle().Which.Should().Be("Charlie");
    }

    [Fact]
    public void ToExpression_UnsupportedType_ThrowsNotSupportedException()
    {
        var action = () =>
        {
            var tokens = new Queue<Token>();
            tokens.Enqueue(new Token("UnsupportedProp", TokenType.Identifier));
            tokens.Enqueue(new Token("123", TokenType.Literal, LiteralType.Numeric));
            tokens.Enqueue(new Token("eq", TokenType.Operator, LiteralType.None, Operator.Eq));
            tokens.ToExpression<EntityWithUnsupportedType>();
        };

        action.Should().Throw<NotSupportedException>()
            .WithMessage("*Unsupported type*");
    }

    private class EntityWithUnsupportedType
    {
        public object UnsupportedProp { get; set; } = null!;
    }
}
