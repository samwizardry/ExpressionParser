using Microsoft.EntityFrameworkCore;

namespace ExpressionParser.Tests;

internal class TestDbContext : DbContext
{
    private readonly string _connectionString;



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
