using TicketFlow.BuildingBlocks.Domain.Interfaces;

namespace TicketFlow.BuildingBlocks.Domain.Models;

/// <summary>
/// The base entity for every model stored in the database.
/// An <see cref="IAuditable"/> contract is required by default for every entity, so it is implemented by the base class.
/// </summary>
/// <typeparam name="TId">Type of unique identifier of model.</typeparam>
public abstract class BaseEntity<TId> : IAuditable where TId : struct
{
    /// <summary>
    /// The unique entity key.
    /// </summary>
    public TId Id { get; protected init; }
    
    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; protected init; }
    
    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; protected set; }
    
}