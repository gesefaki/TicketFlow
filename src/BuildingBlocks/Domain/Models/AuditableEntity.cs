using TicketFlow.BuildingBlocks.Domain.Interfaces;

namespace TicketFlow.BuildingBlocks.Domain.Models;

/// <summary>
/// Base entity that records creation and last-update timestamps.
/// </summary>
/// <typeparam name="TId">Type of the entity identifier.</typeparam>
public abstract class AuditableEntity<TId> : Entity<TId>, IAuditable
    where TId : struct
{
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; protected init; }
    
    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; protected set; }
}
