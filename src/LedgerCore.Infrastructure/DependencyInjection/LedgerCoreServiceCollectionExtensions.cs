using LedgerCore.Application.Repositories;
using LedgerCore.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LedgerCore.Infrastructure.DependencyInjection;

public static class LedgerCoreServiceCollectionExtensions
{
    public static IServiceCollection AddLedgerCore(
        this IServiceCollection services,
        Action<LedgerCoreOptions> configure)
    {
        var options = new LedgerCoreOptions();
        configure(options);

        services.AddDbContext<LedgerDbContext>(dbOptions =>
        {
            if (options.PostgreSqlConnectionString is not null)
                dbOptions.UseNpgsql(options.PostgreSqlConnectionString);
        });

        services.AddScoped<IAccountRepository, EfAccountRepository>();
        services.AddScoped<ILedgerRepository, EfLedgerRepository>();
        services.AddScoped<IPeriodRepository, EfPeriodRepository>();

        return services;
    }
}

public class LedgerCoreOptions
{
    internal string? PostgreSqlConnectionString { get; private set; }

    public void UsePostgreSql(string connectionString)
    {
        PostgreSqlConnectionString = connectionString;
    }
}
