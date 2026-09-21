using LedgerCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace LedgerCore.Infrastructure.Tests.Fixtures;

public class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .Build();

    public LedgerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        return new LedgerDbContext(options);
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition("PostgreSql")]
public class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture> { }
