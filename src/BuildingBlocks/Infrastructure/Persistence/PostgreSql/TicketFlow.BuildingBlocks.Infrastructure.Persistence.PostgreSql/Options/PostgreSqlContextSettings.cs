namespace TicketFlow.BuildingBlocks.Infrastructure.Persistence.PostgreSql.Options;

/// <summary>
///     A class that stores configuration data for PostgreSQL
/// </summary>
public sealed class PostgreSqlContextSettings
{
    public required string ConnectionStringName { get; init; }

    public int MaxBatchSize { get; init; } = 128;
    public int MaxRetryCount { get; init; } = 3;
    public int CommandTimeout { get; init; } = 40;
    public int ConnectionTimeout { get; init; } = 20;

    public bool EnableRetryOnFailure { get; init; } = true;
    public bool EnableSensitiveDataLogging { get; init; }
    public bool EnableDetailedErrors { get; init; }
}