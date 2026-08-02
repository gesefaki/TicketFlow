namespace TicketFlow.BuildingBlocks.Domain.Interfaces;

/// <summary>
/// Defines the contract for implementing domain event storage.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Read-only collection of domain events for this object.
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    
    /// <summary>
    /// Clears all domain events from domain events collection.
    /// </summary>
    void ClearDomainEvents();
}