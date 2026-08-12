using TicketFlow.BuildingBlocks.Domain.Interfaces;

namespace TicketFlow.BuildingBlocks.Domain.Models;

/// <summary>
/// Base class for domain entities that are identified by a unique identifier.
/// </summary>
/// <typeparam name="TId">Type of the entity identifier.</typeparam>
public abstract class Entity<TId> where TId : struct
{
    /// <summary>
    /// The unique entity key.
    /// </summary>
    public TId Id { get; protected init; }
    
}
