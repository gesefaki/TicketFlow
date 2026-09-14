using Microsoft.EntityFrameworkCore;

namespace TicketFlow.BuildingBlocks.Infrastructure.Persistence.PostgreSql.Configurators;

/// <summary>
///     An interface that defines a contract for DbContext configurators.
/// </summary>
/// <typeparam name="TDbContext">Type of the DbContext.</typeparam>
public interface IDbContextConfigurator<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    ///     Configures this DbContext.
    /// </summary>
    /// <param name="optionsBuilder">Options builder for this DbContext.</param>
    void Configure(DbContextOptionsBuilder<TDbContext> optionsBuilder);
}