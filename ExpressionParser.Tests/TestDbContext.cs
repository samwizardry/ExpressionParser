using Microsoft.EntityFrameworkCore;

namespace ExpressionParser.Tests;

internal class TestDbContext : DbContext
{
    private readonly string _connectionString;

    static TestDbContext()
    {
        // Allows DateTime with Kind=Unspecified (what the parser produces via DateTimeOffset.Parse(...).DateTime).
        // Without this Npgsql requires Kind=Utc for timestamp with time zone columns.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
    }

    public TestDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }

    public DbSet<User> Users => Set<User>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(_connectionString);
    }
}
