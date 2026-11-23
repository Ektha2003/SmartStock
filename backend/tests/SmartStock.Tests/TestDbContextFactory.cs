using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartStock.Infrastructure.Data;

namespace SmartStock.Tests;

public static class TestDbContextFactory
{
    private const string PostgresTestConnectionString = "Host=localhost;Port=5432;Database=smartstock_test_db;Username=postgres;Password=Admin@123;Include Error Detail=true";

    public static ApplicationDbContext CreatePostgresDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(PostgresTestConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public static ApplicationDbContext CreateInMemoryDbContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: dbName ?? Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new ApplicationDbContext(options);
    }
}
