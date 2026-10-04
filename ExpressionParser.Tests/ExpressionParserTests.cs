using Microsoft.EntityFrameworkCore;

using Testcontainers.PostgreSql;

namespace ExpressionParser.Tests;

public class ExpressionParserTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder("postgres:16")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    TestDbContext _context = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        _context = new TestDbContext(_postgresContainer.GetConnectionString());
        await _context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _postgresContainer.DisposeAsync();
    }

    [Fact]
    public async Task Test()
    {
        _context.Users.Add(new User { Name = "John" });
        _context.Users.Add(new User { Name = "Tom" });
        await _context.SaveChangesAsync();

        string expression = "Name eq 'John'";

        var users = await _context.Users.Where(expression).ToListAsync();
    }
}
