using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using TicketFlow.BuildingBlocks.Infrastructure.Persistence.PostgreSql.Options;

namespace TicketFlow.BuildingBlocks.Infrastructure.Persistence.PostgreSql.Configurators;

/// <summary>
///     A class that implements a PostgreSQL configuration in EF based on <see cref="PostgreSqlContextSettings"/>.
/// </summary>
/// <param name="configurator">Configuration.</param>
/// <param name="loggerFactory">Logger factory.</param>
/// <param name="settings">Settings for configuration.</param>
/// <param name="interceptors">Collection of interceptors for this DbContext.</param>
/// <typeparam name="TDbContext">Type of the DbContext.</typeparam>
public sealed class PostgreSqlDbContextConfigurator<TDbContext>(
    IConfiguration configurator,
    ILoggerFactory loggerFactory,
    PostgreSqlContextSettings settings,
    IEnumerable<IInterceptor> interceptors)
    : IDbContextConfigurator<TDbContext>
    where TDbContext : DbContext
{
    /// <inheritdoc />
    public void Configure(DbContextOptionsBuilder<TDbContext> optionsBuilder)
    {
        var connectionString = configurator.GetConnectionString(settings.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{settings.ConnectionStringName}' was not configured."
                );
        }

        var connection = new NpgsqlConnectionStringBuilder(connectionString)
        {
            Timeout = settings.ConnectionTimeout,
        };

        optionsBuilder
            .UseLoggerFactory(loggerFactory)
            .UseNpgsql(connection.ConnectionString, npgsql =>
            {
                npgsql.CommandTimeout(settings.CommandTimeout);
                npgsql.MaxBatchSize(settings.MaxBatchSize);

                if (settings.EnableRetryOnFailure)
                {
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: settings.MaxRetryCount);
                }
            })
            .AddInterceptors(interceptors)
            .EnableSensitiveDataLogging(
                settings.EnableSensitiveDataLogging)
            .EnableDetailedErrors(settings.EnableDetailedErrors);
    }
}